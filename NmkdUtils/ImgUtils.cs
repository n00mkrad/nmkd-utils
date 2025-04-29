using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using HeyRed.ImageSharp.Heif;
using SixLabors.ImageSharp.Processing;

namespace NmkdUtils
{
    public class ImgUtils
    {
        public enum Format { Jpg, Png }

        public static string ConvertHeif(string path, Format format = Format.Jpg, int quality = 95)
        {
            var heifDecoderOpts = new HeifDecoderOptions { DecodingMode = DecodingMode.PrimaryImage, ConvertHdrToEightBit = true, Strict = false };
            using var inputStream = File.OpenRead(path);
            using var image = HeifDecoder.Instance.Decode(heifDecoderOpts, inputStream);

            if (format == Format.Jpg)
            {
                var encoder = new JpegEncoder { Quality = quality };
                string savePath = Path.ChangeExtension(path, "jpg");
                image.Save(savePath, encoder);
                return savePath;
            }

            if (format == Format.Png)
            {
                var encoder = new PngEncoder { CompressionLevel = (PngCompressionLevel)quality };
                string savePath = Path.ChangeExtension(path, "png");
                image.Save(savePath, encoder);
                return savePath;
            }

            return "";
        }

        public static string ResizeImageToBase64Jpeg(string imagePath, int targetWidth = 768, int? targetHeight = null, int jpegQual = 85)
        {
            targetHeight ??= targetWidth; // If target height is not specified, use target width
            using Image image = Image.Load(imagePath);
            // Resize (letterbox/pillarbox to black) using ResizeOptions with ResizeMode.Pad automatically preserves aspect ratio and pads the image to fill
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Pad, Size = new Size(targetWidth, (int)targetHeight), PadColor = Color.Black }));
            // Save to MemoryStream as JPEG and get Base64
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms, new JpegEncoder { Quality = jpegQual });
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
