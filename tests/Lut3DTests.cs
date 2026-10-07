using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using UMapx.Core;
using UMapx.Imaging;
using Xunit;

namespace UMapx.Tests;

[Trait("Category", "Imaging")]
[SupportedOSPlatform("windows")]
public class Lut3DTests
{
    public static IEnumerable<object[]> IdentityCases()
    {
        foreach (Lut3DInterpolation interpolation in Enum.GetValues<Lut3DInterpolation>())
            foreach (int size in new[] { 2, 3, 7, 17, 33, 65 })
                foreach (bool negative in new[] { false, true })
                    yield return new object[] { interpolation, size, negative };
    }

    [Theory]
    [MemberData(nameof(IdentityCases))]
    public void IdentityPreservesEveryByteAndSignedStride(Lut3DInterpolation interpolation, int size, bool negative)
    {
        using var data = new Buffer32(256, 3, negative);
        byte[] before = data.Bytes.ToArray();
        new Lut3D(size) { Interpolation = interpolation }.Apply(data.Data);
        Assert.Equal(before, data.Bytes);
    }

    [Theory]
    [InlineData(Lut3DInterpolation.Trilinear)]
    [InlineData(Lut3DInterpolation.Tetrahedral)]
    public void ZeroStrengthPreservesTransparentRgb(Lut3DInterpolation interpolation)
    {
        using var data = new Buffer32(256, 3, true);
        byte[] before = data.Bytes.ToArray();
        new Lut3D(RandomTable(5)) { Interpolation = interpolation, Strength = 0 }.Apply(data.Data);
        Assert.Equal(before, data.Bytes);
    }

    [Theory]
    [InlineData(Lut3DInterpolation.Trilinear, 2, 1f)]
    [InlineData(Lut3DInterpolation.Tetrahedral, 2, 1f)]
    [InlineData(Lut3DInterpolation.Trilinear, 5, 0.35f)]
    [InlineData(Lut3DInterpolation.Tetrahedral, 5, 0.35f)]
    public void NonlinearTablesMatchIndependentCornerWeights(Lut3DInterpolation interpolation, int size, float strength)
    {
        using var data = new Buffer32(256, 7, true);
        float[,,,] table = RandomTable(size);
        var filter = new Lut3D(table) { Interpolation = interpolation, Strength = strength };
        Color[] before = data.Pixels();
        filter.Apply(data.Data);
        for (int i = 0; i < before.Length; i++)
        {
            Color expected = Reference(table, before[i], interpolation, strength, new[] { 0f, 0f, 0f }, new[] { 1f, 1f, 1f });
            ImagingAuditTests.Pixel(expected, data.Get(i % 256, i / 256), 1);
            Assert.Equal(before[i].A, data.Get(i % 256, i / 256).A);
        }
        data.AssertGuards();
    }

    [Theory]
    [InlineData(204, 128, 51)]
    [InlineData(204, 51, 128)]
    [InlineData(128, 204, 51)]
    [InlineData(51, 204, 128)]
    [InlineData(128, 51, 204)]
    [InlineData(51, 128, 204)]
    [InlineData(128, 128, 51)]
    [InlineData(128, 51, 128)]
    [InlineData(51, 128, 128)]
    [InlineData(128, 128, 128)]
    [InlineData(255, 255, 255)]
    [InlineData(0, 0, 0)]
    public void TetrahedraAndSharedFacesUseTheCorrectFourCorners(int red, int green, int blue)
    {
        using var data = new Buffer32(1, 1);
        Color input = Color.FromArgb(37, red, green, blue);
        data.Set(0, 0, input);
        float[,,,] table = RandomTable(2);
        new Lut3D(table).Apply(data.Data);
        Color expected = Reference(table, input, Lut3DInterpolation.Tetrahedral, 1, new[] { 0f, 0f, 0f }, new[] { 1f, 1f, 1f });
        ImagingAuditTests.Pixel(expected, data.Get(0, 0), 1);
        Assert.Equal(37, data.Get(0, 0).A);
    }

