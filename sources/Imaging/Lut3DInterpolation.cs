using System;

namespace UMapx.Imaging
{
    /// <summary>
    /// Defines the interpolation used by a three-dimensional color lookup table.
    /// </summary>
    [Serializable]
    public enum Lut3DInterpolation
    {
        /// <summary>
        /// Interpolates the eight corners of a cube.
        /// </summary>
        Trilinear,
        /// <summary>
        /// Interpolates four vertices of one of the six tetrahedra in a cube.
        /// </summary>
        Tetrahedral
    }
}
