using System;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Using for rectangles operations.
    /// </summary>
    public static partial class Rectangles
    {
        #region Operators

        /// <summary>
        /// Returns processed rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle Add(this Rectangle rectangle, Point point)
        {
            return new Rectangle
            {
                X = rectangle.X + point.X,
                Y = rectangle.Y + point.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }
        /// <summary>
        /// Returns processed rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF Add(this RectangleF rectangle, PointF point)
        {
            return new RectangleF
            {
                X = rectangle.X + point.X,
                Y = rectangle.Y + point.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        /// <summary>
        /// Returns processed rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle Sub(this Rectangle rectangle, Point point)
        {
            return new Rectangle
            {
                X = rectangle.X - point.X,
                Y = rectangle.Y - point.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }
        /// <summary>
        /// Returns processed rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF Sub(this RectangleF rectangle, PointF point)
        {
            return new RectangleF
            {
                X = rectangle.X - point.X,
                Y = rectangle.Y - point.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        /// <summary>
        /// Returns processed rectangles.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangles.</returns>
        public static Rectangle[] Add(this Rectangle[] rectangles, Point point)
        {
            var count = rectangles.Length;
            var output = new Rectangle[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = rectangles[i].Add(point);
            }

            return output;
        }
        /// <summary>
        /// Returns processed rectangles.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangles.</returns>
        public static RectangleF[] Add(this RectangleF[] rectangles, PointF point)
        {
            var count = rectangles.Length;
            var output = new RectangleF[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = rectangles[i].Add(point);
            }

            return output;
        }

        /// <summary>
        /// Returns processed rectangles.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangles.</returns>
        public static Rectangle[] Sub(this Rectangle[] rectangles, Point point)
        {
            var count = rectangles.Length;
            var output = new Rectangle[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = rectangles[i].Sub(point);
            }

            return output;
        }
        /// <summary>
        /// Returns processed rectangles.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <param name="point">Point.</param>
        /// <returns>Rectangles.</returns>
        public static RectangleF[] Sub(this RectangleF[] rectangles, PointF point)
        {
            var count = rectangles.Length;
            var output = new RectangleF[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = rectangles[i].Sub(point);
            }

            return output;
        }

        #endregion

        #region Special operators

        /// <summary>
        /// Returns four points from rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Points.</returns>
        public static Point[] ToPoints(this Rectangle rectangle)
        {
            return new Point[]
            {
                new Point (rectangle.Left, rectangle.Top),
                new Point (rectangle.Right, rectangle.Top),
                new Point (rectangle.Right, rectangle.Bottom),
                new Point (rectangle.Left, rectangle.Bottom)
            };
        }
        /// <summary>
        /// Returns four points from rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Points.</returns>
        public static PointF[] ToPoints(this RectangleF rectangle)
        {
            return new PointF[]
            {
                new PointF (rectangle.Left, rectangle.Top),
                new PointF (rectangle.Right, rectangle.Top),
                new PointF (rectangle.Right, rectangle.Bottom),
                new PointF (rectangle.Left, rectangle.Bottom)
            };
        }

        /// <summary>
        /// Returns rectangle from four points.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Rectangle FromPoints(this Point[] points)
        {
            if (points.Length != 4)
                throw new ArgumentException("A rectangle can only be built using four points");

            return Rectangle.FromLTRB(
                points[0].X,
                points[0].Y,
                points[2].X,
                points[2].Y);
        }
        /// <summary>
        /// Returns rectangle from four points.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static RectangleF FromPoints(this PointF[] points)
        {
            if (points.Length != 4)
                throw new ArgumentException("A rectangle can only be built using four points");

            return RectangleF.FromLTRB(
                points[0].X,
                points[0].Y,
                points[2].X,
                points[2].Y);
        }

        /// <summary>
        /// Returns point from rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Point.</returns>
        public static Point GetPoint(this Rectangle rectangle)
        {
            return new Point
            {
                X = rectangle.X,
                Y = rectangle.Y
            };
        }
        /// <summary>
        /// Returns point from rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Point.</returns>
        public static PointF GetPoint(this RectangleF rectangle)
        {
            return new PointF
            {
                X = rectangle.X,
                Y = rectangle.Y
            };
        }

        /// <summary>
        /// Returns size area.
        /// </summary>
        /// <param name="size">Size.</param>
        /// <returns>Area.</returns>
        public static int Area(this Size size)
        {
            return size.Width * size.Height;
        }
        /// <summary>
        /// Returns size area.
        /// </summary>
        /// <param name="size">Size.</param>
        /// <returns>Area.</returns>
        public static float Area(this SizeF size)
        {
            return size.Width * size.Height;
        }
        /// <summary>
        /// Returns rectangle area.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Area.</returns>
        public static int Area(this Rectangle rectangle)
        {
            return rectangle.Width * rectangle.Height;
        }
        /// <summary>
        /// Returns rectangle area.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Area.</returns>
        public static float Area(this RectangleF rectangle)
        {
            return rectangle.Width * rectangle.Height;
        }

        /// <summary>
        /// Returns the maximum rectangle.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle Max(params Rectangle[] rectangles)
        {
            // params
            var length = rectangles.Length;
            var rectangle = Rectangle.Empty;
            var area = int.MinValue;
            var max = 0;

            // do job
            for (int i = 0; i < length; i++)
            {
                rectangle = rectangles[i];

                if (rectangle.IsEmpty)
                    continue;

                var current = rectangle.Area();

                if (current > area)
                {
                    max = i;
                    area = current;
                }
            }

            // output
            return length > 0 ? rectangles[max] : rectangle;
        }
        /// <summary>
        /// Returns the maximum rectangle.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF Max(params RectangleF[] rectangles)
        {
            // params
            var length = rectangles.Length;
            var rectangle = RectangleF.Empty;
            var area = float.MinValue;
            var max = 0;

            // do job
            for (int i = 0; i < length; i++)
            {
                rectangle = rectangles[i];

                if (rectangle.IsEmpty)
                    continue;

                var current = rectangle.Area();

                if (current > area)
                {
                    max = i;
                    area = current;
                }
            }

            // output
            return length > 0 ? rectangles[max] : rectangle;
        }

        /// <summary>
        /// Returns the minimum rectangle.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle Min(params Rectangle[] rectangles)
        {
            // params
            var length = rectangles.Length;
            var rectangle = Rectangle.Empty;
            var area = int.MaxValue;
            var min = -1;

            // do job
            for (int i = 0; i < length; i++)
            {
                rectangle = rectangles[i];

                if (rectangle.IsEmpty)
                    continue;

                var current = rectangle.Area();

                if (current < area)
                {
                    min = i;
                    area = current;
                }
            }

            // output
            return min >= 0 ? rectangles[min] : Rectangle.Empty;
        }
        /// <summary>
        /// Returns the minimum rectangle.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF Min(params RectangleF[] rectangles)
        {
            // params
            var length = rectangles.Length;
            var rectangle = RectangleF.Empty;
            var area = float.MaxValue;
            var min = -1;

            // do job
            for (int i = 0; i < length; i++)
            {
                rectangle = rectangles[i];

                if (rectangle.IsEmpty)
                    continue;

                var current = rectangle.Area();

                if (current < area)
                {
                    min = i;
                    area = current;
                }
            }

            // output
            return min >= 0 ? rectangles[min] : Rectangle.Empty;
        }

        /// <summary>
        /// Returns rectangle scaled to box.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle ToBox(this Rectangle rectangle)
        {
            var max = Math.Max(rectangle.Width, rectangle.Height);
            var dx = max - rectangle.Width;
            var dy = max - rectangle.Height;

            return new Rectangle
            {
                X = rectangle.X - dx / 2,
                Y = rectangle.Y - dy / 2,
                Width = rectangle.Width + dx,
                Height = rectangle.Height + dy
            };
        }
        /// <summary>
        /// Returns rectangle scaled to box.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF ToBox(this RectangleF rectangle)
        {
            var max = Math.Max(rectangle.Width, rectangle.Height);
            var dx = max - rectangle.Width;
            var dy = max - rectangle.Height;

            return new RectangleF
            {
                X = rectangle.X - dx / 2,
                Y = rectangle.Y - dy / 2,
                Width = rectangle.Width + dx,
                Height = rectangle.Height + dy
            };
        }

        /// <summary>
        /// Returns rectangle scaled to box.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="scale">Factor.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle ToBox(this Rectangle rectangle, float scale)
        {
            float gainX = rectangle.Width * scale;
            float gainY = rectangle.Height * scale;

            return new Rectangle(
                (int)(rectangle.X - gainX / 2),
                (int)(rectangle.Y - gainY / 2),
                (int)(rectangle.Width + gainX),
                (int)(rectangle.Height + gainY)
                );
        }
        /// <summary>
        /// Returns rectangle scaled to box.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="scale">Factor.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF ToBox(this RectangleF rectangle, float scale)
        {
            float gainX = rectangle.Width * scale;
            float gainY = rectangle.Height * scale;

            return new RectangleF(
                (int)(rectangle.X - gainX / 2),
                (int)(rectangle.Y - gainY / 2),
                (int)(rectangle.Width + gainX),
                (int)(rectangle.Height + gainY)
                );
        }

        /// <summary>
        /// Returns rectangle scaled to box.
        /// </summary>
        /// <param name="rectangles">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle[] ToBox(params Rectangle[] rectangles)
        {
            int length = rectangles.Length;
            var newRectangles = new Rectangle[length];

            for (int i = 0; i < length; i++)
            {
                newRectangles[i] = rectangles[i].ToBox();
            }

            return newRectangles;
        }
        /// <summary>
        /// Returns rectangle scaled to box.
        /// </summary>
        /// <param name="rectangles">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF[] ToBox(params RectangleF[] rectangles)
        {
            int length = rectangles.Length;
            var newRectangles = new RectangleF[length];

            for (int i = 0; i < length; i++)
            {
                newRectangles[i] = rectangles[i].ToBox();
            }

            return newRectangles;
        }

        /// <summary>
        /// Returns rectangle scaled to box with image size.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <param name="factor">Factor.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle[] ToBox(float factor, params Rectangle[] rectangles)
        {
            int length = rectangles.Length;
            var newRectangles = new Rectangle[length];

            for (int i = 0; i < length; i++)
            {
                newRectangles[i] = rectangles[i].ToBox(factor);
            }

            return newRectangles;
        }
        /// <summary>
        /// Returns rectangle scaled to box with image size.
        /// </summary>
        /// <param name="rectangles">Rectangles.</param>
        /// <param name="factor">Factor.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF[] ToBox(float factor, params RectangleF[] rectangles)
        {
            int length = rectangles.Length;
            var newRectangles = new RectangleF[length];

            for (int i = 0; i < length; i++)
            {
                newRectangles[i] = rectangles[i].ToBox(factor);
            }

            return newRectangles;
        }

        /// <summary>
        /// Implements IoU operator.
        /// </summary>
        /// <param name="a">First rectangle.</param>
        /// <param name="b">Second rectangle.</param>
        /// <returns>Value.</returns>
        public static float IoU(this Rectangle a, Rectangle b)
        {
            var xA = Math.Max(a.Left, b.Left);
            var yA = Math.Max(a.Top, b.Top);
            var xB = Math.Min(a.Right, b.Right);
            var yB = Math.Min(a.Bottom, b.Bottom);

            // Convert before multiplying so the intersection area cannot overflow Int32.
            var interArea = Math.Abs(Math.Max(xB - xA, 0) * (float)Math.Max(yB - yA, 0));

            if (interArea == 0)
                return 0;

            var boxAArea = Math.Abs((a.Right - a.Left) * (float)(a.Bottom - a.Top));
            var boxBArea = Math.Abs((b.Right - b.Left) * (float)(b.Bottom - b.Top));

            return interArea / (float)(boxAArea + boxBArea - interArea);
        }
        /// <summary>
        /// Implements IoU operator.
        /// </summary>
        /// <param name="a">First rectangle.</param>
        /// <param name="b">Second rectangle.</param>
        /// <returns>Value.</returns>
        public static float IoU(this RectangleF a, RectangleF b)
        {
            var xA = Math.Max(a.Left, b.Left);
            var yA = Math.Max(a.Top, b.Top);
            var xB = Math.Min(a.Right, b.Right);
            var yB = Math.Min(a.Bottom, b.Bottom);

            // Convert before multiplying so the intersection area cannot overflow Int32.
            var interArea = Math.Abs(Math.Max(xB - xA, 0) * (float)Math.Max(yB - yA, 0));

            if (interArea == 0)
                return 0;

            var boxAArea = Math.Abs((a.Right - a.Left) * (float)(a.Bottom - a.Top));
            var boxBArea = Math.Abs((b.Right - b.Left) * (float)(b.Bottom - b.Top));

            return interArea / (float)(boxAArea + boxBArea - interArea);
        }

        /// <summary>
        /// Implements scale operator.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="kx">Factor for x axis.</param>
        /// <param name="ky">Factor for y axis.</param>
        /// <returns></returns>
        public static Rectangle Scale(this Rectangle rectangle, float kx = 0.0f, float ky = 0.0f)
        {
            var x = rectangle.X;
            var y = rectangle.Y;
            var w = rectangle.Width;
            var h = rectangle.Height;

            var dw = (int)(w * kx);
            var dh = (int)(h * ky);

            return new Rectangle
            {
                X = x - dw / 2,
                Y = y - dh / 2,
                Width = w + dw,
                Height = h + dh,
            };
        }
        /// <summary>
        /// Implements scale operator.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <param name="kx">Factor for x axis.</param>
        /// <param name="ky">Factor for y axis.</param>
        /// <returns></returns>
        public static RectangleF Scale(this RectangleF rectangle, float kx = 0.0f, float ky = 0.0f)
        {
            var x = rectangle.X;
            var y = rectangle.Y;
            var w = rectangle.Width;
            var h = rectangle.Height;

            var dw = (int)(w * kx);
            var dh = (int)(h * ky);

            return new RectangleF
            {
                X = x - dw / 2,
                Y = y - dh / 2,
                Width = w + dw,
                Height = h + dh,
            };
        }

        /// <summary>
        /// Implements scale operator.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle Scale(this Rectangle rectangle)
        {
            var r = (int)Math.Sqrt(rectangle.Width * rectangle.Width + rectangle.Height * rectangle.Height);
            var dx = r - rectangle.Width;
            var dy = r - rectangle.Height;

            var x = rectangle.X - dx / 2;
            var y = rectangle.Y - dy / 2;
            var w = rectangle.Width + dx;
            var h = rectangle.Height + dy;

            return new Rectangle
            {
                X = x,
                Y = y,
                Width = w,
                Height = h
            };
        }
        /// <summary>
        /// Implements scale operator.
        /// </summary>
        /// <param name="rectangle">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF Scale(this RectangleF rectangle)
        {
            var r = (int)Math.Sqrt(rectangle.Width * rectangle.Width + rectangle.Height * rectangle.Height);
            var dx = r - rectangle.Width;
            var dy = r - rectangle.Height;

            var x = rectangle.X - dx / 2;
            var y = rectangle.Y - dy / 2;
            var w = rectangle.Width + dx;
            var h = rectangle.Height + dy;

            return new RectangleF
            {
                X = x,
                Y = y,
                Width = w,
                Height = h
            };
        }

        /// <summary>
        /// Implements clamp operator.
        /// </summary>
        /// <param name="first">Rectangle.</param>
        /// <param name="second">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle Clamp(this Rectangle first, Rectangle second)
        {
            if (first.Width < 0) { first.X += first.Width; first.Width = -first.Width; }
            if (first.Height < 0) { first.Y += first.Height; first.Height = -first.Height; }

            int x = Math.Max(first.X, second.Left);
            int y = Math.Max(first.Y, second.Top);

            int right = Math.Min(first.Right, second.Right);
            int bottom = Math.Min(first.Bottom, second.Bottom);

            int w = Math.Max(0, right - x);
            int h = Math.Max(0, bottom - y);

            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Implements clamp operator.
        /// </summary>
        /// <param name="first">Rectangle.</param>
        /// <param name="second">Rectangle.</param>
        /// <returns>Rectangle.</returns>
        public static RectangleF Clamp(this RectangleF first, RectangleF second)
        {
            if (first.Width < 0) { first.X += first.Width; first.Width = -first.Width; }
            if (first.Height < 0) { first.Y += first.Height; first.Height = -first.Height; }

            float x = Math.Max(first.X, second.Left);
            float y = Math.Max(first.Y, second.Top);

            float right = Math.Min(first.Right, second.Right);
            float bottom = Math.Min(first.Bottom, second.Bottom);

            float w = Math.Max(0, right - x);
            float h = Math.Max(0, bottom - y);

            return new RectangleF(x, y, w, h);
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Converts an integer rectangle to a drawing rectangle.
        /// </summary>
        /// <param name="rectangle">Integer rectangle.</param>
        /// <returns>Rectangle with the same position and size.</returns>
        public static Rectangle ToRectangle(this RectangleInt rectangle)
        {
            return new Rectangle
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }
        /// <summary>
        /// Converts a floating-point rectangle to a drawing rectangle.
        /// </summary>
        /// <param name="rectangle">Floating-point rectangle.</param>
        /// <returns>Rectangle with the same position and size.</returns>
        public static RectangleF ToRectangle(this RectangleFloat rectangle)
        {
            return new RectangleF
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        #endregion
    }
}
