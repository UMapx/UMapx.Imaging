using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace UMapx.Imaging
{
    /// <summary>
    /// Removes atmospheric haze using the dark channel prior and guided transmission refinement.
    /// </summary>
    /// <remarks>
    /// Based on He, Sun and Tang, Single Image Haze Removal Using Dark Channel Prior.
    /// Alpha is preserved; fully transparent pixels are excluded from atmospheric-light estimation.
    /// Only Format32bppArgb is supported. Bright objects and skies can violate the dark channel prior;
    /// Strength and MinimumTransmission limit the correction in such scenes.
    /// </remarks>
    [Serializable]
    public class Dehaze : IBitmapFilter2, IBitmapFilter
    {
        #region Private data
        private int radius;
        private int refinementRadius = 16;
        private float strength;
        private float minimumTransmission = 0.1f;
        private float epsilon = 0.001f;
        #endregion

        #region Filter components
        /// <summary>
        /// Initializes the haze removal filter.
        /// </summary>
        /// <param name="radius">Dark channel window radius in pixels [0, 256].</param>
        /// <param name="strength">Haze removal strength [0, 1]. Zero is an exact identity.</param>
        public Dehaze(int radius = 7, float strength = 0.95f) { Radius = radius; Strength = strength; }

        /// <summary>
        /// Gets or sets the dark channel window radius in pixels [0, 256].
        /// </summary>
        public int Radius
        {
            get => radius;
            set
            {
                if (value < 0 || value > 256) throw new ArgumentOutOfRangeException(nameof(value));
                radius = value;
            }
        }

        /// <summary>
        /// Gets or sets haze removal strength [0, 1].
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
        /// Gets or sets the transmission floor [0.01, 1], limiting noise amplification.
        /// </summary>
        public float MinimumTransmission
        {
            get => minimumTransmission;
            set
            {
                if (float.IsNaN(value) || value < 0.01f || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                minimumTransmission = value;
            }
        }

        /// <summary>
        /// Gets or sets the guided refinement radius [0, 256]. Zero disables refinement.
        /// </summary>
        public int RefinementRadius
        {
            get => refinementRadius;
            set
            {
                if (value < 0 || value > 256) throw new ArgumentOutOfRangeException(nameof(value));
                refinementRadius = value;
            }
        }

        /// <summary>
        /// Gets or sets guided filter regularization [0.000001, 1] for normalized luminance.
        /// </summary>
        public float Epsilon
        {
            get => epsilon;
            set
            {
                if (float.IsNaN(value) || value < 0.000001f || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                epsilon = value;
            }
        }

        /// <summary>
        /// Gets or sets an optional atmospheric RGB color. Null estimates it from the brightest
        /// 0.1 percent of dark-channel pixels. Alpha is ignored; RGB components are floored at 1.
        /// </summary>
        public Color? AtmosphericLight { get; set; }

        /// <summary>
        /// Applies the filter to a 32-bit ARGB bitmap.
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
        /// Applies the filter in place, taking a source snapshot before writing.
        /// </summary>
        /// <param name="bmData">Bitmap data.</param>
        public void Apply(BitmapData bmData)
        {
            Apply(bmData, bmData);
        }

        /// <summary>
        /// Applies the filter from source to destination. The source is not modified.
        /// </summary>
        /// <param name="Data">Destination bitmap.</param>
        /// <param name="Src">Source bitmap of the same size.</param>
        public void Apply(Bitmap Data, Bitmap Src)
        {
            if (Data == null) throw new ArgumentNullException(nameof(Data));
            if (Data.Width <= 0 || Data.Height <= 0)
                throw new ArgumentException("Invalid bitmap dimensions", nameof(Data));
            if (Data.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");
            if (Src == null) throw new ArgumentNullException(nameof(Src));
            if (Src.Width <= 0 || Src.Height <= 0)
                throw new ArgumentException("Invalid bitmap dimensions", nameof(Src));
            if (Src.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");
            if (Data.Width != Src.Width || Data.Height != Src.Height)
                throw new ArgumentException("Bitmap sizes must match");

            BitmapData bmData = BitmapFormat.Lock32bpp(Data);
            try
            {
                BitmapData bmSrc = BitmapFormat.Lock32bpp(Src);
                try
                {
                    Apply(bmData, bmSrc);
                }
                finally
                {
                    BitmapFormat.Unlock(Src, bmSrc);
                }
            }
            finally
            {
                BitmapFormat.Unlock(Data, bmData);
            }
        }

        /// <summary>
        /// Applies the filter between equally sized 32-bit ARGB buffers. Aliasing is supported.
        /// </summary>
        /// <param name="bmData">Destination bitmap data.</param>
        /// <param name="bmSrc">Source bitmap data.</param>
        public unsafe void Apply(BitmapData bmData, BitmapData bmSrc)
        {
            static byte ToByte(double value) =>
                value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)(value + 0.5);

            if (bmData == null) throw new ArgumentNullException(nameof(bmData));
            if (bmData.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");
            if (bmData.Width <= 0 || bmData.Height <= 0 || bmData.Scan0 == IntPtr.Zero ||
                Math.Abs((long)bmData.Stride) < (long)bmData.Width * 4)
                throw new ArgumentException("Invalid bitmap buffer", nameof(bmData));
            if (bmSrc == null) throw new ArgumentNullException(nameof(bmSrc));
            if (bmSrc.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");
            if (bmSrc.Width <= 0 || bmSrc.Height <= 0 || bmSrc.Scan0 == IntPtr.Zero ||
                Math.Abs((long)bmSrc.Stride) < (long)bmSrc.Width * 4)
                throw new ArgumentException("Invalid bitmap buffer", nameof(bmSrc));
            if (bmData.Width != bmSrc.Width || bmData.Height != bmSrc.Height)
                throw new ArgumentException("Bitmap sizes must match");

            int rowBytes = checked(bmSrc.Width * 4);
            byte[] source = new byte[checked(rowBytes * bmSrc.Height)];
            byte* src = (byte*)bmSrc.Scan0.ToPointer();
            byte* dst = (byte*)bmData.Scan0.ToPointer();
            fixed (byte* snapshot = source)
            {
                for (int y = 0; y < bmSrc.Height; y++)
                    Buffer.MemoryCopy(src + (long)y * bmSrc.Stride, snapshot + y * rowBytes, rowBytes, rowBytes);
            }
            if (strength == 0)
            {
                fixed (byte* pixels = source)
                {
                    for (int y = 0; y < bmData.Height; y++)
                        Buffer.MemoryCopy(pixels + y * rowBytes, dst + (long)y * bmData.Stride, rowBytes, rowBytes);
                }
                return;
            }
            int width = bmSrc.Width, height = bmSrc.Height, count = checked(width * height);
            var dark = new float[count];
            int visible = 0;
            for (int i = 0; i < count; i++)
            {
                int k = i * 4;
                if (source[k + 3] == 0) { dark[i] = float.PositiveInfinity; continue; }
                dark[i] = Math.Min(source[k], Math.Min(source[k + 1], source[k + 2]));
                visible++;
            }
            if (visible == 0)
            {
                fixed (byte* pixels = source)
                {
                    for (int y = 0; y < bmData.Height; y++)
                        Buffer.MemoryCopy(pixels + y * rowBytes, dst + (long)y * bmData.Stride, rowBytes, rowBytes);
                }
                return;
            }
            Color atmosphere = AtmosphericLight ?? EstimateAtmosphere(source, Minimum(dark, width, height, radius), visible);
            float ab = Math.Max(1, (int)atmosphere.B), ag = Math.Max(1, (int)atmosphere.G), ar = Math.Max(1, (int)atmosphere.R);
            var guide = new float[count];
            for (int i = 0; i < count; i++)
            {
                int k = i * 4;
                if (source[k + 3] == 0) continue;
                dark[i] = Math.Min(source[k] / ab, Math.Min(source[k + 1] / ag, source[k + 2] / ar));
                guide[i] = (0.0722f * source[k] + 0.7152f * source[k + 1] + 0.2126f * source[k + 2]) / 255;
            }
            float[] transmission = Minimum(dark, width, height, radius);
            for (int i = 0; i < count; i++)
                transmission[i] = float.IsPositiveInfinity(transmission[i]) ? 1 : Math.Max(0, 1 - strength * transmission[i]);
            if (refinementRadius > 0) transmission = Refine(guide, transmission, width, height);
            byte[] output = (byte[])source.Clone();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x, k = i * 4;
                    if (source[k + 3] == 0) continue;
                    double t = Math.Max(minimumTransmission, Math.Min(1, transmission[i]));
                    output[k] = ToByte((source[k] - ab) / t + ab);
                    output[k + 1] = ToByte((source[k + 1] - ag) / t + ag);
                    output[k + 2] = ToByte((source[k + 2] - ar) / t + ar);
                }
            });
            fixed (byte* pixels = output)
            {
                for (int y = 0; y < bmData.Height; y++)
                    Buffer.MemoryCopy(pixels + y * rowBytes, dst + (long)y * bmData.Stride, rowBytes, rowBytes);
            }
        }
        #endregion

        #region Private voids
        private static Color EstimateAtmosphere(byte[] source, float[] dark, int visible)
        {
            var histogram = new int[256];
            for (int i = 0; i < dark.Length; i++)
                if (source[i * 4 + 3] != 0) histogram[(int)dark[i]]++;
            int threshold = 255, selected = histogram[255], target = Math.Max(1, (int)Math.Ceiling(visible * 0.001));
            while (threshold > 0 && selected < target) selected += histogram[--threshold];
            int best = -1, brightest = -1;
            for (int i = 0; i < dark.Length; i++)
            {
                int k = i * 4;
                if (source[k + 3] == 0 || dark[i] < threshold) continue;
                int brightness = source[k] + source[k + 1] + source[k + 2];
                if (brightness > brightest) { brightest = brightness; best = k; }
            }
            return Color.FromArgb(source[best + 2], source[best + 1], source[best]);
        }

        // Separable monotone queues compute clipped-window minima in linear time.
        private static float[] Minimum(float[] input, int width, int height, int radius)
        {
            var temporary = new float[input.Length];
            var output = new float[input.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                bool horizontal = pass == 0;
                int length = horizontal ? width : height, lines = horizontal ? height : width;
                int step = horizontal ? 1 : width;
                float[] source = horizontal ? input : temporary, target = horizontal ? temporary : output;
                Parallel.For(0, lines, line =>
                {
                    var queue = new int[length];
                    int head = 0, tail = 0, next = 0, start = horizontal ? line * width : line;
                    for (int x = 0; x < length; x++)
                    {
                        int end = Math.Min(length - 1, x + radius);
                        while (next <= end)
                        {
                            while (tail > head && source[start + queue[tail - 1] * step] >= source[start + next * step]) tail--;
                            queue[tail++] = next++;
                        }
                        while (queue[head] < x - radius) head++;
                        target[start + x * step] = source[start + queue[head] * step];
                    }
                });
            }
            return output;
        }

        // Cross-guided filtering uses image luminance as guidance, not the transmission itself.
        private float[] Refine(float[] guide, float[] transmission, int width, int height)
        {
            float[] meanI = Mean(guide, width, height, refinementRadius);
            float[] meanP = Mean(transmission, width, height, refinementRadius);
            var ii = new float[guide.Length];
            var ip = new float[guide.Length];
            for (int i = 0; i < guide.Length; i++) { ii[i] = guide[i] * guide[i]; ip[i] = guide[i] * transmission[i]; }
            float[] corrI = Mean(ii, width, height, refinementRadius);
            float[] corrIp = Mean(ip, width, height, refinementRadius);
            for (int i = 0; i < guide.Length; i++)
            {
                double variance = Math.Max(0, corrI[i] - (double)meanI[i] * meanI[i]);
                ii[i] = (float)((corrIp[i] - (double)meanI[i] * meanP[i]) / (variance + epsilon));
                ip[i] = meanP[i] - ii[i] * meanI[i];
            }
            float[] meanA = Mean(ii, width, height, refinementRadius);
            float[] meanB = Mean(ip, width, height, refinementRadius);
            for (int i = 0; i < guide.Length; i++) transmission[i] = meanA[i] * guide[i] + meanB[i];
            return transmission;
        }

        private static float[] Mean(float[] input, int width, int height, int radius)
        {
            var temporary = new float[input.Length];
            var output = new float[input.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                bool horizontal = pass == 0;
                int length = horizontal ? width : height, lines = horizontal ? height : width;
                int step = horizontal ? 1 : width;
                float[] source = horizontal ? input : temporary, target = horizontal ? temporary : output;
                Parallel.For(0, lines, line =>
                {
                    int start = horizontal ? line * width : line, left = 0, right = -1;
                    double sum = 0;
                    for (int x = 0; x < length; x++)
                    {
                        int end = Math.Min(length - 1, x + radius), begin = Math.Max(0, x - radius);
                        while (right < end) sum += source[start + ++right * step];
                        while (left < begin) sum -= source[start + left++ * step];
                        target[start + x * step] = (float)(sum / (end - begin + 1));
                    }
                });
            }
            return output;
        }
        #endregion
    }
}
