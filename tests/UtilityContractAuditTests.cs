using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.Versioning;
using UMapx.Core;
using UMapx.Imaging;
using Xunit;
using static UMapx.Tests.NumericAssert;

namespace UMapx.Tests;

[Trait("Category","Contract")]
public class UtilityContractAuditTests
{
    // Independent cubic Hermite polynomial with centered endpoint slopes.
    private static Complex Cubic(Complex a, Complex b, Complex c, Complex d, double t) =>
        ((.5 * (-a + 3 * b - 3 * c + d) * t + a - 2.5 * b + 2 * c - .5 * d) * t + .5 * (c - a)) * t + b;

    private static Complex Resample(Complex[,] input, double y, double x)
    {
        int iy = (int)Math.Floor(y), ix = (int)Math.Floor(x);
        Complex At(int i, int j) => input[Math.Clamp(i, 0, input.GetLength(0) - 1), Math.Clamp(j, 0, input.GetLength(1) - 1)];
        Complex Row(int i) => Cubic(At(i, ix - 1), At(i, ix), At(i, ix + 1), At(i, ix + 2), x - ix);
        return Cubic(Row(iy - 1), Row(iy), Row(iy + 1), Row(iy + 2), y - iy);
    }

    [Fact]
    public void XmlRoundTripPreservesNumericArrayValues()
    {
        float[] expected={-1.25f,0,1e-30f,1e30f};using var stream=new MemoryStream();Xml.Save(stream,expected);stream.Position=0;Assert.Equal(expected,(float[])Xml.Open(stream,typeof(float[])));
    }
    [Theory] [InlineData(255)] [InlineData(256)] [InlineData(300)]
    public void DepthHistogramEqualizationCountsMoreThan65535PixelsWithoutOverflow(int side)
    {
        var depth=new ushort[side,side];var actual=depth.Equalize();foreach(ushort value in actual)Assert.Equal(ushort.MaxValue,value);
    }
	[Theory]
    [InlineData(1, 1)] [InlineData(3, 5)] [InlineData(7, 9)]
    [SupportedOSPlatform("windows")]
    public void BitmapBicubicResizeMatchesIndependentChannelInterpolation(int height, int width)
    {
        using var input = new Bitmap(5, 3, PixelFormat.Format32bppArgb);
        using var output = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var planes = Enumerable.Range(0, 4).Select(_ => new Complex[3, 5]).ToArray();
        for (int i = 0; i < 3; i++) for (int j = 0; j < 5; j++)
        {
            var color = Color.FromArgb(70 + i * 20 + j * 7, i * 45 + j * 17, 180 - i * 12 - j * 21, 20 + i * 25 + j * 31);
            input.SetPixel(j, i, color);
            int[] channels = { color.A, color.R, color.G, color.B };
            for (int k = 0; k < 4; k++) planes[k][i, j] = channels[k];
        }
        new UMapx.Imaging.Resize(width, height, InterpolationMode.Bicubic).Apply(output, input);
        for (int i = 0; i < height; i++) for (int j = 0; j < width; j++)
        {
            var c = output.GetPixel(j, i); int[] channels = { c.A, c.R, c.G, c.B };
            for (int k = 0; k < 4; k++)
            {
                double expected = Math.Clamp(Resample(planes[k], (i + .5) * 3 / height - .5, (j + .5) * 5 / width - .5).Real, 0, 255);
                Close(expected, channels[k], 1.001, 0);
            }
        }
    }
}
