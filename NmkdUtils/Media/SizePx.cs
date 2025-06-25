using System.Drawing;

namespace NmkdUtils.Media;

public class SizePx
{
    public int Width { get; } = 0;
    public int Height { get; } = 0;
    public int AxisSum => Width + Height;
    public int TotalPx => Width * Height;

    public SizePx() { }

    public SizePx(Size s)
    {
        Width = s.Width;
        Height = s.Height;
    }

    public SizePx(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static implicit operator SizePx(Size s) => new(s); // Implicit cast from System.Drawing.Size to SizePx
    public static implicit operator Size(SizePx px) => new(px.Width, px.Height); // Implicit cast from SizePx to System.Drawing.Size

    public override string ToString() => $"{Width}x{Height}";
}
