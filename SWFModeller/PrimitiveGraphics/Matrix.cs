using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeeWorld.SWFProcessing.SWFModeller
{
    /// <summary>
    /// A 2D matrix
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// Transpose X value
        /// </summary>
        public float TransX { get; set; }

        /// <summary>
        /// Transpose Y value
        /// </summary>
        public float TransY { get; set; }

        /// <summary>
        /// Scale factor for X axis
        /// </summary>
        public float ScaleX { get; set; }

        /// <summary>
        /// Scale factor for Y axis
        /// </summary>
        public float ScaleY { get; set; }

        /// <summary>
        /// Sheer amount for X axis
        /// </summary>
        public float SkewX { get; set; }

        /// <summary>
        /// Sheer amount for Y axis
        /// </summary>
        public float SkewY { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "[" + TransX + "," + TransY + "," + ScaleX + "," + ScaleY + "," + SkewX + "," + SkewY + "]";
        }
    }
}
