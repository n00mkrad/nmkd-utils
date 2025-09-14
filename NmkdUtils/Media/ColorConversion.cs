using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace NmkdUtils.Media;
public class ColorConversion
{
    /// <summary>
    /// Converts one YUV420p10le frame (contiguous Y|U|V planes, 10-bit in 16-bit little-endian) to an Rgba32 ImageSharp Image.
    /// </summary>
    public static Image<Rgba32> ImageFromYuv420P10LeBytes(ReadOnlySpan<byte> buffer, int width, int height, bool limitedRange = true)
    {
        if ((width & 1) != 0 || (height & 1) != 0)
            throw new ArgumentException("YUV420p requires even width and height.");

        int yPlaneSize = width * height * 2;
        int cW = width / 2, cH = height / 2;
        int cPlaneSize = cW * cH * 2;
        if (buffer.Length < yPlaneSize + 2 * cPlaneSize)
            throw new ArgumentException("Buffer size does not match YUV420p10le frame.");

        var yPlane = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, ushort>(buffer[..yPlaneSize]);
        var uPlane = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, ushort>(buffer.Slice(yPlaneSize, cPlaneSize));
        var vPlane = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, ushort>(buffer.Slice(yPlaneSize + cPlaneSize, cPlaneSize));

        // Coefficients for color conversion. These are for 8-bit, will be adapted.
        int yMul, yOff, rV, gU, gV, bU;
        if (limitedRange)
        {
            yMul = 298; yOff = 16 << 2; // 10-bit offset
            rV = 459; gU = -55; gV = -136; bU = 541; // BT.709
        }
        else
        {
            yMul = 256; yOff = 0; // Full range
            rV = 359; gU = -88; gV = -183; bU = 453;
        }

        static byte Clip(int x) => (byte)(x < 0 ? 0 : (x > 255 ? 255 : x));

        var img = new Image<Rgba32>(width, height);

        for (int y = 0; y < height; y += 2)
        {
            var row0 = img.DangerousGetPixelRowMemory(y).Span;
            var row1 = img.DangerousGetPixelRowMemory(y + 1).Span;

            int yRow0 = y * width;
            int yRow1 = (y + 1) * width;
            int cRow = (y / 2) * cW;

            for (int x = 0; x < width; x += 2)
            {
                int cIdx = cRow + (x / 2);
                int U = uPlane[cIdx] - 512; // 10-bit center is 512
                int V = vPlane[cIdx] - 512;

                int rC = rV * V;
                int gC = gU * U + gV * V;
                int bC = bU * U;

                // Y samples for 2x2 block, scaled down to 8-bit equivalent for matrix
                int C00 = (yPlane[yRow0 + x] - yOff) * yMul;
                int C01 = (yPlane[yRow0 + x + 1] - yOff) * yMul;
                int C10 = (yPlane[yRow1 + x] - yOff) * yMul;
                int C11 = (yPlane[yRow1 + x + 1] - yOff) * yMul;

                // Upscale result by >> 6 instead of >> 8 to account for 10-bit Y and 8-bit coefficients
                // Final shift is 8 (for coeffs) + 2 (for 10-bit Y) = 10.
                // C*yMul is already 18-bit, C*rV is 19-bit.
                // ( (Y-yOff)*yMul + rC ) >> 10
                const int shift = 10;
                const int rounder = 1 << (shift - 1);

                // Top-left
                row0[x] = new Rgba32(
                    Clip((C00 + rC + rounder) >> shift),
                    Clip((C00 + gC + rounder) >> shift),
                    Clip((C00 + bC + rounder) >> shift), 255);
                // Top-right
                row0[x + 1] = new Rgba32(
                    Clip((C01 + rC + rounder) >> shift),
                    Clip((C01 + gC + rounder) >> shift),
                    Clip((C01 + bC + rounder) >> shift), 255);
                // Bottom-left
                row1[x] = new Rgba32(
                    Clip((C10 + rC + rounder) >> shift),
                    Clip((C10 + gC + rounder) >> shift),
                    Clip((C10 + bC + rounder) >> shift), 255);
                // Bottom-right
                row1[x + 1] = new Rgba32(
                    Clip((C11 + rC + rounder) >> shift),
                    Clip((C11 + gC + rounder) >> shift),
                    Clip((C11 + bC + rounder) >> shift), 255);
            }
        }
        return img;
    }

