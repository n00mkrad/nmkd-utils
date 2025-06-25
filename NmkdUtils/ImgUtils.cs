using CoenM.ImageHash.HashAlgorithms;
using HeyRed.ImageSharp.Heif;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Diagnostics;


namespace NmkdUtils
{
    public class ImgUtils
    {
        /// <summary> Create a high quality drawing context for <paramref name="bmp"/>. </summary>
        public static System.Drawing.Graphics GetGraphics(System.Drawing.Bitmap bmp, System.Drawing.Color? clearColor = null)
        {
            var gfx = System.Drawing.Graphics.FromImage(bmp);
            gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            gfx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            gfx.Clear(clearColor ?? System.Drawing.Color.Black);
            return gfx;
        }

        public enum Format { Jpg, Jpg444, Png }

        private static JpegEncodingColor GetSubsampling(Format f) => f == Format.Jpg444 ? JpegEncodingColor.YCbCrRatio444 : JpegEncodingColor.YCbCrRatio420;

        /// <summary>
        /// Converts an image from HEIF to JPEG or PNG format. <paramref name="quality"/> sets either JPEG quality (1-99) or PNG compression (0-9).
        /// </summary>
        public static string ConvertHeif(string path, Format format = Format.Jpg, int quality = 95)
        {
            var heifDecoderOpts = new HeifDecoderOptions { DecodingMode = DecodingMode.PrimaryImage, ConvertHdrToEightBit = true, Strict = false };
            using var inputStream = File.OpenRead(path);
            using var image = HeifDecoder.Instance.Decode(heifDecoderOpts, inputStream);

            if (format is Format.Jpg or Format.Jpg444)
            {
                var encoder = new JpegEncoder { Quality = quality, ColorType = GetSubsampling(format) };
                string savePath = Path.ChangeExtension(path, "jpg");
                image.Save(savePath, encoder);
                return savePath;
            }

            if (format is Format.Png)
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
            if (source is Image img)
                return img;

            if (source is IList<string> strList) // Unwrap list of strings, take first item
                source = strList.FirstOrDefault();

            try
            {
                if (source is string str)
                {
                    if (str.Length <= 1024 && File.Exists(str))
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
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to get image");
                return null;
            }

            Logger.LogErr($"Failed to get image from {source} ({source.GetType()})");
            return null;
        }
        /// <summary> Load multiple images from a list of paths or base64 strings. </summary>
        public static List<Image> GetImages(List<string> sources) => sources.Select(GetImage).ToList();

        /// <summary>
        /// Saves an image as <paramref name="format"/> to <paramref name="path"/>. If the path exists and <paramref name="allowAltPath"/> is true, an alternative path will be used. <br/>
        /// <paramref name="quality"/> is format-dependent, defaults are used if null. <br/> Returns the path where the image was saved.
        /// </summary>
        public static string Save(object input, string path, Format format = Format.Jpg, bool overwrite = false, int? quality = null, bool allowAltPath = true, bool dispose = false)
        {
            Image img = GetImage(input);
            string savePath = Path.ChangeExtension(path, format.ToString().Low().RemoveNumbers());

            if (File.Exists(path) && !overwrite)
            {
                if (!allowAltPath)
                    return "";

                savePath = IoUtils.GetAvailablePath(path);
            }

            if (format == Format.Jpg)
            {
                quality ??= 95; // Default JPEG quality if not specified
                var encoder = new JpegEncoder { Quality = quality, ColorType = GetSubsampling(format) };
                img.Save(savePath, encoder);
            }

            if (format == Format.Png)
            {
                quality ??= 1; // Default PNG compression level if not specified (0-9)
                var encoder = new PngEncoder { CompressionLevel = (PngCompressionLevel)quality };
                img.Save(savePath, encoder);
            }

            img?.DisposeIf(dispose);
            return savePath;
        }

        /// <summary>
        /// Takes an image (or image path) and resizes it to the specified <paramref name="width"/> and <paramref name="height"/> (Defaults to width). <br/> 
        /// Size values of 8.0 or less are treated as a multiplier relative to the original size (e.g. 0.5).
        /// </summary>
        public static Image Resize(object input, float width, float? height = null, ResizeMode mode = ResizeMode.Stretch, Logger.Level logLvl = Logger.Level.Verbose)
        {
            height ??= width; // If target height is not specified, use target width
            Image image = GetImage(input);

            if (width <= 8.0f)
                width = image.Width * width;

            if (height <= 8.0f)
                height = image.Height * (float)height;

            int w = width.Round().RoundToMultiple(2);
            int h = ((float)height).Round().RoundToMultiple(2);
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = mode, Size = new Size(w, h), PadColor = Color.Black }));

