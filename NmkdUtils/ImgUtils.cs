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
        public static System.Drawing.Graphics GetGraphics (System.Drawing.Bitmap bmp, System.Drawing.Color? clearColor = null)
        {
            var gfx = System.Drawing.Graphics.FromImage(bmp);
            gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            gfx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            gfx.Clear(clearColor ?? System.Drawing.Color.Black);
            return gfx;
        }

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

        /// <summary> Gets an image from a file path, base64 string, or native Image object (in this case it will only be passed through). </summary>
        public static Image GetImage(object source)
        {
            if(source is Image img)
                return img;

            if (source is string str)
            {
                if(str.Length <= 1024 && File.Exists(str))
                    return Image.Load(str);

                if (StringUtils.IsBase64(str))
                {
                    using var ms = new MemoryStream(Convert.FromBase64String(str));
                    return Image.Load(ms);
                }

                if (StringUtils.IsWebUrl(str))
                {
                    using Stream stream = new HttpClient().DownloadFileStream(str);
                    return Image.Load(stream);
                }
            }

            return null;
        }
        public static List<Image> LoadImages(List<string> sources) => sources.Select(GetImage).ToList();
        public static List<Image> LoadImages(List<object> sources) => sources.Select(GetImage).ToList();

        /// <summary>
        /// Takes an image (or image path) and resizes it to the specified <paramref name="width"/> and <paramref name="height"/> (Defaults to width). <br/> 
        /// Size values of 8.0 or less are treated as a multiplier relative to the original size (e.g. 0.5). Inversion can be applied with <paramref name="invert"/>, BoxBlur with <paramref name="blur"/> (0 = Off) <br/>
        /// </summary>
        public static Image ResizeImage(object imageOrPath, float width, float? height = null, ResizeMode mode = ResizeMode.Stretch, bool invert = false, int blur = 0, Logger.Level logLvl = Logger.Level.Verbose)
        {
            height ??= width; // If target height is not specified, use target width
            Image image = imageOrPath is Image img ? img : Image.Load(imageOrPath.ToString());

            if (invert)
            {
                image.Mutate(x => x.Invert());
            }

            if (width <= 8.0f)
                width = image.Width * width;

            if (height <= 8.0f)
                height = image.Height * (float)height;

            int w = width.RoundToInt().RoundToNearestMultiple(2);
            int h = ((float)height).RoundToInt().RoundToNearestMultiple(2);
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = mode, Size = new Size(w, h), PadColor = Color.Black }));

            if (blur > 0)
            {
                image.Mutate(x => x.BoxBlur(blur));
            }

            Logger.Log($"Resized{(invert ? ", inverted" : "")}{(blur > 0 ? $", blurred ({blur})" : "")} image to {image.Width}x{image.Height}", logLvl);
            return image;
        }

        /// <summary>
        /// Converts an image to a base64 string. PNG is used for <paramref name="quality"/> 100, JPEG for anything lower. </br> 
        /// </summary>
        public static string ImageToB64(Image image, int quality = 100, int pngCompression = 1, bool subsample = true)
        {
            using var ms = new MemoryStream();

            if (quality < 100)
            {
                image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality, ColorType = JpegEncodingColor.YCbCrRatio420 });
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
                    var padImg = CreateTextImage($"Paragraph {i + 1}", images.Max(i => i.Width), (images.Average(i => i.Height) / 3d).RoundToInt(), "Arial", 44);
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
                    // center this image
                    int x = (canvasWidth - images[i].Width) / 2;
                    ctx.DrawImage(images[i], new Point(x, y), 1f);
                    y += images[i].Height;
                }
            });

            // Dispose
            images.ForEach(img => img.Dispose());

            return result;
        }

    }
}
