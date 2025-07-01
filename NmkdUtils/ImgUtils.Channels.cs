using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NmkdUtils;
public partial class ImgUtils
{
    public class Channels
    {
        public enum UnwrapMode { Rgb, Rgba, LabPbrN, LabPbrS }
        public enum Channel { R, G, B, A }

        public static void UnwrapImageToAtlas(object input, UnwrapMode mode = UnwrapMode.Rgba)
        {
            using var img = GetImage(input);
            var channelImgs = UnwrapChannels(img, mode);
            string name = channelImgs.Keys.Join("_");
            var stacked = Stack(channelImgs.Values, horizontal: true);
            stacked.SaveImg($"unwrapped_{name}.png", Format.Png, dispose: true);
        }

        // Creates separate R/G/B greyscale images from the original image and stacks them into one.
        public static Dictionary<string, Image> UnwrapChannels(object input, UnwrapMode mode = UnwrapMode.Rgba, bool dispose = true)
        {
            var img = GetImage(input);
            var rgba = img.CloneAs<Rgba32>();
            img.DisposeIf(dispose);
            Dictionary<string, Func<Rgba32, Rgba32>> mapsFuncs = []; // Stores the name of the map/type and the function to construct it.

            if (mode == UnwrapMode.Rgb || mode == UnwrapMode.Rgba)
            {
                mapsFuncs["r"] = p => p.Remap("rrr");
                mapsFuncs["g"] = p => p.Remap("ggg");
                mapsFuncs["b"] = p => p.Remap("bbb");
            }
            if (mode == UnwrapMode.Rgba)
            {
                mapsFuncs["a"] = p => p.Remap("aaa");
            }
            if (mode == UnwrapMode.LabPbrN) // Minecraft LabPBR Normal Map
            {
                mapsFuncs["n"] = p => p.Remap("rg"); // Normals = Red/Green
                mapsFuncs["occ"] = p => p.Remap("bbb"); // Occlusion = Blue
                mapsFuncs["pom"] = p => p.Remap("aaa"); // POM/Depth = Alpha
            }
            if (mode == UnwrapMode.LabPbrS) // Minecraft LabPBR Specular Map
            {
                mapsFuncs["smooth"] = p => p.Remap("rrr"); // R = Perceptual smoothness
                mapsFuncs["f0"] = p => p.Remap("ggg"); // G = F0 aka Reflectance
                mapsFuncs["psss"] = p => p.B < 65 ? p.Remap("bbb") : new Rgba32(p.B - 65, p.B - 65, p.B - 65); // B 0-64 = Porosity; B 65-255 = Subsurface scattering
                mapsFuncs["em"] = p => p.Remap("aaa"); // A = Emission
            }

            var channelImages = new Dictionary<string, Image>();

            foreach (var kvp in mapsFuncs)
            {
                Func<Rgba32, Rgba32> channelFunc = kvp.Value;
                var channelImage = new Image<Rgba32>(rgba.Width, rgba.Height);

                rgba.ProcessPixels(1f, (px, x, y) => channelImage[x, y] = channelFunc(px)); // Set each pixel in the channel image using the provided function on the original pixel

                // TODO: Remove once tested
                // rgba.ProcessPixelRows(accessorSource =>
                // {
                //     for (int y = 0; y < rgba.Height; y++)
                //     {
                //         var currentRow = accessorSource.GetRowSpan(y);
                // 
                //         for (int x = 0; x < rgba.Width; x++)
                //         {
                //             Rgba32 px = currentRow[x];
                //             channelImage[x, y] = channelFunc(px);
                //         }
                //     }
                // });

                channelImages[kvp.Key] = channelImage;
            }

            rgba.Dispose();
            return channelImages;
        }

        /// <summary>
        /// Writes the provided channel image into channel <paramref name="c"/> of <paramref name="img"/>. <br/> 
        /// The <paramref name="imgChannel"/> is excpected to be greyscale, the value of R will be used when writing the channel, G/B/A are ignored.
        /// </summary>
        public static void SetChannel(Channel c, Image<Rgba32> img, Image<Rgba32> imgChannel, bool disposeChannelImg = true)
        {
            Rgba32[,] newPixels = new Rgba32[img.Width, img.Height];

            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var greyscaleValue = imgChannel[x, y].R; // Use the red channel as grayscale value

                    if (c == Channel.R)
                        newPixels[x, y] = new Rgba32(greyscaleValue, img[x, y].G, img[x, y].B, img[x, y].A);
                    else if (c == Channel.G)
                        newPixels[x, y] = new Rgba32(img[x, y].R, greyscaleValue, img[x, y].B, img[x, y].A);
                    else if (c == Channel.B)
                        newPixels[x, y] = new Rgba32(img[x, y].R, img[x, y].G, greyscaleValue, img[x, y].A);
                    else if (c == Channel.A)
                        newPixels[x, y] = new Rgba32(img[x, y].R, img[x, y].G, img[x, y].B, greyscaleValue);
                }
            }

            img.ProcessPixelRows(accessorSource =>
            {
                for (int y = 0; y < img.Height; y++)
                {
                    var currentRow = accessorSource.GetRowSpan(y);

                    for (int x = 0; x < img.Width; x++)
                    {
                        img[x, y] = newPixels[x, y];
                    }
                }
            });

            imgChannel.DisposeIf(disposeChannelImg);
        }

        /// <summary> Extracts/remaps channels with a simple selection syntax: r/g/b/a = channel, 1 = white, 0 = black. Clamping can be applied with <paramref name="normFrom"/> and <paramref name="normTo"/>. </summary>
        public static Rgba32 Remap(Rgba32 px, string channels = "rgba", byte normFrom = 0, byte normTo = 255)
        {
            // Pad channels to 4, filling with 1 (white) if necessary
            channels = channels.PadRight(4, '1');

            // Upperase to invert
            byte GetChannelValue(char channel, byte fallback)
            {
                return channel switch
                {
                    'r' => px.R,
                    'g' => px.G,
                    'b' => px.B,
                    'a' => px.A,
                    'R' => (byte)(255 - px.R),
                    'G' => (byte)(255 - px.G),
                    'B' => (byte)(255 - px.B),
                    'A' => (byte)(255 - px.A),
                    '0' => 0,
                    '1' => 255,
                    _ => fallback
                };
            }

            byte r = byte.Clamp((byte)GetChannelValue(channels[0], px.R), normFrom, normTo);
            byte g = byte.Clamp((byte)GetChannelValue(channels[1], px.G), normFrom, normTo);
            byte b = byte.Clamp((byte)GetChannelValue(channels[2], px.B), normFrom, normTo);
            byte a = byte.Clamp((byte)GetChannelValue(channels[3], px.A), normFrom, normTo);
            return new Rgba32(r, g, b, a);
        }
    }
}
