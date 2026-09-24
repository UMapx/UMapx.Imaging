using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Defines a tone curve correction with a master curve and independent RGB curves.
    /// </summary>
    /// <remarks>
    /// Uses shape-preserving cubic interpolation. Coordinates are normalized to [0, 1].
    /// The master curve is applied before each channel curve. Alpha is preserved.
    /// Only Format32bppArgb images and buffers are supported.
    /// </remarks>
    [Serializable]
    public class CurvesCorrection : Rebuilder, IBitmapFilter
    {
        #region Private data
        private PointFloat[] points = Identity();
        private PointFloat[] red = Identity();
        private PointFloat[] green = Identity();
        private PointFloat[] blue = Identity();
        private byte[][] tables;
        #endregion

        #region Filter components
        /// <summary>
        /// Initializes an identity curve correction.
        /// </summary>
        public CurvesCorrection() { rebuild = true; }

        /// <summary>
        /// Initializes a correction with the specified master curve.
        /// </summary>
        /// <param name="points">At least two control points, ordered by X, including X=0 and X=1.</param>
        public CurvesCorrection(PointFloat[] points) { Points = points; }

        /// <summary>
        /// Gets or sets the master control points. X must strictly increase from 0 to 1; Y is in [0, 1].
        /// Arrays are copied on assignment and retrieval.
        /// </summary>
        public PointFloat[] Points
        {
            get => (PointFloat[])points.Clone();
            set { points = Validate(value); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the red curve, with the same coordinate rules as Points. Arrays are copied.
        /// </summary>
        public PointFloat[] Red
        {
            get => (PointFloat[])red.Clone();
            set { red = Validate(value); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the green curve, with the same coordinate rules as Points. Arrays are copied.
        /// </summary>
        public PointFloat[] Green
        {
            get => (PointFloat[])green.Clone();
            set { green = Validate(value); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the blue curve, with the same coordinate rules as Points. Arrays are copied.
        /// </summary>
        public PointFloat[] Blue
        {
            get => (PointFloat[])blue.Clone();
            set { blue = Validate(value); rebuild = true; }
        }

        /// <summary>
        /// Rebuilds the combined channel lookup tables.
        /// </summary>
        protected override void Rebuild()
        {
            static byte ToByte(double value) =>
                value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)(value + 0.5);

            var curves = new[] { blue, green, red };
            var masterSlopes = Slopes(points);
            tables = new byte[3][];
            for (int c = 0; c < 3; c++)
            {
                tables[c] = new byte[256];
                double[] slopes = Slopes(curves[c]);
                for (int i = 0; i < 256; i++)
                    tables[c][i] = ToByte(255 * Evaluate(curves[c], slopes,
                        Evaluate(points, masterSlopes, i / 255.0)));
            }
        }

        /// <summary>
        /// Applies the correction to a 32-bit ARGB bitmap.
        /// </summary>
        /// <param name="Data">Bitmap.</param>
        public void Apply(Bitmap Data)
        {
            if (Data == null) throw new ArgumentNullException(nameof(Data));
            if (Data.Width <= 0 || Data.Height <= 0)
                throw new ArgumentException("Invalid bitmap dimensions", nameof(Data));
            if (Data.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");

            BitmapData bmData = BitmapFormat.Lock32bpp(Data);
            try
            {
                Apply(bmData);
            }
            finally
            {
                BitmapFormat.Unlock(Data, bmData);
            }
        }

        /// <summary>
        /// Applies the correction to a 32-bit ARGB buffer, preserving alpha.
        /// </summary>
        /// <param name="bmData">Bitmap data.</param>
        public unsafe void Apply(BitmapData bmData)
        {
            if (bmData == null) throw new ArgumentNullException(nameof(bmData));
            if (bmData.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");
            if (bmData.Width <= 0 || bmData.Height <= 0 || bmData.Scan0 == IntPtr.Zero ||
                Math.Abs((long)bmData.Stride) < (long)bmData.Width * 4)
                throw new ArgumentException("Invalid bitmap buffer", nameof(bmData));
            if (rebuild) { Rebuild(); rebuild = false; }
            byte* data = (byte*)bmData.Scan0;
            Parallel.For(0, bmData.Height, y =>
            {
                byte* row = data + (long)y * bmData.Stride;
                for (int x = 0; x < bmData.Width; x++, row += 4)
                    for (int c = 0; c < 3; c++) row[c] = tables[c][row[c]];
            });
        }
        #endregion

        #region Private voids
        private static PointFloat[] Identity()
        {
            return new[] { new PointFloat(0, 0), new PointFloat(1, 1) };
        }

        private static PointFloat[] Validate(PointFloat[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length < 2 || value[0].X != 0 || value[value.Length - 1].X != 1)
                throw new ArgumentException("Curves require at least two points and endpoints at X=0 and X=1", nameof(value));
            for (int i = 0; i < value.Length; i++)
            {
                if (float.IsNaN(value[i].X) || value[i].X < 0 || value[i].X > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (float.IsNaN(value[i].Y) || value[i].Y < 0 || value[i].Y > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (i > 0 && value[i].X <= value[i - 1].X)
                    throw new ArgumentException("Control point X coordinates must strictly increase", nameof(value));
            }
            return (PointFloat[])value.Clone();
        }

        // Weighted harmonic derivatives prevent overshoot on monotone curve segments (PCHIP).
        private static double[] Slopes(PointFloat[] curve)
        {
            int n = curve.Length;
            var h = new double[n - 1];
            var d = new double[n - 1];
            var m = new double[n];
            for (int i = 0; i < n - 1; i++)
            {
                h[i] = (double)curve[i + 1].X - curve[i].X;
                d[i] = ((double)curve[i + 1].Y - curve[i].Y) / h[i];
            }
            if (n == 2) { m[0] = m[1] = d[0]; return m; }
            m[0] = Endpoint(h[0], h[1], d[0], d[1]);
            m[n - 1] = Endpoint(h[n - 2], h[n - 3], d[n - 2], d[n - 3]);
            for (int i = 1; i < n - 1; i++)
            {
                if (d[i - 1] * d[i] <= 0) continue;
                double w1 = 2 * h[i] + h[i - 1], w2 = h[i] + 2 * h[i - 1];
                m[i] = (w1 + w2) / (w1 / d[i - 1] + w2 / d[i]);
            }
            return m;
        }

        private static double Endpoint(double h0, double h1, double d0, double d1)
        {
            double m = ((2 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);
            if (m * d0 <= 0) return 0;
            return d0 * d1 <= 0 && Math.Abs(m) > 3 * Math.Abs(d0) ? 3 * d0 : m;
        }

        private static double Evaluate(PointFloat[] curve, double[] slopes, double x)
        {
            int i = 0;
            while (i < curve.Length - 2 && x > curve[i + 1].X) i++;
            double h = (double)curve[i + 1].X - curve[i].X;
            double t = Math.Max(0, Math.Min(1, (x - curve[i].X) / h));
            double t2 = t * t, t3 = t2 * t;
            double result = (2 * t3 - 3 * t2 + 1) * curve[i].Y + (t3 - 2 * t2 + t) * h * slopes[i]
                + (-2 * t3 + 3 * t2) * curve[i + 1].Y + (t3 - t2) * h * slopes[i + 1];
            return Math.Max(0, Math.Min(1, result));
        }
        #endregion
    }
}