    /// <summary>
    /// Converts one YUV420p frame (contiguous Y|U|V planes) to an Rgba32 ImageSharp Image.
    /// buffer.Length must be width*height*3/2. Width/Height should be even.
    /// </summary>
    public static Image<Rgba32> ImageFromYuv420Bytes(ReadOnlySpan<byte> buffer, int width, int height, bool bt709 = false, bool limitedRange = true)
    {
        if ((width & 1) != 0 || (height & 1) != 0)
            throw new ArgumentException("YUV420p requires even width and height.");

        int ySize = width * height;
        int cW = width / 2, cH = height / 2;
        int cSize = cW * cH;
        if (buffer.Length < ySize + 2 * cSize)
            throw new ArgumentException("Buffer size does not match YUV420p frame.");

        var yPlane = buffer[..ySize];
        var uPlane = buffer.Slice(ySize, cSize);
        var vPlane = buffer.Slice(ySize + cSize, cSize);

        // Coefficients (fixed-point, scale = 256)
        int yMul, yOff, rV, gU, gV, bU;
        if (limitedRange)
        {
            yMul = 298; yOff = 16;
            if (bt709) { rV = 459; gU = -55; gV = -136; bU = 541; }        // BT.709
            else { rV = 409; gU = -100; gV = -208; bU = 516; }       // BT.601
        }
        else
        {
            yMul = 256; yOff = 0;   // JPEG/full range
            rV = 359; gU = -88; gV = -183; bU = 453;
        }

        static byte Clip(int x) => (byte)(x < 0 ? 0 : (x > 255 ? 255 : x));

        var img = new Image<Rgba32>(width, height);

        for (int y = 0; y < height; y += 2)
        {
            var row0 = img.DangerousGetPixelRowMemory(y).Span;
            var row1 = img.DangerousGetPixelRowMemory(y + 1).Span;

            int yRow0 = y * width;
            int yRow1 = (y + 1) * width;
            int cRow = (y / 2) * cW;

            for (int x = 0; x < width; x += 2)
            {
                int cIdx = cRow + (x / 2);
                int U = uPlane[cIdx] - 128;
                int V = vPlane[cIdx] - 128;

                // Precompute chroma terms
                int rC = rV * V;
                int gC = gU * U + gV * V;
                int bC = bU * U;

                // Y samples for 2x2 block
                int C00 = (yPlane[yRow0 + x] - yOff) * yMul;
                int C01 = (yPlane[yRow0 + x + 1] - yOff) * yMul;
                int C10 = (yPlane[yRow1 + x] - yOff) * yMul;
                int C11 = (yPlane[yRow1 + x + 1] - yOff) * yMul;

                // Top-left
                {
                    int R = (C00 + rC + 128) >> 8;
                    int G = (C00 + gC + 128) >> 8;
                    int B = (C00 + bC + 128) >> 8;
                    row0[x] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
                // Top-right
                {
                    int R = (C01 + rC + 128) >> 8;
                    int G = (C01 + gC + 128) >> 8;
                    int B = (C01 + bC + 128) >> 8;
                    row0[x + 1] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
                // Bottom-left
                {
                    int R = (C10 + rC + 128) >> 8;
                    int G = (C10 + gC + 128) >> 8;
                    int B = (C10 + bC + 128) >> 8;
                    row1[x] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
                // Bottom-right
                {
                    int R = (C11 + rC + 128) >> 8;
                    int G = (C11 + gC + 128) >> 8;
                    int B = (C11 + bC + 128) >> 8;
                    row1[x + 1] = new Rgba32(Clip(R), Clip(G), Clip(B), 255);
                }
            }
        }

        return img; // caller owns/Dispose() when done
    }

    /// <summary> Converts a PQ-encoded normalized signal (E′) to absolute display luminance per SMPTE ST 2084. </summary>
    private static double ST2084_EOTF(double E) // E in [0,1], returns nits (cd/m^2)
    {
        const double m1 = 2610.0 / 16384.0;
        const double m2 = 2523.0 / 32.0;
        const double c1 = 3424.0 / 4096.0;
        const double c2 = 2413.0 / 128.0;
        const double c3 = 2392.0 / 128.0;

        E = Math.Clamp(E, 0.0, 1.0);
        double p = Math.Pow(E, 1.0 / m2);
        double num = Math.Max(p - c1, 0.0);
        double den = c2 - c3 * p;
        // Guard against den <= 0 due to numeric edge cases near E=1
        if (den <= 0.0 || num <= 0.0) return (E >= 1.0) ? 10000.0 : 0.0;
        return 10000.0 * Math.Pow(num / den, 1.0 / m1);
    }

    /// <inheritdoc cref="RgbToNitsPq(byte, byte, byte)"/>
    public static float RgbToNitsPq(Rgba32 rgba) => RgbToNitsPq(rgba.R, rgba.G, rgba.B);
    /// <summary> Computes BT.2020 luminance (Y) in nits from an 8-bit RGBA pixel that contains PQ-encoded RGB. </summary>
    public static float RgbToNitsPq(byte r, byte g, byte b)
    {
        // IMPORTANT: The texture/buffer must be read as UNORM (no sRGB decode).
        double Rp = r / 255.0;
        double Gp = g / 255.0;
        double Bp = b / 255.0;

        double Rn = ST2084_EOTF(Rp);
        double Gn = ST2084_EOTF(Gp);
        double Bn = ST2084_EOTF(Bp);

        // BT.2020 luminance
        return (float)(0.2627 * Rn + 0.6780 * Gn + 0.0593 * Bn); // cd/m²
    }
}
