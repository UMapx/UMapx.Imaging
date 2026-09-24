using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using UMapx.Core;
using UMapx.Imaging;
using Xunit;

namespace UMapx.Tests;

[Trait("Category", "Imaging")]
[SupportedOSPlatform("windows")]
public class PhotographicFilterTests
{
    private static readonly string[] Names = { "Curves", "Gradient", "Spin", "Zoom", "Lens", "Dehaze" };

    private static IBitmapFilter Filter(string name, bool neutral = false) => name switch
    {
        "Curves" => neutral ? new CurvesCorrection() : new CurvesCorrection(new[]
        { new PointFloat(0, 0.1f), new PointFloat(0.4f, 0.65f), new PointFloat(1, 0.9f) }),
        "Gradient" => new GradientMap(Color.Navy, Color.Gold) { Strength = neutral ? 0 : 0.75f },
        "Spin" => new RadialBlur(neutral ? 0 : 45, RadialBlurMode.Spin, 17),
        "Zoom" => new RadialBlur(neutral ? 0 : 60, RadialBlurMode.Zoom, 17),
        "Lens" => new LensBlur(neutral ? 0 : 3),
        _ => new Dehaze(2, neutral ? 0 : 0.8f) { RefinementRadius = 2 }
    };

    public static IEnumerable<object[]> IdentityCases() => Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(IdentityCases))]
    public void NeutralSettingsPreserveAllBytesIncludingTransparentRgb(string name)
    {
        using var data = new Buffer32(256, 2, 12, true);
        byte[] before = data.Packed();
        Filter(name, true).Apply(data.Data);
        Assert.Equal(before, data.Packed());
        data.AssertGuards();
    }

    [Fact]
    public void CurvesComposeMasterAndChannelCurvesAndPreserveAlpha()
    {
        using var data = new Buffer32(256, 1);
        for (int x = 0; x < 256; x++) data.Set(x, 0, Color.FromArgb(x, x, x, x));
        var invert = new[] { new PointFloat(0, 1), new PointFloat(1, 0) };
        var curves = new CurvesCorrection(invert) { Red = invert };
        curves.Apply(data.Data);
        for (int x = 0; x < 256; x++)
            Assert.Equal(Color.FromArgb(x, x, 255 - x, 255 - x).ToArgb(), data.Get(x, 0).ToArgb());
    }

    [Fact]
    public void CurvesInterpolateControlPointsWithoutOvershootAndRebuildAfterChanges()
    {
        var points = new[] { new PointFloat(0, 0), new PointFloat(85 / 255f, 0.2f),
            new PointFloat(170 / 255f, 0.8f), new PointFloat(1, 1) };
        var filter = new CurvesCorrection(points);
        points[1] = new PointFloat(0.5f, 1);
        PointFloat[] exposed = filter.Points;
        exposed[1] = new PointFloat(0.5f, 1);
        using var data = new Buffer32(256, 1);
        void Ramp() { for (int x = 0; x < 256; x++) data.Set(x, 0, Color.FromArgb(91, x, x, x)); }
        Ramp();
        filter.Apply(data.Data);
        Assert.Equal(51, data.Get(85, 0).R);
        Assert.Equal(204, data.Get(170, 0).R);
        for (int x = 1; x < 256; x++) Assert.True(data.Get(x, 0).R >= data.Get(x - 1, 0).R);
        filter.Points = new[] { new PointFloat(0, 1), new PointFloat(0.5f, 0), new PointFloat(1, 1) };
        Ramp();
        filter.Apply(data.Data);
        Assert.Equal(255, data.Get(0, 0).R);
        Assert.Equal(0, data.Get(127, 0).R);
        Assert.Equal(255, data.Get(255, 0).R);
    }

    [Fact]
    public void GradientUsesLuminanceStopsStrengthAndPreservesSourceAlpha()
    {
        using var data = new Buffer32(5, 1);
        Color[] original = { Color.FromArgb(19, 0, 0, 0), Color.FromArgb(43, 85, 85, 85),
            Color.FromArgb(71, 255, 255, 255), Color.FromArgb(103, 255, 0, 0), Color.FromArgb(137, 0, 255, 0) };
        var colors = new[] { Color.FromArgb(0, 0, 0, 255), Color.FromArgb(10, 255, 0, 0), Color.White };
        var positions = new[] { 0f, 85 / 255f, 1f };
        var filter = new GradientMap(colors, positions);
        colors[1] = Color.Green; positions[1] = 0.9f;
        for (int x = 0; x < 5; x++) data.Set(x, 0, original[x]);
        filter.Apply(data.Data);
        Assert.Equal(Color.FromArgb(19, 0, 0, 255).ToArgb(), data.Get(0, 0).ToArgb());
        Assert.Equal(Color.FromArgb(43, 255, 0, 0).ToArgb(), data.Get(1, 0).ToArgb());
        Assert.Equal(Color.FromArgb(71, 255, 255, 255).ToArgb(), data.Get(2, 0).ToArgb());
        filter.Colors = new[] { Color.Black, Color.White };
        for (int x = 0; x < 5; x++) data.Set(x, 0, original[x]);
        filter.Apply(data.Data);
        Assert.Equal(54, data.Get(3, 0).R);
        Assert.Equal(182, data.Get(4, 0).R);
        filter.Inverted = true; filter.Strength = 0.5f;
        data.Set(0, 0, original[0]);
        filter.Apply(data.Data);
        Assert.Equal(Color.FromArgb(19, 128, 128, 128).ToArgb(), data.Get(0, 0).ToArgb());
    }

    [Fact]
    public void ZoomSamplesAlongTheRadiusAndDoesNotImportHiddenColors()
    {
        using var data = new Buffer32(3, 1);
        for (int x = 0; x < 3; x++) data.Set(x, 0, Color.FromArgb(x * 100, x * 100, x * 100));
        var filter = new RadialBlur(100, RadialBlurMode.Zoom, 2) { Center = new PointFloat(0, 0) };
        filter.Apply(data.Data);
        Assert.Equal(0, data.Get(0, 0).R);
        Assert.Equal(50, data.Get(1, 0).R);
        Assert.Equal(100, data.Get(2, 0).R);
        data.Set(0, 0, Color.FromArgb(0, 255, 0, 0));
        data.Set(2, 0, Color.Blue);
        filter.Apply(data.Data);
        Assert.Equal(Color.FromArgb(128, 0, 0, 255).ToArgb(), data.Get(2, 0).ToArgb());
    }

    [Fact]
    public void SpinFollowsCircularArcsAndPreservesItsCenter()
    {
        using var data = new Buffer32(3, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) data.Set(x, y, Color.Black);
        data.Set(2, 1, Color.White);
        data.Set(1, 1, Color.FromArgb(77, 10, 20, 30));
        new RadialBlur(90, RadialBlurMode.Spin, 2).Apply(data.Data);
        // The right-hand pixel samples (1+sqrt(.5), 1+-sqrt(.5)), with white weight sqrt(.5)*(1-sqrt(.5)).
        // Use an opaque center for the independent circular sampling check.
        using var opaque = new Buffer32(3, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) opaque.Set(x, y, Color.Black);
        opaque.Set(2, 1, Color.White);
        new RadialBlur(90, RadialBlurMode.Spin, 2).Apply(opaque.Data);
        Assert.Equal((int)Math.Round(255 * (Math.Sqrt(0.5) - 0.5)), opaque.Get(2, 1).R);
        Assert.Equal(Color.FromArgb(77, 10, 20, 30).ToArgb(), data.Get(1, 1).ToArgb());
    }

    [Fact]
    public void CircularLensBlurHasDiskSupportAndRenormalizesBorders()
    {
        using var data = new Buffer32(3, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) data.Set(x, y, Color.Black);
        data.Set(1, 1, Color.White);
        new LensBlur(1).Apply(data.Data);
        Assert.Equal(51, data.Get(1, 1).R);
        Assert.Equal(64, data.Get(1, 0).R);
        Assert.Equal(0, data.Get(0, 0).R);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 0)]
    [InlineData(4, 45)]
    [InlineData(6, 17)]
    public void PolygonLensBlurMatchesIndependentConvexPolygonConvolution(int blades, float rotation)
    {
        const int radius = 3;
        using var data = new Buffer32(9, 7);
        byte[] before = data.Packed();
        new LensBlur(radius, blades) { Rotation = rotation }.Apply(data.Data);
        var vertices = Enumerable.Range(0, blades).Select(i =>
            (x: radius * Math.Cos(rotation * Math.PI / 180 + 2 * Math.PI * i / blades),
             y: radius * Math.Sin(rotation * Math.PI / 180 + 2 * Math.PI * i / blades))).ToArray();
        for (int y = 0; y < 7; y++) for (int x = 0; x < 9; x++)
        {
            double b = 0, g = 0, r = 0, a = 0;
            int count = 0;
            for (int sy = 0; sy < 7; sy++) for (int sx = 0; sx < 9; sx++)
            {
                double dx = sx - x, dy = sy - y;
                bool inside = true;
                for (int i = 0; i < blades; i++)
                {
                    var v = vertices[i]; var next = vertices[(i + 1) % blades];
                    if ((next.x - v.x) * (dy - v.y) - (next.y - v.y) * (dx - v.x) < -1e-8) inside = false;
                }
                if (!inside) continue;
                int k = (sy * 9 + sx) * 4;
                a += before[k + 3]; b += before[k] * before[k + 3];
                g += before[k + 1] * before[k + 3]; r += before[k + 2] * before[k + 3]; count++;
            }
            Assert.Equal(Color.FromArgb(Round(a / count), a == 0 ? 0 : Round(r / a),
                a == 0 ? 0 : Round(g / a), a == 0 ? 0 : Round(b / a)).ToArgb(), data.Get(x, y).ToArgb());
        }
    }

    [Fact]
    public void LensDepthControlsFocusAndIsDefensivelyCopied()
    {
        using var data = new Buffer32(5, 3);
        byte[] before = data.Packed();
        var depth = new float[3, 5];
        var filter = new LensBlur(2) { FocusDepth = 0, DepthMap = depth };
        depth[0, 0] = 1;
        filter.DepthMap[0, 1] = 1;
        filter.Apply(data.Data);
        Assert.Equal(before, data.Packed());
        depth[1, 2] = 1;
        filter.DepthMap = depth;
        filter.Apply(data.Data);
        Assert.Equal(ColorAt(before, 5, 4, 2).ToArgb(), data.Get(4, 2).ToArgb());
        Assert.NotEqual(ColorAt(before, 5, 2, 1).ToArgb(), data.Get(2, 1).ToArgb());
    }

    [Fact]
    public void LensAveragesPremultipliedColorInsteadOfTransparentRgb()
    {
        using var data = new Buffer32(3, 1);
        data.Set(0, 0, Color.FromArgb(0, 255, 0, 0));
        data.Set(1, 0, Color.Blue);
        data.Set(2, 0, Color.FromArgb(0, 0, 255, 0));
        new LensBlur(1).Apply(data.Data);
        Assert.Equal(Color.FromArgb(85, 0, 0, 255).ToArgb(), data.Get(1, 0).ToArgb());
    }

    [Fact]
    public void DehazeRecoversAControlledAtmosphericScatteringImage()
    {
        Color[] clear = { Color.FromArgb(39, 0, 80, 160), Color.FromArgb(75, 160, 0, 40),
            Color.FromArgb(121, 40, 160, 0), Color.FromArgb(199, 0, 120, 60) };
        using var data = new Buffer32(2, 2);
        for (int i = 0; i < clear.Length; i++)
            data.Set(i % 2, i / 2, Color.FromArgb(clear[i].A, 100 + clear[i].R / 2, 100 + clear[i].G / 2, 100 + clear[i].B / 2));
        new Dehaze(1, 1) { AtmosphericLight = Color.FromArgb(200, 200, 200), RefinementRadius = 0 }.Apply(data.Data);
        for (int i = 0; i < clear.Length; i++) Assert.Equal(clear[i].ToArgb(), data.Get(i % 2, i / 2).ToArgb());
    }

    [Fact]
    public void DehazeEstimatesAtmosphericLightWithoutUsingTransparentPixels()
    {
        using var data = new Buffer32(9, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 9; x++)
            data.Set(x, y, x >= 6 ? Color.FromArgb(200, 200, 200) : Color.FromArgb(100, 140, 180));
        data.Set(8, 2, Color.FromArgb(0, 255, 255, 255));
        new Dehaze(1, 1) { RefinementRadius = 0 }.Apply(data.Data);
        Assert.Equal(Color.FromArgb(0, 80, 160).ToArgb(), data.Get(1, 1).ToArgb());
        Assert.Equal(Color.FromArgb(0, 255, 255, 255).ToArgb(), data.Get(8, 2).ToArgb());
    }

    [Theory]
    [InlineData(1, 7, 0, 0)]
    [InlineData(7, 1, 3, 2)]
    [InlineData(5, 4, 1, 0)]
    [InlineData(5, 4, 2, 1)]
    [InlineData(5, 4, 50, 50)]
    public void DehazeMatchesScalarDarkChannelAndCrossGuidedReference(int width, int height, int radius, int refinement)
    {
        using var data = new Buffer32(width, height);
        byte[] before = data.Packed();
        var filter = new Dehaze(radius, 0.7f) { AtmosphericLight = Color.FromArgb(231, 217, 203), RefinementRadius = refinement };
        double[] guide = new double[width * height], t = new double[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int i = y * width + x, k = i * 4;
            guide[i] = before[k + 3] == 0 ? 0 : (0.0722 * before[k] + 0.7152 * before[k + 1] + 0.2126 * before[k + 2]) / 255;
            double min = double.PositiveInfinity;
            for (int sy = Math.Max(0, y - radius); sy <= Math.Min(height - 1, y + radius); sy++)
                for (int sx = Math.Max(0, x - radius); sx <= Math.Min(width - 1, x + radius); sx++)
                {
                    int j = (sy * width + sx) * 4;
                    if (before[j + 3] != 0) min = Math.Min(min, Math.Min(before[j] / 203.0, Math.Min(before[j + 1] / 217.0, before[j + 2] / 231.0)));
                }
            t[i] = double.IsPositiveInfinity(min) ? 1 : Math.Max(0, 1 - 0.7 * min);
        }
        if (refinement > 0)
        {
            double[] mi = BoxMean(guide, width, height, refinement), mp = BoxMean(t, width, height, refinement);
            double[] ii = BoxMean(guide.Select(v => v * v).ToArray(), width, height, refinement);
            double[] ip = BoxMean(guide.Zip(t, (a, b) => a * b).ToArray(), width, height, refinement);
            double[] a = ii.Select((v, i) => (ip[i] - mi[i] * mp[i]) / (v - mi[i] * mi[i] + 0.001)).ToArray();
            double[] b = mp.Select((v, i) => v - a[i] * mi[i]).ToArray();
            a = BoxMean(a, width, height, refinement); b = BoxMean(b, width, height, refinement);
            t = t.Select((v, i) => a[i] * guide[i] + b[i]).ToArray();
        }
        filter.Apply(data.Data);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            Color original = ColorAt(before, width, x, y);
            double transmission = Math.Clamp(t[y * width + x], 0.1, 1);
            Color expected = original.A == 0 ? original : Color.FromArgb(original.A,
                Round((original.R - 231) / transmission + 231), Round((original.G - 217) / transmission + 217),
                Round((original.B - 203) / transmission + 203));
            ImagingAuditTests.Pixel(expected, data.Get(x, y), 1);
        }
    }

    public static IEnumerable<object[]> StrideCases()
    {
        foreach (string name in Names)
            foreach (var size in new[] { (1, 1), (1, 7), (7, 1), (6, 5) })
                foreach (bool negative in new[] { false, true })
                    yield return new object[] { name, size.Item1, size.Item2, negative };
    }

    [Theory]
    [MemberData(nameof(StrideCases))]
    public void AllFiltersHonorSignedStridePaddingAndIndependentDestinationStride(string name, int width, int height, bool negative)
    {
        using var expected = new Buffer32(width, height);
        using var source = new Buffer32(width, height, 20, negative);
        using var destination = new Buffer32(width, height, 12, !negative);
        byte[] original = source.Packed();
        Filter(name).Apply(expected.Data);
        IBitmapFilter filter = Filter(name);
        if (filter is IBitmapFilter2 pair)
        {
            pair.Apply(destination.Data, source.Data);
            Assert.Equal(original, source.Packed());
            Assert.Equal(expected.Packed(), destination.Packed());
        }
        filter.Apply(source.Data);
        Assert.Equal(expected.Packed(), source.Packed());
        source.AssertGuards(); destination.AssertGuards(); expected.AssertGuards();
    }

    [Theory]
    [MemberData(nameof(IdentityCases))]
    public void BitmapOverloadsMatchBufferOverloadsAndReleaseLocks(string name)
    {
        using var data = new Buffer32(7, 5);
        using var source = data.Bitmap();
        using var destination = new Bitmap(7, 5, PixelFormat.Format32bppArgb);
        using var inPlace = (Bitmap)source.Clone();
        IBitmapFilter filter = Filter(name);
        filter.Apply(data.Data);
        filter.Apply(inPlace);
        using var expected = data.Bitmap();
        ImagingAuditTests.Same(expected, inPlace);
        if (filter is IBitmapFilter2 pair)
        {
            pair.Apply(destination, source);
            ImagingAuditTests.Same(expected, destination);
            Assert.Throws<InvalidOperationException>(() => pair.Apply(source, source));
            new CurvesCorrection().Apply(source);
        }
        new CurvesCorrection().Apply(inPlace);
    }

    public static IEnumerable<object[]> UnsupportedCases()
    {
        foreach (string name in Names)
            foreach (PixelFormat format in new[] { PixelFormat.Format24bppRgb, PixelFormat.Format32bppRgb, PixelFormat.Format32bppPArgb, PixelFormat.Format8bppIndexed })
                yield return new object[] { name, format };
    }

    [Theory]
    [MemberData(nameof(UnsupportedCases))]
    public void UnsupportedFormatsAreRejectedByEveryOverload(string name, PixelFormat format)
    {
        IBitmapFilter filter = Filter(name);
        using var unsupported = new Bitmap(3, 2, format);
        using var supported = new Bitmap(3, 2, PixelFormat.Format32bppArgb);
        var invalidData = new BitmapData { Width = 3, Height = 2, PixelFormat = format };
        using var validData = new Buffer32(3, 2);
        Assert.Throws<NotSupportedException>(() => filter.Apply(unsupported));
        Assert.Throws<NotSupportedException>(() => filter.Apply(invalidData));
        if (filter is IBitmapFilter2 pair)
        {
            Assert.Throws<NotSupportedException>(() => pair.Apply(supported, unsupported));
            Assert.Throws<NotSupportedException>(() => pair.Apply(unsupported, supported));
            Assert.Throws<NotSupportedException>(() => pair.Apply(validData.Data, invalidData));
            Assert.Throws<NotSupportedException>(() => pair.Apply(invalidData, validData.Data));
        }
    }

    [Fact]
    public void InvalidParametersAndMismatchedImagesFailWithoutLeavingBitmapLocked()
    {
        Assert.Throws<ArgumentException>(() => new CurvesCorrection(new[] { new PointFloat(0, 0), new PointFloat(0, 1), new PointFloat(1, 1) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CurvesCorrection(new[] { new PointFloat(0, float.NaN), new PointFloat(1, 1) }));
        Assert.Throws<ArgumentException>(() => new GradientMap(new[] { Color.Red }));
        Assert.Throws<ArgumentException>(() => new GradientMap(new[] { Color.Red, Color.Blue }, new[] { 0f, 0.5f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GradientMap { Strength = float.NaN });
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadialBlur(float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadialBlur { Center = new PointFloat(float.NaN, 0) });
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadialBlur { Mode = (RadialBlurMode)9 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadialBlur { Samples = 1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new LensBlur(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LensBlur(1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LensBlur { DepthMap = new[,] { { float.NaN } } });
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dehaze { MinimumTransmission = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dehaze { Epsilon = 0 });
        using var image = new Bitmap(3, 2, PixelFormat.Format32bppArgb);
        var lens = new LensBlur { DepthMap = new float[1, 1] };
        Assert.Throws<ArgumentException>(() => lens.Apply(image));
        lens.DepthMap = null;
        lens.Apply(image);
        using var other = new Bitmap(4, 2, PixelFormat.Format32bppArgb);
        foreach (IBitmapFilter2 filter in new IBitmapFilter2[] { new LensBlur(), new RadialBlur(), new Dehaze() })
            Assert.Throws<ArgumentException>(() => filter.Apply(image, other));
        new GradientMap().Apply(image);
    }

    [Theory]
    [MemberData(nameof(IdentityCases))]
    public void ConstantImagesStayConstantAndFullyTransparentInputsStaySafe(string name)
    {
        using var data = new Buffer32(1, 3);
        Color constant = Color.FromArgb(123, 81, 81, 81);
        for (int y = 0; y < 3; y++) data.Set(0, y, constant);
        IBitmapFilter filter = name == "Gradient" ? new GradientMap() : name == "Curves" ? new CurvesCorrection() : Filter(name);
        filter.Apply(data.Data);
        for (int y = 0; y < 3; y++) Assert.Equal(constant.ToArgb(), data.Get(0, y).ToArgb());
        for (int y = 0; y < 3; y++) data.Set(0, y, Color.FromArgb(0, 0, 0, 0));
        filter.Apply(data.Data);
        for (int y = 0; y < 3; y++) Assert.Equal(0, data.Get(0, y).A);
    }

    private static double[] BoxMean(double[] values, int width, int height, int radius)
    {
        var result = new double[values.Length];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int count = 0;
            for (int sy = Math.Max(0, y - radius); sy <= Math.Min(height - 1, y + radius); sy++)
                for (int sx = Math.Max(0, x - radius); sx <= Math.Min(width - 1, x + radius); sx++)
                { result[y * width + x] += values[sy * width + sx]; count++; }
            result[y * width + x] /= count;
        }
        return result;
    }

    private static int Round(double value) => Math.Clamp((int)Math.Floor(value + 0.5), 0, 255);
    private static Color ColorAt(byte[] bytes, int width, int x, int y)
    {
        int i = (y * width + x) * 4;
        return Color.FromArgb(bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i]);
    }

    private sealed class Buffer32 : IDisposable
    {
        private readonly byte[] bytes;
        private readonly GCHandle pin;
        private readonly int origin;
        private readonly bool[] active;
        public BitmapData Data { get; }

        public Buffer32(int width, int height, int padding = 0, bool negative = false)
        {
            int pitch = width * 4 + padding;
            bytes = Enumerable.Repeat((byte)173, 128 + pitch * height).ToArray();
            active = new bool[bytes.Length];
            origin = 64 + (negative ? (height - 1) * pitch : 0);
            pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Data = new BitmapData { Width = width, Height = height, Stride = negative ? -pitch : pitch,
                Scan0 = IntPtr.Add(pin.AddrOfPinnedObject(), origin), PixelFormat = PixelFormat.Format32bppArgb };
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int k = origin + y * Data.Stride + x * 4;
                for (int c = 0; c < 4; c++) active[k + c] = true;
                Set(x, y, Color.FromArgb((x * 31 + y * 13) % 256, (x * 57 + y * 71 + 19) % 256,
                    (x * 29 + y * 11 + 51) % 256, (x * 17 + y * 43 + 83) % 256));
            }
        }

        public void Set(int x, int y, Color color)
        {
            int k = origin + y * Data.Stride + x * 4;
            bytes[k] = color.B; bytes[k + 1] = color.G; bytes[k + 2] = color.R; bytes[k + 3] = color.A;
        }

        public Color Get(int x, int y)
        {
            int k = origin + y * Data.Stride + x * 4;
            return Color.FromArgb(bytes[k + 3], bytes[k + 2], bytes[k + 1], bytes[k]);
        }

        public byte[] Packed()
        {
            byte[] result = new byte[Data.Width * Data.Height * 4];
            for (int y = 0; y < Data.Height; y++) Array.Copy(bytes, origin + y * Data.Stride, result, y * Data.Width * 4, Data.Width * 4);
            return result;
        }

        public Bitmap Bitmap()
        {
            var image = new Bitmap(Data.Width, Data.Height, PixelFormat.Format32bppArgb);
            BitmapData target = image.Lock32bpp();
            try
            {
                for (int y = 0; y < Data.Height; y++)
                    Marshal.Copy(bytes, origin + y * Data.Stride, IntPtr.Add(target.Scan0, y * target.Stride), Data.Width * 4);
            }
            finally { image.Unlock(target); }
            return image;
        }

        public void AssertGuards()
        {
            for (int i = 0; i < bytes.Length; i++) if (!active[i]) Assert.Equal(173, bytes[i]);
        }

        public void Dispose() { pin.Free(); }
    }
}
