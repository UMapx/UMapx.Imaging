using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Defines a radial spin or zoom blur filter.
    /// </summary>
    /// <remarks>
    /// Uses bilinear sampling with replicated borders and premultiplied-alpha averaging.
    /// Alpha is blurred together with color. Only Format32bppArgb is supported.
    /// </remarks>
    [Serializable]
    public class RadialBlur : IBitmapFilter2, IBitmapFilter
    {
        #region Private data
        private float amount;
        private int samples;
        private PointFloat center = new PointFloat(0.5f, 0.5f);
        private RadialBlurMode mode;
        #endregion

        #region Filter components
        /// <summary>
        /// Initializes a radial blur filter.
        /// </summary>
        /// <param name="amount">Amount [0, 100]: angular sweep in degrees for Spin, percentage for Zoom.</param>
        /// <param name="mode">Sampling path.</param>
        /// <param name="samples">Number of samples per pixel [2, 4096].</param>
        public RadialBlur(float amount = 10, RadialBlurMode mode = RadialBlurMode.Spin, int samples = 32)
        {
            Amount = amount; Mode = mode; Samples = samples;
        }

        /// <summary>
        /// Gets or sets the amount [0, 100]: degrees for Spin, inward travel percentage for Zoom.
        /// Zero preserves every source byte.
        /// </summary>
        public float Amount
        {
            get => amount;
            set
            {
                if (float.IsNaN(value) || value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value));
                amount = value;
            }
        }

        /// <summary>
        /// Gets or sets the normalized center in [0, 1] on each axis; (0.5, 0.5) is the image center.
        /// </summary>
        public PointFloat Center
        {
            get => center;
            set
            {
                if (float.IsNaN(value.X) || value.X < 0 || value.X > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (float.IsNaN(value.Y) || value.Y < 0 || value.Y > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                center = value;
            }
        }

        /// <summary>
        /// Gets or sets the sampling path.
        /// </summary>
        public RadialBlurMode Mode
        {
            get => mode;
            set
            {
                if (value != RadialBlurMode.Spin && value != RadialBlurMode.Zoom) throw new ArgumentOutOfRangeException(nameof(value));
                mode = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of samples per pixel [2, 4096].
        /// </summary>
        public int Samples
        {
            get => samples;
            set
            {
                if (value < 2 || value > 4096) throw new ArgumentOutOfRangeException(nameof(value));
                samples = value;
            }
        }

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
            if (amount == 0)
            {
                fixed (byte* pixels = source)
                {
                    for (int y = 0; y < bmData.Height; y++)
                        Buffer.MemoryCopy(pixels + y * rowBytes, dst + (long)y * bmData.Stride, rowBytes, rowBytes);
                }
                return;
            }
            int width = bmSrc.Width, height = bmSrc.Height;
            double cx = center.X * (width - 1.0), cy = center.Y * (height - 1.0);
            var cos = new double[samples];
            var sin = new double[samples];
            var scale = new double[samples];
            for (int i = 0; i < samples; i++)
            {
                double t = i / (samples - 1.0);
                double angle = (t - 0.5) * amount * Math.PI / 180;
                cos[i] = Math.Cos(angle); sin[i] = Math.Sin(angle);
                scale[i] = 1 - t * amount / 100;
            }
            byte[] output = (byte[])source.Clone();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double dx = x - cx, dy = y - cy;
                    if (dx == 0 && dy == 0) continue;
                    double b = 0, g = 0, r = 0, a = 0;
                    for (int i = 0; i < samples; i++)
                    {
                        double sx = mode == RadialBlurMode.Spin ? cx + dx * cos[i] - dy * sin[i] : cx + dx * scale[i];
                        double sy = mode == RadialBlurMode.Spin ? cy + dx * sin[i] + dy * cos[i] : cy + dy * scale[i];
                        Sample(source, width, height, sx, sy, ref b, ref g, ref r, ref a);
                    }
                    int k = (y * width + x) * 4;
                    output[k] = a > 0 ? ToByte(b / a) : (byte)0;
                    output[k + 1] = a > 0 ? ToByte(g / a) : (byte)0;
                    output[k + 2] = a > 0 ? ToByte(r / a) : (byte)0;
                    output[k + 3] = ToByte(a / samples);
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
        // Accumulates a bilinear sample in premultiplied-alpha space, with replicated borders.
        private static void Sample(byte[] pixels, int width, int height, double x, double y,
            ref double blue, ref double green, ref double red, ref double alpha)
        {
            x = Math.Max(0, Math.Min(width - 1, x));
            y = Math.Max(0, Math.Min(height - 1, y));
            int x0 = (int)x, y0 = (int)y;
            int x1 = Math.Min(x0 + 1, width - 1), y1 = Math.Min(y0 + 1, height - 1);
            double fx = x - x0, fy = y - y0;
            Accumulate(pixels, (y0 * width + x0) * 4, (1 - fx) * (1 - fy), ref blue, ref green, ref red, ref alpha);
            Accumulate(pixels, (y0 * width + x1) * 4, fx * (1 - fy), ref blue, ref green, ref red, ref alpha);
            Accumulate(pixels, (y1 * width + x0) * 4, (1 - fx) * fy, ref blue, ref green, ref red, ref alpha);
            Accumulate(pixels, (y1 * width + x1) * 4, fx * fy, ref blue, ref green, ref red, ref alpha);
        }

        private static void Accumulate(byte[] pixels, int index, double weight,
            ref double blue, ref double green, ref double red, ref double alpha)
        {
            double a = pixels[index + 3] * weight;
            blue += pixels[index] * a;
            green += pixels[index + 1] * a;
            red += pixels[index + 2] * a;
            alpha += a;
        }
        #endregion
    }
}
