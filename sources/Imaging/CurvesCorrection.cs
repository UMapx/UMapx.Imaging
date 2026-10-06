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
    /// Pixel processing uses Format32bppArgb buffers.
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
            set { points = (PointFloat[])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the red curve, with the same coordinate rules as Points. Arrays are copied.
        /// </summary>
        public PointFloat[] Red
        {
            get => (PointFloat[])red.Clone();
            set { red = (PointFloat[])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the green curve, with the same coordinate rules as Points. Arrays are copied.
        /// </summary>
        public PointFloat[] Green
        {
            get => (PointFloat[])green.Clone();
            set { green = (PointFloat[])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the blue curve, with the same coordinate rules as Points. Arrays are copied.
        /// </summary>
        public PointFloat[] Blue
        {
            get => (PointFloat[])blue.Clone();
            set { blue = (PointFloat[])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Rebuilds the combined channel lookup tables.
        /// </summary>
        protected override void Rebuild()
        {
            var curves = new[] { blue, green, red };
            var masterSlopes = Slopes(points);
            tables = new byte[3][];
            for (int c = 0; c < 3; c++)
            {
                tables[c] = new byte[256];
                float[] slopes = Slopes(curves[c]);
                for (int i = 0; i < 256; i++)
                    tables[c][i] = Maths.Byte(Evaluate(curves[c], slopes,
                        Evaluate(points, masterSlopes, i)));
            }
        }

        /// <summary>
        /// Applies the correction to a 32-bit ARGB bitmap.
        /// </summary>
        /// <param name="Data">Bitmap.</param>
        public void Apply(Bitmap Data)
        {
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
            if (bmData.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");

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

        // Weighted harmonic derivatives prevent overshoot on monotone curve segments (PCHIP).
        private static float[] Slopes(PointFloat[] curve)
        {
            int n = curve.Length;
            var h = new float[n - 1];
            var d = new float[n - 1];
            var m = new float[n];
            for (int i = 0; i < n - 1; i++)
            {
                h[i] = curve[i + 1].X - curve[i].X;
                d[i] = (curve[i + 1].Y - curve[i].Y) / h[i];
            }
            if (n == 2) { m[0] = m[1] = d[0]; return m; }
            m[0] = Endpoint(h[0], h[1], d[0], d[1]);
            m[n - 1] = Endpoint(h[n - 2], h[n - 3], d[n - 2], d[n - 3]);
            for (int i = 1; i < n - 1; i++)
            {
                if (d[i - 1] * d[i] <= 0) continue;
                float w1 = 2 * h[i] + h[i - 1], w2 = h[i] + 2 * h[i - 1];
                m[i] = (w1 + w2) / (w1 / d[i - 1] + w2 / d[i]);
            }
            return m;
        }

        private static float Endpoint(float h0, float h1, float d0, float d1)
        {
            float m = ((2 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);
            if (m * d0 <= 0) return 0;
            return d0 * d1 <= 0 && Math.Abs(m) > 3 * Math.Abs(d0) ? 3 * d0 : m;
        }

        private static float Evaluate(PointFloat[] curve, float[] slopes, float x)
        {
            int i = 0;
            while (i < curve.Length - 2 && x > curve[i + 1].X * 255) i++;
            float x0 = curve[i].X * 255, h = curve[i + 1].X * 255 - x0;
            float y0 = curve[i].Y * 255, delta = curve[i + 1].Y * 255 - y0;
            float t = Math.Max(0, Math.Min(1, (x - x0) / h));
            // Evaluate Hermite interpolation in byte coordinates, preserving linear curves and endpoints.
            float result = y0 + t * delta + t * (1 - t) *
                ((1 - t) * (h * slopes[i] - delta) + t * (delta - h * slopes[i + 1]));
            return Math.Max(0, Math.Min(255, result));
        }
        #endregion
    }
}
