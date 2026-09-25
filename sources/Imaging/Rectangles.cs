using System;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Provides rectangle translation, geometry, resizing, clipping, and conversion operations.
    /// </summary>
    public static partial class Rectangles
    {
        #region Operators

        /// <summary>
        /// Translates a rectangle by adding the specified offset to its position.
        /// </summary>
        /// <param name="rectangle">Rectangle to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to add.</param>
        /// <returns>Translated rectangle with the original width and height.</returns>
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
        /// Translates a rectangle by adding the specified offset to its position.
        /// </summary>
        /// <param name="rectangle">Rectangle to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to add.</param>
        /// <returns>Translated rectangle with the original width and height.</returns>
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
        /// Translates a rectangle by subtracting the specified offset from its position.
        /// </summary>
        /// <param name="rectangle">Rectangle to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to subtract.</param>
        /// <returns>Translated rectangle with the original width and height.</returns>
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
        /// Translates a rectangle by subtracting the specified offset from its position.
        /// </summary>
        /// <param name="rectangle">Rectangle to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to subtract.</param>
        /// <returns>Translated rectangle with the original width and height.</returns>
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
        /// Translates each rectangle by adding the specified offset to its position.
        /// </summary>
        /// <param name="rectangles">Rectangles to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to add.</param>
        /// <returns>New array of translated rectangles in the original order, with unchanged sizes.</returns>
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
        /// Translates each rectangle by adding the specified offset to its position.
        /// </summary>
        /// <param name="rectangles">Rectangles to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to add.</param>
        /// <returns>New array of translated rectangles in the original order, with unchanged sizes.</returns>
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
        /// Translates each rectangle by subtracting the specified offset from its position.
        /// </summary>
        /// <param name="rectangles">Rectangles to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to subtract.</param>
        /// <returns>New array of translated rectangles in the original order, with unchanged sizes.</returns>
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
        /// Translates each rectangle by subtracting the specified offset from its position.
        /// </summary>
        /// <param name="rectangles">Rectangles to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to subtract.</param>
        /// <returns>New array of translated rectangles in the original order, with unchanged sizes.</returns>
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
        /// Returns the four corners of a rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle whose corners are returned.</param>
        /// <returns>New array containing the top-left, top-right, bottom-right, and bottom-left corners, in that order.</returns>
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
        /// Returns the four corners of a rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle whose corners are returned.</param>
        /// <returns>New array containing the top-left, top-right, bottom-right, and bottom-left corners, in that order.</returns>
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
        /// Creates a rectangle from an array of four corner points.
        /// </summary>
        /// <param name="points">Four points ordered as top-left, top-right, bottom-right, and bottom-left.</param>
        /// <returns>Rectangle with its left and top edges taken from the first point and its right and bottom edges from the third point.</returns>
        /// <remarks>
        /// The second and fourth points are not used or validated.
        /// </remarks>
        /// <exception cref="ArgumentException">The array does not contain exactly four points.</exception>
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
        /// Creates a rectangle from an array of four corner points.
        /// </summary>
        /// <param name="points">Four points ordered as top-left, top-right, bottom-right, and bottom-left.</param>
        /// <returns>Rectangle with its left and top edges taken from the first point and its right and bottom edges from the third point.</returns>
        /// <remarks>
        /// The second and fourth points are not used or validated.
        /// </remarks>
        /// <exception cref="ArgumentException">The array does not contain exactly four points.</exception>
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
        /// Returns the location of a rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle whose location is returned.</param>
        /// <returns>Point containing the rectangle's X and Y coordinates.</returns>
        public static Point GetPoint(this Rectangle rectangle)
        {
            return new Point
            {
                X = rectangle.X,
                Y = rectangle.Y
            };
        }
        /// <summary>
        /// Returns the location of a rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle whose location is returned.</param>
        /// <returns>Point containing the rectangle's X and Y coordinates.</returns>
        public static PointF GetPoint(this RectangleF rectangle)
        {
            return new PointF
            {
                X = rectangle.X,
                Y = rectangle.Y
            };
        }

        /// <summary>
        /// Calculates the product of a size's width and height.
        /// </summary>
        /// <param name="size">Size whose area is calculated.</param>
        /// <returns>Width multiplied by height, without taking the absolute value.</returns>
        public static int Area(this Size size)
        {
            return size.Width * size.Height;
        }
        /// <summary>
        /// Calculates the product of a size's width and height.
        /// </summary>
        /// <param name="size">Size whose area is calculated.</param>
        /// <returns>Width multiplied by height, without taking the absolute value.</returns>
        public static float Area(this SizeF size)
        {
            return size.Width * size.Height;
        }
        /// <summary>
        /// Calculates the product of a rectangle's width and height.
        /// </summary>
        /// <param name="rectangle">Rectangle whose area is calculated.</param>
        /// <returns>Width multiplied by height, without taking the absolute value.</returns>
        public static int Area(this Rectangle rectangle)
        {
            return rectangle.Width * rectangle.Height;
        }
        /// <summary>
        /// Calculates the product of a rectangle's width and height.
        /// </summary>
        /// <param name="rectangle">Rectangle whose area is calculated.</param>
        /// <returns>Width multiplied by height, without taking the absolute value.</returns>
        public static float Area(this RectangleF rectangle)
        {
            return rectangle.Width * rectangle.Height;
        }

        /// <summary>
        /// Selects the rectangle with the largest width-height product.
        /// </summary>
        /// <param name="rectangles">Rectangles to compare by area.</param>
        /// <returns>First rectangle with the largest qualifying area, or the first input rectangle if none qualifies; <see cref="Rectangle.Empty"/> for an empty array.</returns>
        /// <remarks>
        /// Rectangles whose <see cref="Rectangle.IsEmpty"/> property is true are skipped.
        /// Only areas greater than <see cref="int.MinValue"/> qualify for selection.
        /// </remarks>
        public static Rectangle Max(params Rectangle[] rectangles)
        {
            // Initialize the area comparison and fallback index.
            var length = rectangles.Length;
            var rectangle = Rectangle.Empty;
            var area = int.MinValue;
            var max = 0;

            // Keep the first rectangle with the largest qualifying area.
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

            // Fall back to the first input rectangle, or Empty for an empty array.
            return length > 0 ? rectangles[max] : rectangle;
        }
        /// <summary>
        /// Selects the rectangle with the largest width-height product.
        /// </summary>
        /// <param name="rectangles">Rectangles to compare by area.</param>
        /// <returns>First rectangle with the largest qualifying area, or the first input rectangle if none qualifies; <see cref="RectangleF.Empty"/> for an empty array.</returns>
        /// <remarks>
        /// Rectangles whose <see cref="RectangleF.IsEmpty"/> property is true are skipped.
        /// Only areas greater than <see cref="float.MinValue"/> qualify for selection. NaN areas are ignored.
        /// </remarks>
        public static RectangleF Max(params RectangleF[] rectangles)
        {
            // Initialize the area comparison and fallback index.
            var length = rectangles.Length;
            var rectangle = RectangleF.Empty;
            var area = float.MinValue;
            var max = 0;

            // Keep the first rectangle with the largest qualifying area.
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

            // Fall back to the first input rectangle, or Empty for an empty array.
            return length > 0 ? rectangles[max] : rectangle;
        }

        /// <summary>
        /// Selects the rectangle with the smallest width-height product.
        /// </summary>
        /// <param name="rectangles">Rectangles to compare by area.</param>
        /// <returns>First rectangle with the smallest qualifying area, or <see cref="Rectangle.Empty"/> if none qualifies.</returns>
        /// <remarks>
        /// Rectangles whose <see cref="Rectangle.IsEmpty"/> property is true are skipped.
        /// Only areas less than <see cref="int.MaxValue"/> qualify for selection.
        /// </remarks>
        public static Rectangle Min(params Rectangle[] rectangles)
        {
            // Initialize the area comparison with no rectangle selected.
            var length = rectangles.Length;
            var rectangle = Rectangle.Empty;
            var area = int.MaxValue;
            var min = -1;

            // Keep the first rectangle with the smallest qualifying area.
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

            // Return Empty if no rectangle was selected.
            return min >= 0 ? rectangles[min] : Rectangle.Empty;
        }
        /// <summary>
        /// Selects the rectangle with the smallest width-height product.
        /// </summary>
        /// <param name="rectangles">Rectangles to compare by area.</param>
        /// <returns>First rectangle with the smallest qualifying area, or <see cref="RectangleF.Empty"/> if none qualifies.</returns>
        /// <remarks>
        /// Rectangles whose <see cref="RectangleF.IsEmpty"/> property is true are skipped.
        /// Only areas less than <see cref="float.MaxValue"/> qualify for selection. NaN areas are ignored.
        /// </remarks>
        public static RectangleF Min(params RectangleF[] rectangles)
        {
            // Initialize the area comparison with no rectangle selected.
            var length = rectangles.Length;
            var rectangle = RectangleF.Empty;
            var area = float.MaxValue;
            var min = -1;

            // Keep the first rectangle with the smallest qualifying area.
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

            // Return Empty if no rectangle was selected.
            return min >= 0 ? rectangles[min] : Rectangle.Empty;
        }

        /// <summary>
        /// Converts a rectangle to a square using its larger dimension.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Square with a side equal to the larger of the original width and height.</returns>
        /// <remarks>
        /// Half of each dimension increase is subtracted from the corresponding coordinate using integer division, so the center may shift by half a unit.
        /// </remarks>
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
        /// Converts a rectangle to a square using its larger dimension.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Square with a side equal to the larger of the original width and height.</returns>
        /// <remarks>
        /// The original center is preserved.
        /// </remarks>
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
        /// Resizes a rectangle by a relative change in both dimensions.
        /// </summary>
        /// <param name="rectangle">Rectangle to resize.</param>
        /// <param name="scale">Relative change in width and height; for example, 0.1 increases both by 10% before truncation.</param>
        /// <returns>Resized rectangle with coordinates and dimensions truncated toward zero to integers.</returns>
        /// <remarks>
        /// Before truncation, each dimension is multiplied by 1 plus <paramref name="scale"/> and the center is preserved.
        /// </remarks>
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
        /// Resizes a rectangle by a relative change in both dimensions.
        /// </summary>
        /// <param name="rectangle">Rectangle to resize.</param>
        /// <param name="scale">Relative change in width and height; for example, 0.1 increases both by 10% before truncation.</param>
        /// <returns>Resized rectangle with coordinates and dimensions truncated toward zero to integers.</returns>
        /// <remarks>
        /// Before truncation, each dimension is multiplied by 1 plus <paramref name="scale"/> and the center is preserved.
        /// This truncation also applies to the floating-point result.
        /// </remarks>
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
        /// Converts each rectangle to a square using its larger dimension.
        /// </summary>
        /// <param name="rectangles">Rectangles to convert.</param>
        /// <returns>New array of squares in the original order, each with a side equal to the larger original dimension.</returns>
        /// <remarks>
        /// Half of each dimension increase is subtracted from the corresponding coordinate using integer division, so the center may shift by half a unit.
        /// </remarks>
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
        /// Converts each rectangle to a square using its larger dimension.
        /// </summary>
        /// <param name="rectangles">Rectangles to convert.</param>
        /// <returns>New array of squares in the original order, each with a side equal to the larger original dimension.</returns>
        /// <remarks>
        /// The original center is preserved.
        /// </remarks>
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
        /// Resizes each rectangle by a relative change in both dimensions.
        /// </summary>
        /// <param name="factor">Relative change in width and height; for example, 0.1 increases both by 10% before truncation.</param>
        /// <param name="rectangles">Rectangles to resize.</param>
        /// <returns>New array of resized rectangles in the original order.</returns>
        /// <remarks>
        /// Before truncation, each dimension is multiplied by 1 plus <paramref name="factor"/> and the center is preserved.
        /// Each rectangle's coordinates and dimensions are truncated toward zero to integers.
        /// </remarks>
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
        /// Resizes each rectangle by a relative change in both dimensions.
        /// </summary>
        /// <param name="factor">Relative change in width and height; for example, 0.1 increases both by 10% before truncation.</param>
        /// <param name="rectangles">Rectangles to resize.</param>
        /// <returns>New array of resized rectangles in the original order.</returns>
        /// <remarks>
        /// Before truncation, each dimension is multiplied by 1 plus <paramref name="factor"/> and the center is preserved.
        /// Each rectangle's coordinates and dimensions are truncated toward zero to integers.
        /// This truncation also applies to the floating-point result.
        /// </remarks>
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
        /// Calculates the intersection-over-union (IoU) ratio of two rectangles.
        /// </summary>
        /// <param name="a">First rectangle, with nonnegative width and height.</param>
        /// <param name="b">Second rectangle, with nonnegative width and height.</param>
        /// <returns>Intersection area divided by union area, or zero if the intersection area is zero.</returns>
        public static float IoU(this Rectangle a, Rectangle b)
        {
            var xA = Math.Max(a.Left, b.Left);
            var yA = Math.Max(a.Top, b.Top);
            var xB = Math.Min(a.Right, b.Right);
            var yB = Math.Min(a.Bottom, b.Bottom);

            // Convert one factor to float so area multiplication does not overflow Int32.
            var interArea = Math.Abs(Math.Max(xB - xA, 0) * (float)Math.Max(yB - yA, 0));

            if (interArea == 0)
                return 0;

            var boxAArea = Math.Abs((a.Right - a.Left) * (float)(a.Bottom - a.Top));
            var boxBArea = Math.Abs((b.Right - b.Left) * (float)(b.Bottom - b.Top));

            return interArea / (float)(boxAArea + boxBArea - interArea);
        }
        /// <summary>
        /// Calculates the intersection-over-union (IoU) ratio of two rectangles.
        /// </summary>
        /// <param name="a">First rectangle, with nonnegative width and height.</param>
        /// <param name="b">Second rectangle, with nonnegative width and height.</param>
        /// <returns>Intersection area divided by union area, or zero if the intersection area is zero.</returns>
        public static float IoU(this RectangleF a, RectangleF b)
        {
            var xA = Math.Max(a.Left, b.Left);
            var yA = Math.Max(a.Top, b.Top);
            var xB = Math.Min(a.Right, b.Right);
            var yB = Math.Min(a.Bottom, b.Bottom);

            // Clamp intersection dimensions to zero when the rectangles do not overlap.
            var interArea = Math.Abs(Math.Max(xB - xA, 0) * (float)Math.Max(yB - yA, 0));

            if (interArea == 0)
                return 0;

            var boxAArea = Math.Abs((a.Right - a.Left) * (float)(a.Bottom - a.Top));
            var boxBArea = Math.Abs((b.Right - b.Left) * (float)(b.Bottom - b.Top));

            return interArea / (float)(boxAArea + boxBArea - interArea);
        }

        /// <summary>
        /// Resizes a rectangle by independent relative changes in width and height.
        /// </summary>
        /// <param name="rectangle">Rectangle to resize.</param>
        /// <param name="kx">Relative width change; for example, 0.1 adds 10% of the width before truncation. Defaults to zero.</param>
        /// <param name="ky">Relative height change; for example, 0.1 adds 10% of the height before truncation. Defaults to zero.</param>
        /// <returns>Rectangle with the computed dimension changes added and half of each change subtracted from its position.</returns>
        /// <remarks>
        /// Width and height changes are truncated toward zero to integers before being added.
        /// Position offsets use integer division by two, so the center may shift by half a unit.
        /// </remarks>
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
        /// Resizes a rectangle by independent relative changes in width and height.
        /// </summary>
        /// <param name="rectangle">Rectangle to resize.</param>
        /// <param name="kx">Relative width change; for example, 0.1 adds 10% of the width before truncation. Defaults to zero.</param>
        /// <param name="ky">Relative height change; for example, 0.1 adds 10% of the height before truncation. Defaults to zero.</param>
        /// <returns>Rectangle with the computed dimension changes added and half of each change subtracted from its position.</returns>
        /// <remarks>
        /// Width and height changes are truncated toward zero to integers before being added.
        /// Position offsets use integer division by two, so the center may shift by half a unit. These integer calculations also apply to floating-point rectangles.
        /// </remarks>
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
        /// Converts a rectangle to a square using its diagonal length.
        /// </summary>
        /// <param name="rectangle">Rectangle whose diagonal determines the square's size.</param>
        /// <returns>Square with a side equal to the original diagonal length truncated toward zero to an integer.</returns>
        /// <remarks>
        /// Position offsets use integer division by two, so the center may shift by half a unit.
        /// </remarks>
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
        /// Converts a rectangle to a square using its diagonal length.
        /// </summary>
        /// <param name="rectangle">Rectangle whose diagonal determines the square's size.</param>
        /// <returns>Square with a side equal to the original diagonal length truncated toward zero to an integer.</returns>
        /// <remarks>
        /// The original center is preserved; the side length is still truncated to an integer.
        /// </remarks>
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
        /// Clips a rectangle against the bounds of another rectangle.
        /// </summary>
        /// <param name="first">Rectangle to clip; negative dimensions are normalized by moving its position.</param>
        /// <param name="second">Clipping bounds, expected to have nonnegative width and height.</param>
        /// <returns>Rectangle starting at the maximum left and top coordinates, with each intersection dimension clamped to zero.</returns>
        /// <remarks>
        /// Only the first rectangle is normalized.
        /// A disjoint result has zero width or height, but its position is retained and it need not equal <see cref="Rectangle.Empty"/>.
        /// </remarks>
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
        /// Clips a rectangle against the bounds of another rectangle.
        /// </summary>
        /// <param name="first">Rectangle to clip; negative dimensions are normalized by moving its position.</param>
        /// <param name="second">Clipping bounds, expected to have nonnegative width and height.</param>
        /// <returns>Rectangle starting at the maximum left and top coordinates, with each intersection dimension clamped to zero.</returns>
        /// <remarks>
        /// Only the first rectangle is normalized.
        /// A disjoint result has zero width or height, but its position is retained and it need not equal <see cref="RectangleF.Empty"/>.
        /// </remarks>
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
        /// Converts a <see cref="RectangleInt"/> to a <see cref="Rectangle"/>.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Rectangle with the same position, width, and height.</returns>
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
        /// Converts a <see cref="RectangleFloat"/> to a <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Rectangle with the same position, width, and height.</returns>
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

        /// <summary>
        /// Converts a <see cref="Rectangle"/> to a <see cref="RectangleInt"/>.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Rectangle with the same position, width, and height.</returns>
        public static RectangleInt FromRectangle(this Rectangle rectangle)
        {
            return new RectangleInt
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }
        /// <summary>
        /// Converts a <see cref="RectangleF"/> to a <see cref="RectangleFloat"/>.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Rectangle with the same position, width, and height.</returns>
        public static RectangleFloat FromRectangle(this RectangleF rectangle)
        {
            return new RectangleFloat
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
