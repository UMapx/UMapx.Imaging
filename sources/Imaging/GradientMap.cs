using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace UMapx.Imaging
{
    /// <summary>
    /// Maps image luminance to a multicolor gradient.
    /// </summary>
    /// <remarks>
    /// Uses Rec.709 luminance weights on encoded RGB values and linear interpolation between stops.
    /// Source alpha is preserved; gradient color alpha is ignored. Only Format32bppArgb is supported.
    /// </remarks>
    [Serializable]
    public class GradientMap : Rebuilder, IBitmapFilter
    {
        #region Private data
        private Color[] colors;
        private float[] positions;
        private byte[][] tables;
        private float strength = 1;
        #endregion

        #region Filter components
        /// <summary>
        /// Initializes a black-to-white gradient map.
        /// </summary>
        public GradientMap() : this(new[] { Color.Black, Color.White }) { }

        /// <summary>
        /// Initializes a two-color gradient map.
        /// </summary>
        /// <param name="shadows">Shadow color.</param>
        /// <param name="highlights">Highlight color.</param>
        public GradientMap(Color shadows, Color highlights) : this(new[] { shadows, highlights }) { }

        /// <summary>
        /// Initializes a gradient map with evenly spaced colors.
        /// </summary>
        /// <param name="colors">At least two gradient colors.</param>
        public GradientMap(Color[] colors) { Colors = colors; }

        /// <summary>
        /// Initializes a gradient map with explicit stop positions.
        /// </summary>
        /// <param name="colors">Gradient colors.</param>
        /// <param name="positions">Strictly increasing positions from 0 to 1, one per color.</param>
        public GradientMap(Color[] colors, float[] positions) { Colors = colors; Positions = positions; }

        /// <summary>
        /// Gets or sets the gradient colors. Assignment resets positions to evenly spaced stops.
        /// Arrays are copied on assignment and retrieval.
        /// </summary>
        public Color[] Colors
        {
            get => (Color[])colors.Clone();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length < 2) throw new ArgumentException("At least two colors are required", nameof(value));
                colors = (Color[])value.Clone();
                positions = new float[value.Length];
                for (int i = 0; i < positions.Length; i++) positions[i] = i / (float)(positions.Length - 1);
                rebuild = true;
            }
        }

        /// <summary>
        /// Gets or sets strictly increasing stop positions in [0, 1], including 0 and 1.
        /// The count must match Colors. Arrays are copied.
        /// </summary>
        public float[] Positions
        {
            get => (float[])positions.Clone();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length != colors.Length || value[0] != 0 || value[value.Length - 1] != 1)
                    throw new ArgumentException("Stop positions must match colors and include 0 and 1", nameof(value));
                for (int i = 0; i < value.Length; i++)
                {
                    if (float.IsNaN(value[i]) || value[i] < 0 || value[i] > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    if (i > 0 && value[i] <= value[i - 1]) throw new ArgumentException("Stop positions must strictly increase", nameof(value));
                }
                positions = (float[])value.Clone();
                rebuild = true;
            }
        }

        /// <summary>
        /// Gets or sets the blend strength in [0, 1]. Zero preserves the input exactly.
        /// </summary>
        public float Strength
        {
            get => strength;
            set
            {
                if (float.IsNaN(value) || value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                strength = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to reverse the luminance used to address the gradient.
        /// </summary>
        public bool Inverted { get; set; }

        /// <summary>
        /// Rebuilds the gradient lookup table.
        /// </summary>
        protected override void Rebuild()
        {
            static byte ToByte(double value) =>
                value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)(value + 0.5);

            tables = new[] { new byte[256], new byte[256], new byte[256] };
            int stop = 0;
            for (int i = 0; i < 256; i++)
            {
                double x = i / 255.0;
                while (stop < colors.Length - 2 && x > positions[stop + 1]) stop++;
                double t = (x - positions[stop]) / ((double)positions[stop + 1] - positions[stop]);
                Color a = colors[stop], b = colors[stop + 1];
                tables[0][i] = ToByte(a.B + t * (b.B - a.B));
                tables[1][i] = ToByte(a.G + t * (b.G - a.G));
                tables[2][i] = ToByte(a.R + t * (b.R - a.R));
            }
        }

        /// <summary>
        /// Applies the gradient map to a 32-bit ARGB bitmap.
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
        /// Applies the gradient map to a 32-bit ARGB buffer.
        /// </summary>
        /// <param name="bmData">Bitmap data.</param>
        public unsafe void Apply(BitmapData bmData)
        {
            static byte ToByte(double value) =>
                value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)(value + 0.5);

            if (bmData == null) throw new ArgumentNullException(nameof(bmData));
            if (bmData.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");
            if (bmData.Width <= 0 || bmData.Height <= 0 || bmData.Scan0 == IntPtr.Zero ||
                Math.Abs((long)bmData.Stride) < (long)bmData.Width * 4)
                throw new ArgumentException("Invalid bitmap buffer", nameof(bmData));
            if (strength == 0) return;
            if (rebuild) { Rebuild(); rebuild = false; }
            byte* data = (byte*)bmData.Scan0;
            Parallel.For(0, bmData.Height, y =>
            {
                byte* row = data + (long)y * bmData.Stride;
                for (int x = 0; x < bmData.Width; x++, row += 4)
                {
                    int index = ToByte(0.0722 * row[0] + 0.7152 * row[1] + 0.2126 * row[2]);
                    if (Inverted) index = 255 - index;
                    for (int c = 0; c < 3; c++) row[c] = ToByte(row[c] + strength * (tables[c][index] - row[c]));
                }
            });
        }
        #endregion
    }
}
