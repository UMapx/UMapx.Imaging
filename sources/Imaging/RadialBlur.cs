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
    /// Alpha is blurred together with color. Pixel processing uses Format32bppArgb buffers.
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
                samples = value;
            }
        }

        /// <summary>
        /// Applies the filter to a 32-bit ARGB bitmap.
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
            if (bmData.Width != bmSrc.Width || bmData.Height != bmSrc.Height)
                throw new ArgumentException("Bitmap sizes must match");

            if (bmData.PixelFormat != PixelFormat.Format32bppArgb || bmSrc.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Only support Format32bppArgb pixelFormat");

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
            float cx = center.X * (width - 1), cy = center.Y * (height - 1);
            var cos = new float[samples];
            var sin = new float[samples];
            var scale = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (samples - 1f);
                float angle = (t - 0.5f) * amount * Maths.Pi / 180;
                cos[i] = Maths.Cos(angle); sin[i] = Maths.Sin(angle);
                scale[i] = 1 - t * amount / 100;
            }
            byte[] output = (byte[])source.Clone();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx == 0 && dy == 0) continue;
                    int k = (y * width + x) * 4;
                    float b = 0, g = 0, r = 0, a = 0;
                    for (int i = 0; i < samples; i++)
                    {
                        float sx = mode == RadialBlurMode.Spin ? cx + dx * cos[i] - dy * sin[i] : cx + dx * scale[i];
                        float sy = mode == RadialBlurMode.Spin ? cy + dx * sin[i] + dy * cos[i] : cy + dy * scale[i];
                        Sample(source, width, height, sx, sy, k, ref b, ref g, ref r, ref a);
                    }
                    output[k] = a > 0 ? Maths.Byte(source[k] + b / a) : (byte)0;
                    output[k + 1] = a > 0 ? Maths.Byte(source[k + 1] + g / a) : (byte)0;
                    output[k + 2] = a > 0 ? Maths.Byte(source[k + 2] + r / a) : (byte)0;
                    output[k + 3] = Maths.Byte(a / samples);
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
        private static void Sample(byte[] pixels, int width, int height, float x, float y, int center,
            ref float blue, ref float green, ref float red, ref float alpha)
        {
            x = Math.Max(0, Math.Min(width - 1, x));
            y = Math.Max(0, Math.Min(height - 1, y));
            int x0 = (int)x, y0 = (int)y;
            int x1 = Math.Min(x0 + 1, width - 1), y1 = Math.Min(y0 + 1, height - 1);
            float fx = x - x0, fy = y - y0;
            int i00 = (y0 * width + x0) * 4, i10 = (y0 * width + x1) * 4;
            int i01 = (y1 * width + x0) * 4, i11 = (y1 * width + x1) * 4;
            for (int c = 0; c < 4; c++)
            {
                float v00 = pixels[i00 + c], v10 = pixels[i10 + c];
                float v01 = pixels[i01 + c], v11 = pixels[i11 + c];
                if (c < 3)
                {
                    // Sum color differences from the destination color to preserve constant regions.
                    v00 -= pixels[center + c]; v10 -= pixels[center + c];
                    v01 -= pixels[center + c]; v11 -= pixels[center + c];
                    v00 *= pixels[i00 + 3]; v10 *= pixels[i10 + 3];
                    v01 *= pixels[i01 + 3]; v11 *= pixels[i11 + 3];
                }
                float top = v00 + fx * (v10 - v00), bottom = v01 + fx * (v11 - v01);
                float value = top + fy * (bottom - top);
                switch (c)
                {
                    case 0: blue += value; break;
                    case 1: green += value; break;
                    case 2: red += value; break;
                    case 3: alpha += value; break;
                }
            }
        }
        #endregion
    }
}
