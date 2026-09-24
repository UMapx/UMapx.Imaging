using System;

namespace UMapx.Imaging
{
    /// <summary>
    /// Defines the radial blur sampling path.
    /// </summary>
    [Serializable]
    public enum RadialBlurMode
    {
        /// <summary>
        /// Rotates samples around the center.
        /// </summary>
        Spin,
        /// <summary>
        /// Moves samples towards the center.
        /// </summary>
        Zoom
    }
}