    [Theory]
    [InlineData(Lut3DInterpolation.Trilinear, 20)]
    [InlineData(Lut3DInterpolation.Tetrahedral, 51)]
    public void InterpolationsProduceTheirAnalyticNonlinearResults(Lut3DInterpolation interpolation, int expected)
    {
        var table = new float[2, 2, 2, 3];
        for (int c = 0; c < 3; c++) table[1, 1, 1, c] = 1;
        using var data = new Buffer32(1, 1);
        data.Set(0, 0, Color.FromArgb(0, 204, 128, 51));
        new Lut3D(table) { Interpolation = interpolation }.Apply(data.Data);
        Assert.Equal(Color.FromArgb(0, expected, expected, expected).ToArgb(), data.Get(0, 0).ToArgb());
    }

    [Theory]
    [InlineData(Lut3DInterpolation.Trilinear)]
    [InlineData(Lut3DInterpolation.Tetrahedral)]
    public void CustomDomainsClampInputPerChannelAndRebuildAfterChanges(Lut3DInterpolation interpolation)
    {
        using var data = new Buffer32(256, 2);
        var min = new[] { -0.25f, 0.2f, 0f };
        var max = new[] { 0.75f, 0.8f, 1.5f };
        var filter = new Lut3D(5) { Interpolation = interpolation };
        float[,,,] table = filter.Table;
        filter.Apply(data.Data);
        filter.DomainMin = min;
        filter.DomainMax = max;
        Color[] before = data.Pixels();
        filter.Apply(data.Data);
        for (int i = 0; i < before.Length; i++)
            ImagingAuditTests.Pixel(Reference(table, before[i], interpolation, 1, min, max), data.Get(i % 256, i / 256), 1);
        data.AssertGuards();
    }

    [Fact]
    public void TableAndDomainArraysAreCopiedAndReplacingTheTableRebuildsItsSize()
    {
        float[,,,] original = new Lut3D(3).Table;
        var min = new[] { 0f, 0f, 0f };
        var max = new[] { 1f, 1f, 1f };
        var filter = new Lut3D(original) { DomainMin = min, DomainMax = max };
        original[2, 2, 2, 0] = 0; min[0] = 1; max[0] = 2;
        filter.Table[2, 2, 2, 0] = 0; filter.DomainMin[0] = 1; filter.DomainMax[0] = 2;
        using var data = new Buffer32(1, 1);
        data.Set(0, 0, Color.White);
        filter.Apply(data.Data);
        Assert.Equal(Color.White.ToArgb(), data.Get(0, 0).ToArgb());
        filter.Table = new float[2, 2, 2, 3];
        Assert.Equal(2, filter.Size);
        filter.Apply(data.Data);
        Assert.Equal(Color.Black.ToArgb(), data.Get(0, 0).ToArgb());
    }

    [Theory]
    [InlineData(Lut3DInterpolation.Trilinear)]
    [InlineData(Lut3DInterpolation.Tetrahedral)]
    public void OutputIsClampedAfterBlendingAndAlphaIsUnchanged(Lut3DInterpolation interpolation)
    {
        var table = new float[2, 2, 2, 3];
        for (int r = 0; r < 2; r++) for (int g = 0; g < 2; g++) for (int b = 0; b < 2; b++)
        { table[r, g, b, 0] = 2; table[r, g, b, 1] = -1; table[r, g, b, 2] = 0.5f; }
        using var data = new Buffer32(1, 1);
        data.Set(0, 0, Color.FromArgb(17, 102, 102, 102));
        var filter = new Lut3D(table) { Interpolation = interpolation, Strength = 0.25f };
        filter.Apply(data.Data);
        Assert.Equal(Color.FromArgb(17, 204, 12, 108).ToArgb(), data.Get(0, 0).ToArgb());
        filter.Strength = 1;
        filter.Apply(data.Data);
        Assert.Equal(Color.FromArgb(17, 255, 0, 127).ToArgb(), data.Get(0, 0).ToArgb());
    }

