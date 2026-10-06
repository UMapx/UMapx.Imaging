using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Defines an aperture-shaped lens blur with an optional normalized depth map.
    /// </summary>
    /// <remarks>
    /// Uses a circular or regular polygon aperture and premultiplied-alpha averaging.
    /// Borders use only available pixels. Alpha is blurred with color. Pixel processing uses Format32bppArgb buffers.
    /// Depth controls the destination pixel's radius; this is a gather approximation without occlusion reconstruction.
    /// </remarks>
    [Serializable]
    public class LensBlur : IBitmapFilter2, IBitmapFilter
    {
        #region Private data
        private int radius;
        private int blades;
        private float rotation;
        private float focusDepth = 0.5f;
        private float[,] depthMap;
        #endregion

        #region Filter components
        /// <summary>
        /// Initializes a lens blur filter.
        /// </summary>
        /// <param name="radius">Maximum aperture radius in pixels [0, 256].</param>
        /// <param name="blades">Zero for a circular aperture, or [3, 16] for a regular polygon.</param>
        public LensBlur(int radius = 8, int blades = 0) { Radius = radius; Blades = blades; }

        /// <summary>
        /// Gets or sets the maximum aperture radius in pixels [0, 256]. Zero is an exact identity.
        /// </summary>
        public int Radius
        {
            get => radius;
            set
            {
                radius = value;
            }
        }

        /// <summary>
        /// Gets or sets the aperture blade count: zero for a circle, or [3, 16] for a polygon.
        /// </summary>
        public int Blades
        {
            get => blades;
            set
            {
                blades = value;
            }
        }

        /// <summary>
        /// Gets or sets polygon rotation in degrees [0, 360]. Ignored for circular apertures.
        /// </summary>
        public float Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
            }
        }

        /// <summary>
        /// Gets or sets the normalized depth kept in focus [0, 1].
        /// The pixel radius is round(Radius * abs(depth - FocusDepth)).
        /// </summary>
        public float FocusDepth
        {
            get => focusDepth;
            set
            {
                focusDepth = value;
            }
        }

        /// <summary>
        /// Gets or sets an optional [height, width] depth map in [0, 1], where zero is near and one is far.
        /// Null applies a uniform Radius. Dimensions must match the image. Arrays are copied;
        /// this filter does not estimate depth from image brightness.
        /// </summary>
        public float[,] DepthMap
        {
            get => depthMap == null ? null : (float[,])depthMap.Clone();
            set
            {
                depthMap = value == null ? null : (float[,])value.Clone();
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

            int width = bmSrc.Width, height = bmSrc.Height;
            float[,] depth = depthMap;
            int rowBytes = checked(bmSrc.Width * 4);
            byte[] source = new byte[checked(rowBytes * bmSrc.Height)];
            byte* src = (byte*)bmSrc.Scan0.ToPointer();
            byte* dst = (byte*)bmData.Scan0.ToPointer();
            fixed (byte* snapshot = source)
            {
                for (int y = 0; y < bmSrc.Height; y++)
                    Buffer.MemoryCopy(src + (long)y * bmSrc.Stride, snapshot + y * rowBytes, rowBytes, rowBytes);
            }
            if (radius == 0)
            {
                fixed (byte* pixels = source)
                {
                    for (int y = 0; y < bmData.Height; y++)
                        Buffer.MemoryCopy(pixels + y * rowBytes, dst + (long)y * bmData.Stride, rowBytes, rowBytes);
                }
                return;
            }

            var radii = new int[checked(width * height)];
            var used = new bool[radius + 1];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int r = depth == null ? radius : (int)(radius * Math.Abs(depth[y, x] - focusDepth) + 0.5f);
                    radii[y * width + x] = r;
                    used[r] = true;
                }
            var spans = new int[radius + 1][];
            for (int r = 1; r <= radius; r++) if (used[r]) spans[r] = Aperture(r);

            // Row prefix sums turn each aperture row into one constant-time interval query.
            int pitch = checked(width + 1), length = checked(pitch * height);
            var blue = new float[length];
            var green = new float[length];
            var red = new float[length];
            var alpha = new float[length];
            Parallel.For(0, height, y =>
            {
                int row = y * pitch;
                for (int x = 0; x < width; x++)
                {
                    int k = (y * width + x) * 4, i = row + x;
                    float a = source[k + 3];
                    blue[i + 1] = blue[i] + source[k] * a;
                    green[i + 1] = green[i] + source[k + 1] * a;
                    red[i + 1] = red[i] + source[k + 2] * a;
                    alpha[i + 1] = alpha[i] + a;
                }
            });

            byte[] output = (byte[])source.Clone();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    int r = radii[y * width + x];
                    if (r == 0) continue;
                    int[] span = spans[r];
                    float b = 0, g = 0, redSum = 0, a = 0;
                    int count = 0;
                    for (int dy = Math.Max(-r, -y); dy <= Math.Min(r, height - 1 - y); dy++)
                    {
                        int index = (dy + r) * 2;
                        int left = Math.Max(0, x + span[index]);
                        int right = Math.Min(width - 1, x + span[index + 1]);
                        if (left > right) continue;
                        int row = (y + dy) * pitch, start = row + left, end = row + right + 1;
                        b += blue[end] - blue[start]; g += green[end] - green[start];
                        redSum += red[end] - red[start]; a += alpha[end] - alpha[start];
                        count += right - left + 1;
                    }
                    int k = (y * width + x) * 4;
                    output[k] = a > 0 ? Maths.Byte(b / a) : (byte)0;
                    output[k + 1] = a > 0 ? Maths.Byte(g / a) : (byte)0;
                    output[k + 2] = a > 0 ? Maths.Byte(redSum / a) : (byte)0;
                    output[k + 3] = Maths.Byte(a / count);
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
        private int[] Aperture(int r)
        {
            var spans = new int[(2 * r + 1) * 2];
            var nx = new float[blades];
            var ny = new float[blades];
            for (int i = 0; i < blades; i++)
            {
                float angle = rotation * Maths.Pi / 180 + (2 * i + 1) * Maths.Pi / blades;
                nx[i] = Maths.Cos(angle); ny[i] = Maths.Sin(angle);
            }
            float limit = blades == 0 ? r : r * Maths.Cos(Maths.Pi / blades);
            for (int y = -r; y <= r; y++)
            {
                int left = r + 1, right = -r - 1;
                for (int x = -r; x <= r; x++)
                {
                    bool inside = x * x + y * y <= r * r;
                    for (int i = 0; inside && i < blades; i++) inside = x * nx[i] + y * ny[i] <= limit + r * 1e-6f;
                    if (inside) { left = Math.Min(left, x); right = x; }
                }
                int index = (y + r) * 2;
                spans[index] = left; spans[index + 1] = right;
            }
            return spans;
        }
        #endregion
    }
}
