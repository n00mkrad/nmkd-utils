using System.Diagnostics;
using System.Drawing;

namespace NmkdUtils.Media
{
    [DebuggerDisplay("X {X} - Y {Y} - {Width}x{Height}")]
    public class Area
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Point TopLeft => new(X, Y);
        public Size Size => new(Width, Height);

        public Area() { }

        public Area(int w, int h, int x, int y)
        {
            Width = w;
            Height = h;
            X = x;
            Y = y;
        }

        public static Area FromCropdetect(string cropdetectOutput, int paddingSides = 0, int paddingTopBot = 0, int minHeight = 0)
        {
            string crop = cropdetectOutput.SplitIntoLines().Where(l => l.Contains(" crop=") && l.Contains("Parsed_cropdetect")).Last().Split(" crop=").Last();
            var cropSplit = crop.Split(':').Select(c => c.GetInt()).ToArray();
            var a = new Area(cropSplit[0], cropSplit[1], cropSplit[2], cropSplit[3]);

            a.ApplyMinHeight(minHeight);
            a.ApplyPadding(paddingSides, paddingTopBot);
            return a;
        }

        // Convert ImageSharp Rectangle to System.Drawing.Rectangle
        public static Area FromRect(SixLabors.ImageSharp.Rectangle rect, int paddingSides = 0, int paddingTopBot = 0, int minHeight = 0) => 
            FromRect((new Rectangle(rect.X, rect.Y, rect.Width, rect.Height)), paddingSides, paddingTopBot, minHeight);

        public static Area FromRect(Rectangle rect, int paddingSides = 0, int paddingTopBot = 0, int minHeight = 0)
        {
            var a = new Area(rect.Width, rect.Height, rect.X, rect.Y);
            a.ApplyMinHeight(minHeight);
            a.ApplyPadding(paddingSides, paddingTopBot);
            return a;
        }

        public void ApplyMinHeight(int minHeight)
        {
            if (minHeight > 0 && Height < minHeight)
            {
                int diff = (minHeight * 2) - Height;
                Height = minHeight;
                Y -= (diff / 2f).Round(MathExtensions.Rounding.Up);
            }
        }

        public void ApplyPadding(int paddingSides, int paddingTopBot)
        {
            if (paddingSides < 0 || paddingTopBot < 0)
                return;

            X -= paddingSides;
            Y -= paddingTopBot;
            Width += (paddingSides * 2);
            Height += (paddingTopBot * 2);
        }

        public string GetCropFilter()
        {
            return $"crop={Width}:{Height}:{Math.Max(0, X)}:{Math.Max(0, Y)}";
        }

        public string GetCropFilterCentered(int totalWidth, int totalHeight)
        {
            int x = (totalWidth - Width) / 2;
            int y = (totalHeight - Height) / 2;
            return $"crop={Width}:{Height}:{x}:{y}";
        }
    }
}