    [Fact]
    public void CubeReaderUsesRedFastestOrderInvariantNumbersAndQuotedTitles()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            float[,,,] table = RandomTable(2);
            string text = "\uFEFF \tTITLE \"Look #1\" # comment\n# generated fixture\nDOMAIN_MAX 1 1 1\nDOMAIN_MIN 0 0 0\n" + Cube(table);
            using var reader = new StringReader(text);
            Lut3D filter = Lut3D.FromCube(reader);
            Assert.Equal("Look #1", filter.Title);
            Assert.Equal(2, filter.Size);
            Assert.Equal(table.Cast<float>(), filter.Table.Cast<float>());
            Assert.Equal(-1, reader.Peek());
            using var data = new Buffer32(256, 2, true);
            Color[] before = data.Pixels();
            filter.Apply(data.Data);
            for (int i = 0; i < before.Length; i++)
                ImagingAuditTests.Pixel(Reference(table, before[i], filter.Interpolation, 1, filter.DomainMin, filter.DomainMax),
                    data.Get(i % 256, i / 256), 1);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void CubeInputRangeExtensionMapsAllAxes()
    {
        using var reader = new StringReader("LUT_3D_INPUT_RANGE 2.5e-1 7.5e-1\n" + Cube(new Lut3D().Table));
        Lut3D filter = Lut3D.FromCube(reader);
        using var data = new Buffer32(3, 1);
        data.Set(0, 0, Color.FromArgb(7, 0, 0, 0));
        data.Set(1, 0, Color.FromArgb(11, 128, 128, 128));
        data.Set(2, 0, Color.FromArgb(19, 255, 255, 255));
        filter.Apply(data.Data);
        Assert.Equal(Color.FromArgb(7, 0, 0, 0).ToArgb(), data.Get(0, 0).ToArgb());
        Assert.Equal(Color.FromArgb(11, 128, 128, 128).ToArgb(), data.Get(1, 0).ToArgb());
        Assert.Equal(Color.FromArgb(19, 255, 255, 255).ToArgb(), data.Get(2, 0).ToArgb());
    }

    [Theory]
    [InlineData("LUT_3D_SIZE 1\n")]
    [InlineData("LUT_3D_SIZE 257\n")]
    [InlineData("LUT_3D_SIZE 2\n0 0 0\n")]
    [InlineData("LUT_3D_SIZE 2\nLUT_3D_SIZE 2\n")]
    [InlineData("LUT_3D_SIZE 2\nNaN 0 0\n")]
    [InlineData("LUT_3D_SIZE 2\nInfinity 0 0\n")]
    [InlineData("LUT_3D_SIZE 2\n0,5 0 0\n")]
    [InlineData("TITLE missing-quotes\n")]
    public void MalformedCubeDataIsRejected(string text)
    {
        using var reader = new StringReader(text);
        Assert.Throws<FormatException>(() => Lut3D.FromCube(reader));
        reader.Peek();
    }

    [Theory]
    [InlineData("DOMAIN_MIN 1 0 0\n")]
    [InlineData("DOMAIN_MIN 2 0 0\n")]
    [InlineData("DOMAIN_MIN 0 0 0\nDOMAIN_MIN 0 0 0\n")]
    [InlineData("LUT_3D_INPUT_RANGE 0 1\nDOMAIN_MAX 1 1 1\n")]
    [InlineData("UNKNOWN_TAG 2\n")]
    public void AmbiguousDomainsAndUnknownHeadersAreRejected(string header)
    {
        using var reader = new StringReader(header + Cube(new Lut3D().Table));
        Assert.Throws<FormatException>(() => Lut3D.FromCube(reader));
    }

