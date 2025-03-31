using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using HeyRed.ImageSharp.Heif.Formats.Heif;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using HeyRed.ImageSharp.Heif;

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
    }
}
