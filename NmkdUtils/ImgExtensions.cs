using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static NmkdUtils.Logger;

namespace NmkdUtils;

public static class ImgExtensions
{
    /// <inheritdoc cref="ImgUtils.Resize(object, float, float?, ResizeMode, Level)"/>
    public static Image Resize(this Image i, float width, float? height = null, ResizeMode mode = ResizeMode.Stretch, Level logLvl = Level.Verbose)
        => ImgUtils.Resize(i, width, height, mode, logLvl);

    /// <inheritdoc cref="ImgUtils.Resize(object, float, float?, ResizeMode, Level)"/>
    public static Image Crop(this Image i, int? width = null, int? height = null, int? x = null, int? y = null, Level logLvl = Level.Verbose)
        => ImgUtils.Crop(i, width, height, x, y, logLvl);

    /// <inheritdoc cref="ImgUtils.PadTo(object, float?, float?, bool, Color?, Level)"/>
    public static Image PadTo(this Image i, float? width = null, float? height = null, bool allowDownsize = false, Color? bgColor = null, Level logLvl = Level.Verbose)
        => ImgUtils.PadTo(i, width, height, allowDownsize, bgColor, logLvl);

    /// <inheritdoc cref="ImgUtils.Save(object, ImgUtils.Format, string, bool, int, bool)"/>
    public static string Save(this Image i, string path, ImgUtils.Format format = ImgUtils.Format.Jpg, bool overwrite = false, int? quality = null, bool allowAltPath = true, bool dispose = false)
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
}