            Logger.Log($"Resized image to {image.Width}x{image.Height}", logLvl);
            return image;
        }

        /// <summary> Applies image augmentation effects. <paramref name="invert"/> to invert colors, <paramref name="blur"/> for box blur </summary>
        public static Image Augment(object input, bool invert, int blur = 0, Logger.Level logLvl = Logger.Level.Verbose)
        {
            Image img = GetImage(input);

            if (invert == false && blur <= 0)
                return img;

            if (invert)
            {
                img.Mutate(x => x.Invert());
            }
            if (blur > 0)
            {
                img.Mutate(x => x.BoxBlur(blur));
            }

            Logger.Log($"Augmented image{(invert ? ", inverted" : "")}{(blur > 0 ? $", box blur ({blur})" : "")}", logLvl);
            return img;
        }

        /// <summary>
        /// Crops an image to <paramref name="width"/> x <paramref name="height"/> starting from the top-left point (<paramref name="x"/>, <paramref name="y"/>). <br/>
        /// If they are null, it will center the crop area. <br/>
        /// </summary>
        public static Image Crop(object input, int? width = null, int? height = null, int? x = null, int? y = null, Logger.Level logLvl = Logger.Level.Verbose)
        {
            Image img = GetImage(input);
            width ??= img.Width; // Default to full width if not specified
            height ??= img.Height; // Default to full height if not specified
            x ??= (img.Width - width) / 2; // Center crop if x is not specified
            y ??= (img.Height - height) / 2; // Center crop if y is not specified

            if (width >= img.Width && height >= img.Height)
                return img;

            // If x/y are out of bounds, default to center
            if (!x.Value.IsInRange(0, img.Width) || !y.Value.IsInRange(0, img.Height))
            {
                x = (img.Width - width) / 2;
                y = (img.Height - height) / 2;
            }

            // Check if out of bounds
            if (x + width > img.Width || y + height > img.Height)
            {
                Logger.LogWrn($"Crop area ({x}, {y}, {width}, {height}) is out of bounds for image size {img.Size.Format()}. Returning original image.");
                return img;
            }

            img.Mutate(ctx => ctx.Crop(new Rectangle((int)x, (int)y, (int)width, (int)height)));
            Logger.Log($"Cropped image to {img.Size.Format()} at ({x}, {y})", logLvl);
            return img;
        }

        /// <summary>
        /// Pads an image to the target <paramref name="width"/> and <paramref name="height"/> (both default to original size) using the background color <paramref name="bgColor"/>. <br/>
        /// Size values of 8.0 or less are treated as a multiplier relative to the original size (e.g. 0.5). <br/>
        /// With <paramref name="allowDownsize"/>, images larger than the target will be downscaled first.
        /// </summary>
        public static Image PadTo(object input, float? width = null, float? height = null, bool allowDownsize = false, Color? bgColor = null, Logger.Level logLvl = Logger.Level.Verbose)
        {
            Image img = GetImage(input);
            width ??= img.Width; // Default to original width if not specified
            height ??= img.Height; // Default to original height if not specified

            if (width <= 8.0f)
                width = img.Width * width;

            if (height <= 8.0f)
                height = img.Height * (float)height;

            if (allowDownsize && (img.Width > width || img.Height > height))
            {
                Logger.Log($"Image is larger than target size ({img.Width}x{img.Height} > {width}x{height}), resizing to fit.", logLvl);
                img.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size((int)width, (int)height) }));
            }

            img.Mutate(i => i.Pad((int)width, (int)height, bgColor ?? Color.Black));
            Logger.Log($"Padded image to {img.Size.Format()}", logLvl);
            return img;
        }

        /// <summary> Rotate an image by <paramref name="angle"/> degrees. </summary>
        public static void Rotate(object input, float angle, Logger.Level logLvl = Logger.Level.Verbose)
        {
            Image img = GetImage(input);

            if (angle == 0f)
                return;

            img.Mutate(x => x.Rotate(angle));
            Logger.Log($"Rotated image by {angle}°", logLvl);
        }

        /// <summary>
        /// Converts an image to a base64 string. PNG is used for <paramref name="quality"/> 100, JPEG for anything lower, with 4:2:0 sampling unless <paramref name="subsample"/> is true. <br/>
        /// <paramref name="pngCompression"/> sets the PNG compression level (0-9). <paramref name="stripMetadata"/> removes EXIF, ICC, and XMP metadata from the image. <br/>
        /// If a path is passed to <paramref name="writeTo"/>, the image will also be written to that file.
        /// </summary>
        public static string ImageToB64(Image image, int quality = 100, int pngCompression = 1, bool subsample = true, bool stripMetadata = true, string writeTo = "")
        {
            using var ms = new MemoryStream();

            if (stripMetadata)
                image = StripMetadata(image);

            if (quality < 100)
            {
                image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality, ColorType = subsample ? JpegEncodingColor.YCbCrRatio420 : JpegEncodingColor.YCbCrRatio444 });
            }
            else
            {
                image.SaveAsPng(ms, new PngEncoder() { CompressionLevel = (PngCompressionLevel)pngCompression });
            }

            if (writeTo.IsNotEmpty())
            {
                File.WriteAllBytes(writeTo, ms.ToArray());
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary> Remove EXIF, ICC and XMP metadata from an image. </summary>
        public static Image StripMetadata(Image image, bool stripExif = true, bool stripIcc = true, bool stripXmp = true)
        {
            image.Metadata.ExifProfile = stripExif ? null : image.Metadata.ExifProfile;
            image.Metadata.IccProfile = stripIcc ? null : image.Metadata.IccProfile;
            image.Metadata.XmpProfile = stripXmp ? null : image.Metadata.XmpProfile;
            return image;
        }

        /// <summary> Estimate font size that results in <paramref name="targetHeightPx"/> pixel height. </summary>
        public static float GetFontSizeByTargetHeight(int targetHeightPx, string fontName = "Arial", FontStyle style = FontStyle.Regular, string sampleText = "The Quick Brown Fox Jumps Over The Lazy Dog.")
        {
            Font font = SystemFonts.CreateFont(fontName, 1f, style);
            var size = TextMeasurer.MeasureSize(sampleText, new TextOptions(font));
            float fontSize = targetHeightPx / size.Height;
            return fontSize;
        }

        /// <summary> Create a simple text image sized to <paramref name="width"/>×<paramref name="height"/>. </summary>
        public static Image CreateTextImage(string text, int width, int height, string fontName = "Arial", float maxFontSize = 100f, bool whiteBg = false, bool invert = false)
        {
            float fontSize = GetFontSizeByTargetHeight(height);
            Font font = SystemFonts.CreateFont(fontName, fontSize > maxFontSize ? maxFontSize : fontSize, FontStyle.Regular);
            var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));
            height = height.Clamp(0, (textSize.Height * 1.45f).Round());
            var image = new Image<Rgb24>(width, height, whiteBg ? Color.White : Color.Black);
            var textOptions = new RichTextOptions(font) { Origin = new PointF(width / 2f, height / 2f), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            image.Mutate(ctx => ctx.DrawText(textOptions, text, whiteBg ? Color.Black : Color.White));

            if (invert)
            {
                image.Mutate(x => x.Invert());
            }

            return image;
        }

        /// <summary> Stack images vertically, optionally inserting padding images. </summary>
        public static Image StackVertical(List<Image> images, List<Image>? padImages = null, bool padFirst = true, bool autoGenPadImgs = false, Color? bgCol = null, int marginPx = 8)
        {
            padImages ??= new List<Image>();
            bgCol ??= Color.Black;

            if (autoGenPadImgs && padImages.None())
            {
                for (int i = 0; i < images.Count; i++)
                {
                    var padImg = CreateTextImage($"Paragraph {i + 1}", images.Max(i => i.Width), (images.Average(i => i.Height) / 3d).RoundToInt(), "Arial");
                    padImages.Add(padImg);
                }
            }

            List<Image> interleaved = [];

            // Interleave padding images between the main images
            for (int i = 0; i < images.Count; i++)
            {
                if (padFirst || (!padFirst && i > 0))
                {
                    interleaved.Add(padImages.Count < images.Count ? padImages[0] : padImages[i]);
                }

                interleaved.Add(images[i]);
            }

            images = interleaved;

            // 1) compute final size
            int canvasWidth = images.Max(img => img.Width);
            int canvasHeight = images.Sum(img => img.Height) + ((images.Count + 1) * marginPx);

            // 2) make the canvas
            var result = new Image<Rgb24>(canvasWidth, canvasHeight, (Rgb24)bgCol);
            int y = marginPx;

            // 3) draw everything in one Mutate call
            result.Mutate(ctx =>
            {
                for (int i = 0; i < images.Count; i++)
                {
                    int x = (canvasWidth - images[i].Width) / 2; // center this image
                    ctx.DrawImage(images[i], new Point(x, y), 1f);
                    y = y + images[i].Height + marginPx;
                }
            });

            // Dispose
            images.ForEach(img => img.Dispose());
            return result;
        }

        /// <summary> Compute a perceptual hash from an image. </summary>
        public static ulong ComputeAverageHash(object input)
        {
            var img = GetImage(input);
            using var clone = img.CloneAs<Rgba32>();
            var hash = new PerceptualHash().Hash(clone);
            return hash;
        }

        /// <summary> 
        /// Returns the rectangle containing anything that's not pure black. <br/> If <paramref name="apply"/> is true, the detected crop will be applied right away. <br/>
        /// Subsampling can be controlled with <paramref name="scale"/>. <br/>
        /// </summary>
        public static Rectangle GetAutoCrop(object input, int paddingW = 0, int paddingH = 0, bool apply = false, float scale = 0.2f, bool dispose = false)
        {
            var img = GetImage(input);
            using Image<Rgba32> tempImg = img.CloneAs<Rgba32>();
            tempImg.Resize(scale, scale); // Downscale for speed/memory efficiency

            // initialize to "no content"
            int minX = tempImg.Width, minY = tempImg.Height;
            int maxX = 0, maxY = 0;

            // 2) Single pass: find extents of any non‐black pixel
            tempImg.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < tempImg.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < tempImg.Width; x++)
                    {
                        ref var p = ref row[x];
                        if ((p.R + p.G + p.B) > 0) // Grayscale test: (R+G+B)>0
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            });

            if (minX > maxX || minY > maxY)
            {
                img.DisposeIf(dispose);
                return Rectangle.Empty;
            }

            // Undo downsampling
            var x = (minX / scale).Round();
            var y = (minY / scale).Round();
            int w = ((maxX - minX + 1) / scale).Round();
            int h = ((maxY - minY + 1) / scale).Round();

            // Add padding
            x = Math.Max(0, x - paddingW);
            y = Math.Max(0, y - paddingH);
            w = Math.Min(img.Width - x, w + paddingW * 2);
            h = Math.Min(img.Height - y, h + paddingH * 2);

            if (apply)
                img.Crop(w, h, x, y);

            img.DisposeIf(dispose);
            return new Rectangle(x, y, w, h);
        }

        /// <summary>
        /// <inheritdoc cref="CountNonBlackPixels(object, float, int, bool)"/> for a list of inputs. <br/> 
        /// </summary>
        public static Dictionary<int, int> CountNonBlackPixelsList (List<object> inputs, float scale = 0.25f, int minValue = 1, bool dispose = false, int? threads = null)
        {
            threads ??= Environment.ProcessorCount;
            var results = new ConcurrentDictionary<int, int>();

            inputs.ParallelForEach(input =>
            {
                int index = inputs.IndexOf(input);
                int count = CountNonBlackPixels(input, scale, minValue, dispose);
                results[index] = count;
            });

            return results.ToDictionary();
        }

        /// <summary> Counts non-black pixels in an image, using a grayscale sum threshold of <paramref name="minValue"/>. </summary>
        public static int CountNonBlackPixels(object input, float scale = 0.25f, int minValue = 1, bool dispose = false)
        {
            var img = GetImage(input);
            int nonBlackCount = 0;

            img.ProcessPixels(scale, p =>
            {
                if (p.GetRgbSum() >= minValue)
                {
                    nonBlackCount++;
                }
            });

            img.DisposeIf(dispose);
            return (int)(nonBlackCount / scale);
        }

        /// <summary> Release ImageSharp memory and run garbage collection. </summary>
        public static void CollectGarbage()
        {
            Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