    [Fact]
    public void ExtraCubeEntriesAndLateHeadersAreRejected()
    {
        string cube = Cube(new Lut3D().Table);
        foreach (string tail in new[] { "0 0 0\n", "DOMAIN_MIN 0 0 0\n" })
        {
            using var reader = new StringReader(cube + tail);
            Assert.Throws<FormatException>(() => Lut3D.FromCube(reader));
        }
    }

    [Theory]
    [InlineData("LUT_1D_SIZE 2\n")]
    [InlineData("LUT_3D_SIZE 2\nLUT_1D_SIZE 2\n")]
    public void OneDimensionalAndCombinedCubesAreExplicitlyUnsupported(string text)
    {
        using var reader = new StringReader(text);
        Assert.Throws<NotSupportedException>(() => Lut3D.FromCube(reader));
    }

    [Fact]
    public void FileReaderIsReleasedOnSuccessAndOnParseFailure()
    {
        string path = Path.Combine(Path.GetTempPath(), "umapx-lut-" + Guid.NewGuid().ToString("N") + ".cube");
        try
        {
            File.WriteAllText(path, Cube(new Lut3D().Table));
            Assert.Equal(2, Lut3D.FromCube(path).Size);
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            File.WriteAllText(path, "LUT_3D_SIZE 2\n");
            Assert.Throws<FormatException>(() => Lut3D.FromCube(path));
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(PixelFormat.Format24bppRgb)]
    [InlineData(PixelFormat.Format32bppRgb)]
    [InlineData(PixelFormat.Format32bppPArgb)]
    [InlineData(PixelFormat.Format8bppIndexed)]
    public void BufferFormatsAreCheckedEvenAtZeroStrength(PixelFormat format)
    {
        using var data = new Buffer32(3, 2);
        data.Data.PixelFormat = format;
        foreach (float strength in new[] { 0f, 1f })
            Assert.Throws<NotSupportedException>(() => new Lut3D { Strength = strength }.Apply(data.Data));
    }

    [Theory]
    [InlineData(Lut3DInterpolation.Trilinear)]
    [InlineData(Lut3DInterpolation.Tetrahedral)]
    public void BitmapOverloadConverts24BitImagesAndReleasesItsLock(Lut3DInterpolation interpolation)
    {
        using var image = new Bitmap(13, 7, PixelFormat.Format24bppRgb);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 13; x++) image.SetPixel(x, y, Color.FromArgb(x * 19, y * 37, (x + y) * 13));
        using var expected = image.To32bpp();
        var filter = new Lut3D(RandomTable(3)) { Interpolation = interpolation };
        filter.Apply(expected);
        filter.Apply(image);
        ImagingAuditTests.Same(expected, image);
        new Lut3D().Apply(image);
        filter.Table = new float[2, 2, 2, 2];
        Assert.Throws<IndexOutOfRangeException>(() => filter.Apply(image));
        new Lut3D().Apply(image);
    }

    private static float[,,,] RandomTable(int size)
    {
        var random = new Random(418);
        var table = new float[size, size, size, 3];
        for (int r = 0; r < size; r++) for (int g = 0; g < size; g++) for (int b = 0; b < size; b++)
            for (int c = 0; c < 3; c++) table[r, g, b, c] = (float)(random.NextDouble() * 1.6 - 0.3);
        return table;
    }

    private static string Cube(float[,,,] table)
    {
        int size = table.GetLength(0);
        var text = new StringBuilder("LUT_3D_SIZE " + size + "\n");
        for (int b = 0; b < size; b++) for (int g = 0; g < size; g++) for (int r = 0; r < size; r++)
            text.Append(table[r, g, b, 0].ToString("R", CultureInfo.InvariantCulture)).Append(' ')
                .Append(table[r, g, b, 1].ToString("R", CultureInfo.InvariantCulture)).Append(' ')
                .Append(table[r, g, b, 2].ToString("R", CultureInfo.InvariantCulture)).Append(" # entry\n");
        return text.ToString();
    }

