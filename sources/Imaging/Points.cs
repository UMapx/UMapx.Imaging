using System;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Imaging
{
    /// <summary>
    /// Provides operations for translating, rotating, bounding, and averaging points, and calculating angles.
    /// </summary>
    public static partial class Points
    {
        #region Operators

        /// <summary>
        /// Translates each point by adding the specified coordinate offsets.
        /// </summary>
        /// <param name="points">Points to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to add.</param>
        /// <returns>New array containing the translated points in the original order.</returns>
        public static Point[] Add(this Point[] points, Point point)
        {
            var count = points.Length;
            var output = new Point[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = new Point
                {
                    X = points[i].X + point.X,
                    Y = points[i].Y + point.Y
                };
            }

            return output;
        }
        /// <summary>
        /// Translates each point by adding the specified coordinate offsets.
        /// </summary>
        /// <param name="points">Points to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to add.</param>
        /// <returns>New array containing the translated points in the original order.</returns>
        public static PointF[] Add(this PointF[] points, PointF point)
        {
            var count = points.Length;
            var output = new PointF[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = new PointF
                {
                    X = points[i].X + point.X,
                    Y = points[i].Y + point.Y
                };
            }

            return output;
        }

        /// <summary>
        /// Translates each point by subtracting the specified coordinate offsets.
        /// </summary>
        /// <param name="points">Points to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to subtract.</param>
        /// <returns>New array containing the translated points in the original order.</returns>
        public static Point[] Sub(this Point[] points, Point point)
        {
            var count = points.Length;
            var output = new Point[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = new Point
                {
                    X = points[i].X - point.X,
                    Y = points[i].Y - point.Y
                };
            }

            return output;
        }
        /// <summary>
        /// Translates each point by subtracting the specified coordinate offsets.
        /// </summary>
        /// <param name="points">Points to translate.</param>
        /// <param name="point">Horizontal and vertical offsets to subtract.</param>
        /// <returns>New array containing the translated points in the original order.</returns>
        public static PointF[] Sub(this PointF[] points, PointF point)
        {
            var count = points.Length;
            var output = new PointF[count];

            for (int i = 0; i < count; i++)
            {
                output[i] = new PointF
                {
                    X = points[i].X - point.X,
                    Y = points[i].Y - point.Y
                };
            }

            return output;
        }

        #endregion

        #region Special operators

        /// <summary>
        /// Rotates each point around the specified center.
        /// </summary>
        /// <param name="points">Points to rotate.</param>
        /// <param name="centerPoint">Center of rotation.</param>
        /// <param name="angle">Rotation angle in degrees.</param>
        /// <returns>New array containing the rotated points in the original order.</returns>
        /// <remarks>
        /// Positive angles rotate counterclockwise when Y increases upward, or clockwise in image coordinates where Y increases downward.
        /// Each resulting coordinate is truncated toward zero to an integer.
        /// </remarks>
        public static Point[] Rotate(this Point[] points, Point centerPoint, float angle)
        {
            int length = points.Length;
            var output = new Point[length];

            for (int i = 0; i < length; i++)
            {
                output[i] = points[i].Rotate(centerPoint, angle);
            }

            return output;
        }
        /// <summary>
        /// Rotates each point around the specified center.
        /// </summary>
        /// <param name="points">Points to rotate.</param>
        /// <param name="centerPoint">Center of rotation.</param>
        /// <param name="angle">Rotation angle in degrees.</param>
        /// <returns>New array containing the rotated points in the original order.</returns>
        /// <remarks>
        /// Positive angles rotate counterclockwise when Y increases upward, or clockwise in image coordinates where Y increases downward.
        /// The resulting coordinates are converted to single-precision floating-point values.
        /// </remarks>
        public static PointF[] Rotate(this PointF[] points, PointF centerPoint, float angle)
        {
            int length = points.Length;
            var output = new PointF[length];

            for (int i = 0; i < length; i++)
            {
                output[i] = points[i].Rotate(centerPoint, angle);
            }

            return output;
        }

        /// <summary>
        /// Rotates a point around the specified center.
        /// </summary>
        /// <param name="pointToRotate">Point to rotate.</param>
        /// <param name="centerPoint">Center of rotation.</param>
        /// <param name="angleInDegrees">Rotation angle in degrees.</param>
        /// <returns>Rotated point with coordinates truncated toward zero to integers.</returns>
        /// <remarks>
        /// Positive angles rotate counterclockwise when Y increases upward, or clockwise in image coordinates where Y increases downward.
        /// Each resulting coordinate is truncated toward zero to an integer.
        /// </remarks>
        public static Point Rotate(this Point pointToRotate, Point centerPoint, float angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            return new Point
            {
                X =
                    (int)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (int)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }
        /// <summary>
        /// Rotates a point around the specified center.
        /// </summary>
        /// <param name="pointToRotate">Point to rotate.</param>
        /// <param name="centerPoint">Center of rotation.</param>
        /// <param name="angleInDegrees">Rotation angle in degrees.</param>
        /// <returns>Rotated point with single-precision floating-point coordinates.</returns>
        /// <remarks>
        /// Positive angles rotate counterclockwise when Y increases upward, or clockwise in image coordinates where Y increases downward.
        /// The resulting coordinates are converted to single-precision floating-point values.
        /// </remarks>
        public static PointF Rotate(this PointF pointToRotate, PointF centerPoint, float angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            return new PointF
            {
                X =
                    (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (float)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        /// <summary>
        /// Calculates the axis-aligned coordinate bounds of a point array.
        /// </summary>
        /// <param name="points">Nonempty array of points to bound.</param>
        /// <returns>Rectangle whose left and top edges are the minimum coordinates and whose width and height are the coordinate spans.</returns>
        /// <remarks>
        /// Width and height are calculated as maximum minus minimum, without adding an extra pixel.
        /// Empty arrays are not validated and do not produce meaningful coordinate bounds.
        /// </remarks>
        public static Rectangle GetRectangle(this Point[] points)
        {
            int length = points.Length;
            int xmin = int.MaxValue;
            int ymin = int.MaxValue;
            int xmax = int.MinValue;
            int ymax = int.MinValue;

            for (int i = 0; i < length; i++)
            {
                int x = points[i].X;
                int y = points[i].Y;

                if (x < xmin)
                    xmin = x;
                if (y < ymin)
                    ymin = y;
                if (x > xmax)
                    xmax = x;
                if (y > ymax)
                    ymax = y;
            }

            return new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin);
        }
        /// <summary>
        /// Calculates the axis-aligned coordinate bounds of a point array.
        /// </summary>
        /// <param name="points">Nonempty array of points with finite coordinates to bound.</param>
        /// <returns>Rectangle whose left and top edges are the minimum coordinates and whose width and height are the coordinate spans.</returns>
        /// <remarks>
        /// Width and height are calculated as maximum minus minimum, without adding an extra pixel.
        /// Empty arrays are not validated and do not produce meaningful coordinate bounds.
        /// </remarks>
        public static RectangleF GetRectangle(this PointF[] points)
        {
            int length = points.Length;
            float xmin = float.MaxValue;
            float ymin = float.MaxValue;
            float xmax = float.MinValue;
            float ymax = float.MinValue;

            for (int i = 0; i < length; i++)
            {
                float x = points[i].X;
                float y = points[i].Y;

                if (x < xmin)
                    xmin = x;
                if (y < ymin)
                    ymin = y;
                if (x > xmax)
                    xmax = x;
                if (y > ymax)
                    ymax = y;
            }

            return new RectangleF(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        /// <summary>
        /// Calculates a signed angle at the left point using the support and right points.
        /// </summary>
        /// <param name="left">Vertex of the angle; its Y coordinate also determines the sign multiplier.</param>
        /// <param name="right">Point defining one ray from the vertex.</param>
        /// <param name="support">Point defining the other ray from the vertex.</param>
        /// <returns>Signed angle in approximate degrees, or NaN if the argument passed to the inverse cosine is outside its valid range.</returns>
        /// <remarks>
        /// The result is <c>sign * (180 - 57.3 * acos(d))</c>, where d is the normalized dot product
        /// of the vectors <c>left - support</c> and <c>right - left</c>.
        /// The sign multiplier is +1 when <c>left.Y &gt; right.Y</c>, and -1 otherwise.
        /// The value 57.3 approximates the radians-to-degrees conversion factor.
        /// Coincident points are not rejected; zero-over-zero divisions use the special handling in the private division helper.
        /// The normalized dot product is not clamped to the interval [-1, 1].
        /// </remarks>
        public static float GetAngle(this Point left, Point right, Point support)
        {
            double kk = left.Y > right.Y ? 1 : -1;

            double x1 = left.X - support.X;
            double y1 = left.Y - support.Y;

            double x2 = right.X - left.X;
            double y2 = right.Y - left.Y;

            double a = Math.Sqrt(x1 * x1 + y1 * y1);
            double b = Math.Sqrt(x2 * x2 + y2 * y2);
            double c = x1 * x2 + y1 * y2;

            double d = c.Div(a).Div(b);

            return (float)(kk * (180.0 - Math.Acos(d) * 57.3));
        }
        /// <summary>
        /// Calculates a signed angle at the left point using the support and right points.
        /// </summary>
        /// <param name="left">Vertex of the angle; its Y coordinate also determines the sign multiplier.</param>
        /// <param name="right">Point defining one ray from the vertex.</param>
        /// <param name="support">Point defining the other ray from the vertex.</param>
        /// <returns>Signed angle in approximate degrees, or NaN if the argument passed to the inverse cosine is outside its valid range.</returns>
        /// <remarks>
        /// The result is <c>sign * (180 - 57.3 * acos(d))</c>, where d is the normalized dot product
        /// of the vectors <c>left - support</c> and <c>right - left</c>.
        /// The sign multiplier is +1 when <c>left.Y &gt; right.Y</c>, and -1 otherwise.
        /// The value 57.3 approximates the radians-to-degrees conversion factor.
        /// Coincident points are not rejected; zero-over-zero divisions use the special handling in the private division helper.
        /// The normalized dot product is not clamped to the interval [-1, 1].
        /// </remarks>
        public static float GetAngle(this PointF left, PointF right, PointF support)
        {
            double kk = left.Y > right.Y ? 1 : -1;

            double x1 = left.X - support.X;
            double y1 = left.Y - support.Y;

            double x2 = right.X - left.X;
            double y2 = right.Y - left.Y;

            double a = Math.Sqrt(x1 * x1 + y1 * y1);
            double b = Math.Sqrt(x2 * x2 + y2 * y2);
            double c = x1 * x2 + y1 * y2;

            double d = c.Div(a).Div(b);

            return (float)(kk * (180.0 - Math.Acos(d) * 57.3));
        }

        /// <summary>
        /// Creates a point by combining coordinates from two points.
        /// </summary>
        /// <param name="left">Point supplying the Y coordinate.</param>
        /// <param name="right">Point supplying the X coordinate.</param>
        /// <returns>Point with coordinates <c>(right.X, left.Y)</c>.</returns>
        public static Point GetSupportedPoint(this Point left, Point right)
        {
            return new Point(right.X, left.Y);
        }
        /// <summary>
        /// Creates a point by combining coordinates from two points.
        /// </summary>
        /// <param name="left">Point supplying the Y coordinate.</param>
        /// <param name="right">Point supplying the X coordinate.</param>
        /// <returns>Point with coordinates <c>(right.X, left.Y)</c>.</returns>
        public static PointF GetSupportedPoint(this PointF left, PointF right)
        {
            return new PointF(right.X, left.Y);
        }

        /// <summary>
        /// Calculates the arithmetic mean of the point coordinates.
        /// </summary>
        /// <param name="points">Nonempty array of points to average.</param>
        /// <returns>Point containing the mean X and Y coordinates, with integer division truncating each result toward zero.</returns>
        /// <remarks>
        /// Coordinate sums and division use 32-bit integer arithmetic.
        /// </remarks>
        /// <exception cref="DivideByZeroException">The array contains no points.</exception>
        public static Point GetMeanPoint(params Point[] points)
        {
            var point = new Point(0, 0);
            var length = points.Length;

            for (int i = 0; i < length; i++)
            {
                point.X += points[i].X;
                point.Y += points[i].Y;
            }

            point.X /= length;
            point.Y /= length;

            return point;
        }
        /// <summary>
        /// Calculates the arithmetic mean of the point coordinates.
        /// </summary>
        /// <param name="points">Points to average.</param>
        /// <returns>Point containing the mean X and Y coordinates, or NaN in both coordinates for an empty array.</returns>
        /// <remarks>
        /// Coordinate sums and division use single-precision floating-point arithmetic.
        /// </remarks>
        public static PointF GetMeanPoint(params PointF[] points)
        {
            var point = new PointF(0, 0);
            var length = points.Length;

            for (int i = 0; i < length; i++)
            {
                point.X += points[i].X;
                point.Y += points[i].Y;
            }

            point.X /= length;
            point.Y /= length;

            return point;
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Converts a <see cref="PointInt"/> to a <see cref="Point"/>.
        /// </summary>
        /// <param name="point">Point to convert.</param>
        /// <returns>Point with the same X and Y coordinates.</returns>
        public static Point ToPoint(this PointInt point)
        {
            return new Point
            {
                X = point.X,
                Y = point.Y
            };
        }
        /// <summary>
        /// Converts a <see cref="PointFloat"/> to a <see cref="PointF"/>.
        /// </summary>
        /// <param name="point">Point to convert.</param>
        /// <returns>Point with the same X and Y coordinates.</returns>
        public static PointF ToPoint(this PointFloat point)
        {
            return new PointF
            {
                X = point.X,
                Y = point.Y
            };
        }

        /// <summary>
        /// Converts a <see cref="Point"/> to a <see cref="PointInt"/>.
        /// </summary>
        /// <param name="point">Point to convert.</param>
        /// <returns>Point with the same X and Y coordinates.</returns>
        public static PointInt FromPoint(this Point point)
        {
            return new PointInt
            {
                X = point.X,
                Y = point.Y
            };
        }
        /// <summary>
        /// Converts a <see cref="PointF"/> to a <see cref="PointFloat"/>.
        /// </summary>
        /// <param name="point">Point to convert.</param>
        /// <returns>Point with the same X and Y coordinates.</returns>
        public static PointFloat FromPoint(this PointF point)
        {
            return new PointFloat
            {
                X = point.X,
                Y = point.Y
            };
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Divides two values, substituting the smallest positive double value for zero divided by zero.
        /// </summary>
        /// <param name="a">Numerator.</param>
        /// <param name="b">Denominator.</param>
        /// <returns><see cref="double.Epsilon"/> when both operands are zero; otherwise, the result of dividing the numerator by the denominator.</returns>
        /// <remarks>
        /// All other inputs follow normal floating-point division rules, including infinity and NaN results.
        /// </remarks>
        private static double Div(this double a, double b)
        {
            if (a == 0 && b == 0)
            {
                return double.Epsilon;
            }

            return a / b;
        }

        #endregion
    }
}
