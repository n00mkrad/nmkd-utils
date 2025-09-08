namespace NmkdUtils.Media;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

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
            var vf = scale.IsEmpty() ? "" : $" -vf scale={scale}";
            var kf = onlyKf ? $" -skip_frame nokey" : "";
            var frames = (maxFrames is int n && n > 0) ? $" -frames:v {n}" : "";
            string argsIn = $"-loglevel error {kf} -hwaccel auto -i {path.Wrap()}";
            string argsOut = $"{vf}{frames} -map 0:v:0 -vsync 0 -f rawvideo -pix_fmt yuv420p pipe:1";
            string a = $"{argsIn}{argsOut}";

            var psi = new ProcessStartInfo { FileName = "ffmpeg", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, Arguments = a };
            using var p = Process.Start(psi)!;

            // Drain stderr so ffmpeg can't block on a full pipe
            _ = Task.Run(async () =>
            {
                var tmp = new byte[8192];
                while (true)
                {
                    int n;
                    try { n = await p.StandardError.BaseStream.ReadAsync(tmp, 0, tmp.Length, ct); }
                    catch { break; }
                    if (n <= 0) break;
                }
            }, ct);

            var stdout = p.StandardOutput.BaseStream;
            int ySize = outW * outH;
            int cW = outW / 2, cH = outH / 2;
            int cSize = cW * cH;
            int frameSize = checked(ySize + cSize + cSize);

            int index = 0;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (maxFrames is > 0 && index >= maxFrames.Value) break;

                    var buf = ArrayPool<byte>.Shared.Rent(frameSize);
                    bool ok = await ReadExactlyAsync(stdout, buf, frameSize, ct);
                    if (!ok) { ArrayPool<byte>.Shared.Return(buf); break; }

                    var frame = new PooledFrame
                    {
                        Index = index,
                        Width = outW,
                        Height = outH,
                        TimestampSec = (info.Fps > 0) ? (index / info.Fps) : (index * (1.0 / 24.0)),
                        Buffer = buf,
                        Length = frameSize
                    };

                    await channel.Writer.WriteAsync(frame, ct);
                    index++;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                channel.Writer.TryComplete();
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
                try { await p.WaitForExitAsync(CancellationToken.None); } catch { }
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

    public static Image<Rgba32> ToImageRgba32(PooledFrame f) => Image.LoadPixelData<Rgba32>(f.Buffer.AsSpan(0, f.Length), f.Width, f.Height);
    public static Image<Rgba32> ToImageYuv420(PooledFrame f) => FromYuv420Bytes(f.Buffer.AsSpan(0, f.Length), f.Width, f.Height);

    public static async Task Test(IEnumerable<string> args)
    {
        var sw = Stopwatch.StartNew();
        ConcurrentDictionary<int, float> frameLumaMax = [];
        ConcurrentDictionary<int, float> frameLumaAvg = [];
        void frameAction(PooledFrame frame, int workerId)
        {
            float maxLuma = 0f;
            using var img = ToImageYuv420(frame);

            // Apply GetRec709Luminance on all pixels and average
            float totalLuma = 0f;
            img.ProcessPixelRows(pa =>
            {
                for (int y = 0; y < pa.Height; y++)
                {
                    var row = pa.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var luma = row[x].GetRec709Luminance();

                        if (luma > maxLuma)
                            maxLuma = luma;

                        totalLuma += luma;
                    }
                }
            });

            var avgLuma = totalLuma / (img.Width * img.Height);

            if (avgLuma < 0.005 || avgLuma > 0.995)
                return; // Skip pure black/white frames

            frameLumaMax[frame.Index] = maxLuma;
            frameLumaAvg[frame.Index] = avgLuma;
        }
        string path = args.First(File.Exists);
        await Test(path, frameAction, onlyKf: true, logBufferStats: true);
        Logger.Log($"Processed {frameLumaMax.Keys.Count} frames in {sw.Format()}, FPS = {Math.Round(frameLumaMax.Keys.Count / sw.Elapsed.TotalSeconds, 2)}");
        Logger.Log($"Peak Brightness avg: {frameLumaMax.Values.Average():P1}, max: {frameLumaMax.Values.Max():P1}");
        Logger.Log($"Peak Brightness (8-bit) avg: {(frameLumaMax.Values.Average() * 255f).Round()}, max: {(frameLumaMax.Values.Max() * 255f).Round()}");
        Logger.Log($"Frame Brightness avg: {frameLumaAvg.Values.Average():P1}, max: {frameLumaAvg.Values.Max():P1}");
        Logger.Log($"Frame Brightness (8-bit) avg: {(frameLumaAvg.Values.Average() * 255f).Round()}, max: {(frameLumaAvg.Values.Max() * 255f).Round()}");

        int binning = 2;
        int plotH = (frameLumaMax.Keys.Count / binning) / 2;
        var plotMax = ImgUtils.RenderColumnPlot(frameLumaMax, plotH, binning, maxValue: 1, barColor: new Rgba32(0, 74, 127, 255), backgroundColor: new Rgba32(0, 0, 0, 0));
        var plotAvg = ImgUtils.RenderColumnPlot(frameLumaAvg, plotH, binning, maxValue: 1, barColor: new Rgba32(0, 148, 255, 255), backgroundColor: new Rgba32(0, 0, 0, 0));
        ImgUtils.LayerImages([plotMax, plotAvg]).SaveImg($"{path}.Brightness.png", ImgUtils.Format.Png, overwrite: true, dispose: true);
    }

    public static async Task Test(string path, Action<PooledFrame, int> frameAction, string scale = "480:-2", bool onlyKf = false, int? maxFrames = null, int? bufferSize = null, bool logBufferStats = false, CancellationToken ct = default)
    {
        var info = await FfmpegStreaming.ProbeAsync(path);

        bufferSize ??= 256; // Default: width*height*channels * 24 bytes
        var cts = ct.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(ct) : new CancellationTokenSource();

        var channel = FfmpegStreaming.StartDecodeToChannel(path, info, maxFrames, bufferSize: (int)bufferSize, onlyKf, scale, ct: cts.Token);

        // Track buffer statistics
        int processedCount = 0;
        int highestFrameIndex = -1;

        if (logBufferStats)
        {
            _ = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    int producedCount = highestFrameIndex + 1; // +1 because index is 0-based
                    int inBuffer = Math.Max(0, producedCount - processedCount);
                    double fullness = (double)inBuffer / bufferSize.Value;
                    Logger.Log($"Buffer fullness: {inBuffer}/{bufferSize.Value} ({fullness:P1})", condition: () => inBuffer >= 12);
                    await Task.Delay(1000, cts.Token);
                }
            }, cts.Token);
        }

        // Spin up workers
        int workers = Math.Max(2, Environment.ProcessorCount - 2);
        Logger.Log($"Starting {workers} worker tasks");
        var tasks = Enumerable.Range(0, workers).Select(workerId => Task.Run(async () =>
        {
            await foreach (var frame in channel.Reader.ReadAllAsync(cts.Token))
            {
                // Update the highest frame index we've seen
                Interlocked.Exchange(ref highestFrameIndex, Math.Max(highestFrameIndex, frame.Index));

                try
                {
                    if (frame.Index % 500 == 0)
                        Logger.Log($"Worker {workerId.ZPad(3)}: Frame {frame.Index.ZPad(5)} at {FormatUtils.Time(frame.TimestampSec * 1000)} - {FormatUtils.FileSize(frame.Buffer.LongLength)}");

                    frameAction(frame, workerId); // Execute the provided action on the frame
                }
                finally
                {
                    Interlocked.Increment(ref processedCount);
                    frame.Dispose(); // Always return the pooled buffer
                }
            }
        }, cts.Token)).ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Converts one YUV420p frame (contiguous Y|U|V planes) to an Rgba32 ImageSharp Image.
    /// buffer.Length must be width*height*3/2. Width/Height should be even.
    /// </summary>
    public static Image<Rgba32> FromYuv420Bytes(ReadOnlySpan<byte> buffer, int width, int height, bool bt709 = false, bool limitedRange = true)
    {
        if ((width & 1) != 0 || (height & 1) != 0)
            throw new ArgumentException("YUV420p requires even width and height.");

        int ySize = width * height;
        int cW = width / 2, cH = height / 2;
        int cSize = cW * cH;
        if (buffer.Length < ySize + 2 * cSize)
            throw new ArgumentException("Buffer size does not match YUV420p frame.");

        var yPlane = buffer[..ySize];
        var uPlane = buffer.Slice(ySize, cSize);
        var vPlane = buffer.Slice(ySize + cSize, cSize);

        // Coefficients (fixed-point, scale = 256)
        int yMul, yOff, rV, gU, gV, bU;
        if (limitedRange)
        {
            yMul = 298; yOff = 16;
            if (bt709) { rV = 459; gU = -55; gV = -136; bU = 541; }        // BT.709
            else { rV = 409; gU = -100; gV = -208; bU = 516; }       // BT.601
        }
        else
        {
            yMul = 256; yOff = 0;   // JPEG/full range
            rV = 359; gU = -88; gV = -183; bU = 453;
        }

        static byte Clip(int x) => (byte)(x < 0 ? 0 : (x > 255 ? 255 : x));

        var img = new Image<Rgba32>(width, height);

        for (int y = 0; y < height; y += 2)
        {
            var row0 = img.DangerousGetPixelRowMemory(y).Span;
            var row1 = img.DangerousGetPixelRowMemory(y + 1).Span;

            int yRow0 = y * width;
            int yRow1 = (y + 1) * width;
            int cRow = (y / 2) * cW;

            for (int x = 0; x < width; x += 2)
            {
                int cIdx = cRow + (x / 2);
                int U = uPlane[cIdx] - 128;
                int V = vPlane[cIdx] - 128;

                // Precompute chroma terms
                int rC = rV * V;
                int gC = gU * U + gV * V;
                int bC = bU * U;

                // Y samples for 2x2 block
                int C00 = (yPlane[yRow0 + x] - yOff) * yMul;
                int C01 = (yPlane[yRow0 + x + 1] - yOff) * yMul;
                int C10 = (yPlane[yRow1 + x] - yOff) * yMul;
                int C11 = (yPlane[yRow1 + x + 1] - yOff) * yMul;

                // Top-left
                {
                    int R = (C00 + rC + 128) >> 8;
                    int G = (C00 + gC + 128) >> 8;
                    int B = (C00 + bC + 128) >> 8;
                    row0[x] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
                // Top-right
                {
                    int R = (C01 + rC + 128) >> 8;
                    int G = (C01 + gC + 128) >> 8;
                    int B = (C01 + bC + 128) >> 8;
                    row0[x + 1] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
                // Bottom-left
                {
                    int R = (C10 + rC + 128) >> 8;
                    int G = (C10 + gC + 128) >> 8;
                    int B = (C10 + bC + 128) >> 8;
                    row1[x] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
                // Bottom-right
                {
                    int R = (C11 + rC + 128) >> 8;
                    int G = (C11 + gC + 128) >> 8;
                    int B = (C11 + bC + 128) >> 8;
                    row1[x + 1] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
            }
        }

        return img; // caller owns/Dispose() when done
    }
}