    private static Color Reference(float[,,,] table, Color input, Lut3DInterpolation interpolation, float strength, float[] min, float[] max)
    {
        int size = table.GetLength(0);
        int[] original = { input.R, input.G, input.B };
        double[] position = Enumerable.Range(0, 3).Select(c => Math.Clamp((original[c] / 255.0 - min[c]) / (max[c] - (double)min[c]), 0, 1) * (size - 1)).ToArray();
        int[] low = position.Select(p => Math.Min((int)p, size - 2)).ToArray();
        double[] fraction = position.Select((p, c) => p - low[c]).ToArray();
        var output = new double[3];
        void Add(int r, int g, int b, double weight)
        { for (int c = 0; c < 3; c++) output[c] += weight * table[r, g, b, c]; }
        if (interpolation == Lut3DInterpolation.Trilinear)
        {
            for (int r = 0; r < 2; r++) for (int g = 0; g < 2; g++) for (int b = 0; b < 2; b++)
                Add(low[0] + r, low[1] + g, low[2] + b,
                    (r == 0 ? 1 - fraction[0] : fraction[0]) * (g == 0 ? 1 - fraction[1] : fraction[1]) * (b == 0 ? 1 - fraction[2] : fraction[2]));
        }
        else
        {
            int[] axes = Enumerable.Range(0, 3).OrderByDescending(c => fraction[c]).ToArray();
            int[] vertex = low.ToArray();
            double previous = 1;
            for (int i = 0; i < 3; i++)
            {
                double next = fraction[axes[i]];
                Add(vertex[0], vertex[1], vertex[2], previous - next);
                vertex[axes[i]]++;
                previous = next;
            }
            Add(vertex[0], vertex[1], vertex[2], previous);
        }
        return Color.FromArgb(input.A, Maths.Byte((float)(original[0] + strength * (255 * output[0] - original[0]))),
            Maths.Byte((float)(original[1] + strength * (255 * output[1] - original[1]))),
            Maths.Byte((float)(original[2] + strength * (255 * output[2] - original[2]))));
    }

    private sealed class Buffer32 : IDisposable
    {
        private readonly GCHandle pin;
        private readonly int origin;
        public byte[] Bytes { get; }
        public BitmapData Data { get; }
        public Buffer32(int width, int height, bool negative = false)
        {
            int pitch = width * 4 + 12;
            Bytes = Enumerable.Repeat((byte)173, 128 + pitch * height).ToArray();
            origin = 64 + (negative ? (height - 1) * pitch : 0);
            pin = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Data = new BitmapData { Width = width, Height = height, Stride = negative ? -pitch : pitch,
                Scan0 = IntPtr.Add(pin.AddrOfPinnedObject(), origin), PixelFormat = PixelFormat.Format32bppArgb };
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                Set(x, y, Color.FromArgb((x + y * 13) % 256, x % 256, (255 - x % 256 + y * 37) % 256, (x * 67 + y * 29) % 256));
        }
        public void Set(int x, int y, Color color)
        { int k = origin + y * Data.Stride + x * 4; Bytes[k] = color.B; Bytes[k + 1] = color.G; Bytes[k + 2] = color.R; Bytes[k + 3] = color.A; }
        public Color Get(int x, int y)
        { int k = origin + y * Data.Stride + x * 4; return Color.FromArgb(Bytes[k + 3], Bytes[k + 2], Bytes[k + 1], Bytes[k]); }
        public Color[] Pixels() => Enumerable.Range(0, Data.Width * Data.Height).Select(i => Get(i % Data.Width, i / Data.Width)).ToArray();
        public void AssertGuards()
        {
            var active = new bool[Bytes.Length];
            for (int y = 0; y < Data.Height; y++) for (int x = 0; x < Data.Width * 4; x++) active[origin + y * Data.Stride + x] = true;
            for (int i = 0; i < Bytes.Length; i++) if (!active[i]) Assert.Equal(173, Bytes[i]);
        }
        public void Dispose() { pin.Free(); }
    }
}
