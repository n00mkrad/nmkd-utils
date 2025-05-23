using HeyRed.ImageSharp.Heif;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;


namespace NmkdUtils
{
    public class ImgUtils
    {
        public enum Format { Jpg, Png }

        /// <summary>
        /// Converts an image from HEIF to JPEG or PNG format. <paramref name="quality"/> sets either JPEG quality (1-99) or PNG compression (0-9).
        /// </summary>
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

        public static Image LoadImage(string path)
        {
            return Image.Load(path);
        }

        public static List<Image> LoadImages(List<string> paths)
        {
            return paths.Select(Image.Load).ToList();
        }

        /// <summary> Takes an image or image path, resizes it to the specified width and height, and returns it as a Base64-encoded JPEG string. Width/height is treated as a relative factor if <10.0f </summary>
        public static Image ResizeImage(object imageOrPath, float targetWidth = 768, float? targetHeight = null, ResizeMode mode = ResizeMode.Stretch, bool invert = false, int blur = 0, Logger.Level logLvl = Logger.Level.Verbose)
        {
            targetHeight ??= targetWidth; // If target height is not specified, use target width
            Image image = imageOrPath is Image img ? img : Image.Load(imageOrPath.ToString());

            if (targetWidth < 10.0f)
                targetWidth = image.Width * targetWidth;

            if (targetHeight < 10.0f)
                targetHeight = image.Height * (float)targetHeight;

            int w = targetWidth.RoundToInt().RoundToNearestMultiple(2);
            int h = ((float)targetHeight).RoundToInt().RoundToNearestMultiple(2);

            image.Mutate(x => x.Resize(new ResizeOptions { Mode = mode, Size = new Size(w, h), PadColor = Color.Black }));

            if (invert)
            {
                image.Mutate(x => x.Invert());
            }

            if (blur > 0)
            {
                image.Mutate(x => x.BoxBlur(blur));
            }

            Logger.Log($"Resized{(invert ? ", inverted" : "")}{(blur > 0 ? $", bluured ({blur})" : "")} image to {image.Width}x{image.Height}", logLvl);
            return image;
        }

        /// <summary>
        /// Converts an image to a base64 string. PNG is used for <paramref name="quality"/> 100, JPEG for anything lower.
        /// </summary>
        public static string ImageToB64(Image image, int quality = 100, int pngCompression = 1)
        {
            using var ms = new MemoryStream();

            if (quality < 100)
            {
                image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
            }
            else
            {
                image.SaveAsPng(ms, new PngEncoder() { CompressionLevel = (PngCompressionLevel)pngCompression });
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static Image CreateTextImage(string text, int width, int height, string fontName = "Arial", int fontSize = 36, bool invert = false)
        {
            Font font = SystemFonts.CreateFont(fontName, fontSize, FontStyle.Regular);
            var image = new Image<Rgb24>(width, height, Color.Black);

            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(width / 2f, height / 2f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            image.Mutate(ctx => ctx.DrawText(textOptions, text, Color.White));

            if (invert)
            {
                image.Mutate(x => x.Invert());
            }

            return image;
        }

        public static Image StackVertical(List<Image> images, List<Image>? padImages = null, bool padFirst = true, bool autoGenPadImgs = false)
        {
            padImages ??= new List<Image>();

            if (autoGenPadImgs && padImages.None())
            {
                for (int i = 0; i < images.Count; i++)
                {
                    var padImg = CreateTextImage($"Paragraph {i + 1}", images.Max(i => i.Width), (images.Average(i => i.Height) / 3d).RoundToInt(), "Arial", 32);
                    padImages.Add(padImg);
                }
            }

            List<Image> interleaved = [];

            // Interleave padding images between the main images
            for (int i = 0; i < images.Count; i++)
            {
                if(padFirst || (!padFirst && i > 0))
                {
                    interleaved.Add(padImages.Count < images.Count ? padImages[0] : padImages[i]);
                }
                
                interleaved.Add(images[i]);
                // images.Insert(i * 2 /* + 1 */, padImages.Count < images.Count ? padImages[0] : padImages[i]); // Insert corresponding padding image, use first if not enough
            }

            images = interleaved;

            // 1) compute final size
            int canvasWidth = images.Max(img => img.Width);
            int canvasHeight = images.Sum(img => img.Height);

            // 2) make the canvas
            var result = new Image<Rgb24>(canvasWidth, canvasHeight);
            int y = 0;

            // 3) draw everything in one Mutate call
            result.Mutate(ctx =>
            {
                for (int i = 0; i < images.Count; i++)
                {
                    var img = images[i];

                    // center this image
                    int x = (canvasWidth - img.Width) / 2;
                    ctx.DrawImage(img, new Point(x, y), 1f);
                    y += img.Height;
                }
            });

            // Dispose
            images.ForEach(img => img.Dispose());

            return result;
        }

    }
}
