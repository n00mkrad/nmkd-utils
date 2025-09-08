namespace NmkdUtils.Media;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
// using Stream = System.IO.Stream;

public record VideoInfo(int Width, int Height, double Fps);

public sealed class PooledFrame : IDisposable
{
    public int Index { get; init; }
    public byte[] Buffer { get; init; } = default!;
    public int Length { get; init; }       // == frameSize
    public int Width { get; init; }
    public int Height { get; init; }
    public double TimestampSec { get; init; }

    public ReadOnlySpan<byte> Span => Buffer.AsSpan(0, Length);

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(Buffer);
    }
}

public static class FfmpegStreaming
{
    public static async Task<VideoInfo> ProbeAsync(string path, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffprobe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
            Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,avg_frame_rate -of json -i {path.Wrap()}"
        };

        using var p = Process.Start(psi)!;
        string json = await p.StandardOutput.ReadToEndAsync();
        string err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new InvalidOperationException($"ffprobe failed: {err}");

        using var doc = JsonDocument.Parse(json);
        var stream = doc.RootElement.GetProperty("streams")[0];
        int w = stream.GetProperty("width").GetInt32();
        int h = stream.GetProperty("height").GetInt32();
        var afr = stream.GetProperty("avg_frame_rate").GetString() ?? "0/1";
        double fps = ParseRational(afr);
        if (fps <= 0) fps = 24000d / 1001d;
        return new VideoInfo(w, h, fps);
    }

    private static readonly Regex ShowInfoSizeRegex = new(@"s:(\d+)x(\d+)", RegexOptions.Compiled);

    public static (int Width, int Height) GetOutputSize(string path, string? scale, CancellationToken ct = default)
    {
        var vf = string.IsNullOrWhiteSpace(scale) ? "showinfo" : $"scale={scale},showinfo";
        string ffmpegOutput = OsUtils.Run($"ffmpeg -hide_banner -loglevel info -nostats -i {path.Wrap()} -vf {vf} -frames:v 1 -f null -");

        if (ffmpegOutput.IsEmpty())
            throw new InvalidOperationException($"ffmpeg preflight failed.\n{ffmpegOutput}");

        var m = ShowInfoSizeRegex.Match(ffmpegOutput.Trim());
        if (!m.Success)
            throw new InvalidOperationException($"Could not parse output size from showinfo.\n{ffmpegOutput}");

        return (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
    }

    public static Channel<PooledFrame> StartDecodeToChannel(string path, VideoInfo info, int? maxFrames = null, int bufferSize = 64, bool onlyKf = false, string? scale = null, CancellationToken ct = default)
    {
        var (outW, outH) = GetOutputSize(path, scale, ct);
        Logger.Log($"Frame size: {outW}x{outH}");

        var bco = new BoundedChannelOptions(bufferSize) { SingleWriter = true, SingleReader = false, FullMode = BoundedChannelFullMode.Wait };
        var channel = Channel.CreateBounded<PooledFrame>(bco);

        _ = Task.Run(async () =>
        {
            var pixFmt = "rgba";
            var vf = scale.IsEmpty() ? "" : $" -vf scale={scale}";
            var kf = onlyKf ? $" -skip_frame nokey" : "";
            var frames = (maxFrames is int n && n > 0) ? $" -frames:v {n}" : "";
            string argsIn = $"-loglevel error -hwaccel auto {kf} -i {path.Wrap()}";
            string argsOut = $"{vf}{frames} -map 0:v:0 -vsync 0 -f rawvideo -pix_fmt {pixFmt} pipe:1";
            string a = $"{argsIn}{argsOut}";

            var psi = new ProcessStartInfo { FileName = "ffmpeg", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, Arguments = a };
            using var p = Process.Start(psi)!;

            var stdout = p.StandardOutput.BaseStream;
            _ = Task.Run(async () => { // Drain stderr to avoid blocking on full pipe
                var buf = new byte[8192];
                while (!ct.IsCancellationRequested)
                {
                    int n;
                    try { n = await p.StandardError.BaseStream.ReadAsync(buf, 0, buf.Length, ct); }
                    catch { break; }
                    if (n <= 0) break;
                }
            }, ct);

            // If you used -vf scale, actual output size may differ from probed size:
            // int outW = info.Width, outH = info.Height;
            // if (!string.IsNullOrWhiteSpace(scale))
            // {
            //     // If you set scale, consider probing again with -of lavfi or pass known target size here.
            //     // For simplicity, assume scale is exact like "1280:720".
            //     var parts = scale.Split(':');
            //     if (parts.Length == 2 && int.TryParse(parts[0], out var sw) && int.TryParse(parts[1], out var sh))
            //     { outW = sw; outH = sh; }
            // }

            int frameSize = checked(outW * outH * 4);
            int index = 0;

            try
            {
                while (true)
                {
                    // rent a buffer for one frame
                    var buf = ArrayPool<byte>.Shared.Rent(frameSize);
                    bool ok = await ReadExactlyAsync(stdout, buf, frameSize, ct);
                    if (!ok)
                    {
                        ArrayPool<byte>.Shared.Return(buf);
                        break;
                    }

                    double ts = (info.Fps > 0) ? (index / info.Fps) : (index * (1.0 / 30.0));

                    var frame = new PooledFrame
                    {
                        Index = index,
                        Buffer = buf,
                        Length = frameSize,
                        Width = outW,
                        Height = outH,
                        TimestampSec = ts
                    };

                    await channel.Writer.WriteAsync(frame, ct);
                    index++;
                }
            }
            catch (OperationCanceledException) { /* cancelled */ }
            finally
            {
                channel.Writer.TryComplete();
                try
                {
                    if (!p.HasExited)
                    {
                        try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                        await p.WaitForExitAsync(CancellationToken.None);
                    }
                }
                catch { /* ignore */ }
            }
        }, ct);

        return channel;
    }

    private static double ParseRational(string s)
    {
        var parts = s.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], out var n) &&
            double.TryParse(parts[1], out var d) &&
            d != 0) return n / d;
        return double.TryParse(s, out var v) ? v : 0;
    }

    private static async Task<bool> ReadExactlyAsync(System.IO.Stream s, byte[] buffer, int count, CancellationToken ct)
    {
        int offset = 0;
        while (offset < count)
        {
            int n = await s.ReadAsync(buffer, offset, count - offset, ct);
            if (n == 0) return false; // EOF
            offset += n;
        }
        return true;
    }

    public static Bitmap ToBitmapBgra32(PooledFrame frame)
    {
        var bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
        var rect = new System.Drawing.Rectangle(0, 0, frame.Width, frame.Height);
        var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
        try
        {
            int bytes = frame.Width * frame.Height * 4;
            Marshal.Copy(frame.Buffer, 0, data.Scan0, bytes);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
        return bmp;
    }

    public static Image<Bgr24> ToImageSharpBgr24(PooledFrame frame)
    {
        // Cast bytes → Bgr24 pixels (no per-pixel loops).
        var pixelSpan = MemoryMarshal.Cast<byte, Bgr24>(frame.Span);
        // This copies into ImageSharp’s buffers (safe with ArrayPool).
        return SixLabors.ImageSharp.Image.LoadPixelData<Bgr24>(pixelSpan, frame.Width, frame.Height);
    }

    public static Image<Bgra32> ToImageBgra32(PooledFrame f)
    {
        // Copies pixel data into ImageSharp-managed memory
        return SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(f.Buffer.AsSpan(0, f.Length), f.Width, f.Height);
    }

    public static Image<Rgba32> ToImageRgba32(PooledFrame f)
    {
        return SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(f.Buffer.AsSpan(0, f.Length), f.Width, f.Height);
    }

    public static async Task Test()
    {
        var sw = Stopwatch.StartNew();
        ConcurrentDictionary<int, float> frameLuma = [];
        void frameAction(PooledFrame frame, int workerId)
        {
            using var img = ToImageRgba32(frame);
            img.Mutate(x => x.Resize(new ResizeOptions() { Size = new SixLabors.ImageSharp.Size(1, 1), Mode = ResizeMode.Stretch }));
            frameLuma[frame.Index] = img[0, 0].GetRec709Luminance();
        }
        string path = @"\\NAS\media\Shows\Fallout (2024)\S01\Fallout.S01E01.1080p.BluRay.AV1.DDP.5.1-dAV1nci.mkv";
        await Test(path, frameAction, onlyKf: true);
        Logger.Log($"Processed {frameLuma.Keys.Count} frames in {sw.Format()}, FPS = {Math.Round(frameLuma.Keys.Count / sw.Elapsed.TotalSeconds, 2)}");
        Logger.Log($"Brightness ({frameLuma.Count} samples) avg: {frameLuma.Values.Average():P1}, max: {frameLuma.Values.Max():P1}");
        Logger.Log($"Brightness (8-bit) avg: {(frameLuma.Values.Average() * 255f).Round()}, max: {(frameLuma.Values.Max() * 255f).Round()}");
    }

    public static async Task Test(string path, Action<PooledFrame, int> frameAction, string scale = "1280:-2", bool onlyKf = false, int? maxFrames = null, int? bufferSize = null, CancellationToken ct = default)
    {
        var info = await FfmpegStreaming.ProbeAsync(path);

        bufferSize ??= 512; // Default: width*height*channels * 24 bytes
        var cts = ct.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(ct) : new CancellationTokenSource();
        
        var channel = FfmpegStreaming.StartDecodeToChannel(path, info, maxFrames, bufferSize: (int)bufferSize, onlyKf, scale, ct: cts.Token);

        // Spin up workers
        int workers = 64; // Math.Max(2, Environment.ProcessorCount - 2);
        var tasks = Enumerable.Range(0, workers).Select(workerId => Task.Run(async () =>
        {
            await foreach (var frame in channel.Reader.ReadAllAsync(cts.Token))
            {
                try
                {
                    // Build an ImageSharp image (this copies the frame's bytes into the image)
                    if(frame.Index % 100 == 0)
                        Logger.Log($"Worker {workerId.ZPad(3)}: Frame {frame.Index.ZPad(6)} at {frame.TimestampSec:F2}s - {FormatUtils.FileSize(frame.Buffer.LongLength)}");
                    
                    // Execute the provided action on the frame
                    frameAction(frame, workerId);
                }
                finally
                {
                    // Always return the pooled buffer
                    frame.Dispose();
                }
            }
        }, cts.Token)).ToArray();
        
        await Task.WhenAll(tasks);
    }
}

