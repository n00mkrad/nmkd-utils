using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using static NmkdUtils.Logger;

namespace NmkdUtils;

public static class ImgExtensions
{
    /// <inheritdoc cref="ImgUtils.Resize(object, float, float?, ResizeMode, Level)"/>
    public static Image Resize(this Image i, float width, float? height = null, ResizeMode mode = ResizeMode.Stretch, IResampler? sampler = null, Level logLvl = Level.Verbose)
        => ImgUtils.Resize(i, width, height, mode, sampler, logLvl);

    /// <inheritdoc cref="ImgUtils.Crop(object, int?, int?, int?, int?, Level))"/>
    public static Image Crop(this Image i, int? width = null, int? height = null, int? x = null, int? y = null, Level logLvl = Level.Verbose)
        => ImgUtils.Crop(i, width, height, x, y, logLvl);

    /// <inheritdoc cref="ImgUtils.PadTo(object, float?, float?, bool, Color?, Level)"/>
    public static Image PadTo(this Image i, float? width = null, float? height = null, bool allowDownsize = false, Color? bgColor = null, Level logLvl = Level.Verbose)
        => ImgUtils.PadTo(i, width, height, allowDownsize, bgColor, logLvl);

    /// <inheritdoc cref="ImgUtils.Save(object, string, ImgUtils.Format, bool, int?, bool, bool)"/>
    public static string SaveImg(this Image i, string path, ImgUtils.Format format = ImgUtils.Format.Jpg, bool overwrite = false, int? quality = null, bool allowAltPath = true, bool dispose = false)
        => ImgUtils.Save(i, path, format, overwrite, quality, allowAltPath, dispose);

    /// <summary> Calculates the Rec. 709 luminance of a pixel. </summary>
    public static float GetRec709Luminance(this Rgba32 px) => 0.2126f * (px.R / 255f) + 0.7152f * (px.G / 255f) + 0.0722f * (px.B / 255f);

    /// <summary> Dispose <paramref name="image"/> when <paramref name="condition"/> is true. </summary>
    public static void DisposeIf(this Image image, bool condition)
    {
        if (!condition || image == null)
            return;

        image.Dispose();
    }

    /// <summary> Run an action with every pixel, optionally with subsampling (<paramref name="scale"/>). </summary>
    public static void ProcessPixels(this Image image, float scale, Action<Rgba32, int, int> pixelAction)
    {
        using var tempImg = image.CloneAs<Rgba32>();

        if (scale < 0.999f)
        {
            tempImg.Resize(scale, scale);
        }

        tempImg.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < tempImg.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < tempImg.Width; x++)
                {
                    pixelAction(row[x], x, y);
                }
            }
        });
    }
    /// <inheritdoc cref="ProcessPixels(Image, float, Action{Rgba32, int, int})"/>
    public static void ProcessPixels(this Image image, Action<Rgba32, int, int> pixelAction)
        => ProcessPixels(image, 1.0f, pixelAction);

    /// <summary> Run an action with every pixel, optionally with subsampling (<paramref name="scale"/>). </summary>
    public static void ProcessPixels(this Image image, float scale, Action<Rgba32> pixelAction)
        => ProcessPixels(image, scale, (px, _, _) => pixelAction(px));

    /// <inheritdoc cref="ProcessPixels(Image, float, Action{Rgba32})"/>
    public static void ProcessPixels(this Image image, Action<Rgba32> pixelAction)
        => ProcessPixels(image, 1.0f, pixelAction);

    /// <summary> Gets the sum of the RGB values of a pixel. </summary>
    public static int GetRgbSum(this Rgba32 px) => px.R + px.G + px.B;

    /// <summary> Gets the average of the RGB values of a pixel ((R+G+B)/3). </summary>
    public static float GetRgbAvg(this Rgba32 px) => (px.R + px.G + px.B) / 3f;

    /// <inheritdoc cref="ImgUtils.Channels.Remap(Rgba32, string, byte, byte)"/>
    public static Rgba32 Remap(this Rgba32 px, string channels = "rgba", byte min = 0, byte max = 255)
        => ImgUtils.Channels.Remap(px, channels, min, max);

    /// <summary> Transforms a pixel using the provided function. </summary>
    public static Rgba32 Transform(this Rgba32 px, Func<Rgba32, Rgba32> func) => func == null ? px : func(px);

    /// <summary> Remaps a byte value to a new range [<paramref name="outMin"/>, <paramref name="outMax"/>]. </summary>
    public static byte RemapByte(this byte value, byte outMin = 0, byte outMax = 255) => (byte)((value / 255f) * (outMax - outMin) + outMin).Round();

    public static System.Drawing.Image ToImage(this Image img, bool dispose = false)
    {
        using var ms = new MemoryStream();
        img.SaveAsPng(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder() { CompressionLevel = 0 });
        img.DisposeIf(dispose);
        return System.Drawing.Image.FromStream(ms);
    }
}
