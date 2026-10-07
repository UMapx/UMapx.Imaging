using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Applies a three-dimensional RGB color lookup table.
    /// </summary>
    /// <remarks>
    /// Table coordinates and output values use normalized RGB, without an implicit color-space conversion.
    /// Inputs outside the domain use the nearest boundary. Output is clamped only when converted to bytes.
    /// Pixel processing uses Format32bppArgb buffers and preserves alpha, including transparent RGB.
    /// </remarks>
    [Serializable]
    public partial class Lut3D : Rebuilder, IBitmapFilter
    {
        #region Private data
        private float[,,,] table;
        private bool[] identity;
        private float[] domainMin = { 0, 0, 0 };
        private float[] domainMax = { 1, 1, 1 };
        private float[][] inputs;
        private float strength = 1;
        #endregion

        #region Filter components
        /// <summary>
        /// Initializes an identity lookup table of size 2.
        /// </summary>
        public Lut3D() : this(2) { }

        /// <summary>
        /// Initializes an identity lookup table.
        /// </summary>
        /// <param name="size">Number of grid points per axis [2, 256].</param>
        public Lut3D(int size)
        {
            table = new float[size, size, size, 3];
            for (int r = 0; r < size; r++)
                for (int g = 0; g < size; g++)
                    for (int b = 0; b < size; b++)
                    {
                        table[r, g, b, 0] = r / (size - 1f);
                        table[r, g, b, 1] = g / (size - 1f);
                        table[r, g, b, 2] = b / (size - 1f);
                    }
            rebuild = true;
        }

        /// <summary>
        /// Initializes a filter with an RGB lookup table.
        /// </summary>
        /// <param name="table">Array [red, green, blue, channel], with shape [N, N, N, 3], N in [2, 256].</param>
        public Lut3D(float[,,,] table) { Table = table; }

        /// <summary>
        /// Gets the number of grid points per axis.
        /// </summary>
        public int Size => table.GetLength(0);

        /// <summary>
        /// Gets or sets the lookup table [red, green, blue, channel]. Shape is [N, N, N, 3].
        /// Output channels are R, G, B; values normally lie in [0, 1] but may exceed that range.
        /// Arrays are copied on assignment and retrieval.
        /// </summary>
        public float[,,,] Table
        {
            get => (float[,,,])table.Clone();
            set { table = (float[,,,])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the three lower input-domain bounds, in R, G, B order. Defaults to [0, 0, 0].
        /// Each component must be less than its DomainMax counterpart. Arrays are copied.
        /// </summary>
        public float[] DomainMin
        {
            get => (float[])domainMin.Clone();
            set { domainMin = (float[])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the three upper input-domain bounds, in R, G, B order. Defaults to [1, 1, 1].
        /// Arrays are copied on assignment and retrieval.
        /// </summary>
        public float[] DomainMax
        {
            get => (float[])domainMax.Clone();
            set { domainMax = (float[])value.Clone(); rebuild = true; }
        }

        /// <summary>
        /// Gets or sets the interpolation. Defaults to tetrahedral.
        /// </summary>
        public Lut3DInterpolation Interpolation { get; set; } = Lut3DInterpolation.Tetrahedral;

        /// <summary>
        /// Gets or sets the blend strength [0, 1]. Zero preserves every input byte.
        /// </summary>
        public float Strength
        {
            get => strength;
            set => strength = value;
        }

        /// <summary>
        /// Gets or sets the optional title of the lookup table.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Rebuilds input mappings and identifies unchanged output channels.
        /// </summary>
        protected override void Rebuild()
        {
            int size = Size;
            identity = new[] { true, true, true };
            for (int r = 0; r < size; r++)
                for (int g = 0; g < size; g++)
                    for (int b = 0; b < size; b++)
                    {
                        identity[0] &= table[r, g, b, 0] == r / (size - 1f);
                        identity[1] &= table[r, g, b, 1] == g / (size - 1f);
                        identity[2] &= table[r, g, b, 2] == b / (size - 1f);
                    }
            inputs = new float[3][];
            for (int c = 0; c < 3; c++)
            {
                inputs[c] = new float[256];
                for (int i = 0; i < 256; i++)
                    inputs[c][i] = Math.Max(0, Math.Min(255,
                        (i - 255 * domainMin[c]) / (domainMax[c] - domainMin[c])));
            }
        }

        /// <summary>
        /// Applies the lookup table to a bitmap using a 32-bit ARGB lock.
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
        /// Applies the lookup table to a 32-bit ARGB buffer, preserving alpha.
        /// </summary>
        /// <param name="bmData">Bitmap data.</param>
        public unsafe void Apply(BitmapData bmData)
        {
            if (bmData.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");

            if (strength == 0) return;
            if (rebuild) { Rebuild(); rebuild = false; }
            int last = Size - 1;
            byte* data = (byte*)bmData.Scan0;
            Parallel.For(0, bmData.Height, y =>
            {
                byte* row = data + (long)y * bmData.Stride;
                for (int x = 0; x < bmData.Width; x++, row += 4)
                {
                    float red = inputs[0][row[2]], green = inputs[1][row[1]], blue = inputs[2][row[0]];
                    float fr = red * last / 255, fg = green * last / 255, fb = blue * last / 255;
                    int r = Math.Min((int)fr, last - 1), g = Math.Min((int)fg, last - 1), b = Math.Min((int)fb, last - 1);
                    fr -= r; fg -= g; fb -= b;
                    for (int c = 0; c < 3; c++)
                    {
                        // Neutral channels use the exact input mapping, without an interpolation round trip.
                        float value = identity[c] ? (c == 0 ? red : c == 1 ? green : blue)
                            : 255 * (Interpolation == Lut3DInterpolation.Trilinear
                                ? Trilinear(r, g, b, fr, fg, fb, c) : Tetrahedral(r, g, b, fr, fg, fb, c));
                        row[2 - c] = Maths.Byte(row[2 - c] + strength * (value - row[2 - c]));
                    }
                }
            });
        }
        #endregion

        #region Private voids
        private float Trilinear(int r, int g, int b, float fr, float fg, float fb, int c)
        {
            float v000 = table[r, g, b, c], v100 = table[r + 1, g, b, c];
            float v010 = table[r, g + 1, b, c], v110 = table[r + 1, g + 1, b, c];
            float v001 = table[r, g, b + 1, c], v101 = table[r + 1, g, b + 1, c];
            float v011 = table[r, g + 1, b + 1, c], v111 = table[r + 1, g + 1, b + 1, c];
            float v00 = v000 + fr * (v100 - v000), v10 = v010 + fr * (v110 - v010);
            float v01 = v001 + fr * (v101 - v001), v11 = v011 + fr * (v111 - v011);
            float v0 = v00 + fg * (v10 - v00), v1 = v01 + fg * (v11 - v01);
            return v0 + fb * (v1 - v0);
        }

        private float Tetrahedral(int r, int g, int b, float fr, float fg, float fb, int c)
        {
            int r1 = r, g1 = g, b1 = b, r2 = r, g2 = g, b2 = b;
            float f1, f2, f3;
            if (fr >= fg)
            {
                if (fg >= fb) { r1++; r2++; g2++; f1 = fr; f2 = fg; f3 = fb; }
                else if (fr >= fb) { r1++; r2++; b2++; f1 = fr; f2 = fb; f3 = fg; }
                else { b1++; b2++; r2++; f1 = fb; f2 = fr; f3 = fg; }
            }
            else
            {
                if (fr >= fb) { g1++; g2++; r2++; f1 = fg; f2 = fr; f3 = fb; }
                else if (fg >= fb) { g1++; g2++; b2++; f1 = fg; f2 = fb; f3 = fr; }
                else { b1++; b2++; g2++; f1 = fb; f2 = fg; f3 = fr; }
            }
            float v0 = table[r, g, b, c], v1 = table[r1, g1, b1, c];
            float v2 = table[r2, g2, b2, c], v3 = table[r + 1, g + 1, b + 1, c];
            return v0 + f1 * (v1 - v0) + f2 * (v2 - v1) + f3 * (v3 - v2);
        }
        #endregion
    }
}
