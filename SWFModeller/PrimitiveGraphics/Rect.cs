using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeeWorld.SWFProcessing.SWFModeller
{
    /// <summary>
    /// A rectangle.
    /// </summary>
    public class Rect
    {
        /// <summary>
        /// The left edge of the rectangle
        /// </summary>
        public float XMin { get; set; }

        /// <summary>
        /// The right edge of the rectangle
        /// </summary>
        public float XMax { get; set; }

        /// <summary>
        /// The top edge of the rectangle
        /// </summary>
        public float YMin { get; set; }

        /// <summary>
        /// The bottom edge of the rectangle
        /// </summary>
        public float YMax { get; set; }

        /// <summary>
        /// The height in pixels.
        /// </summary>
        public float Height
        {
            get
            {
                return YMax - YMin;
            }
        }

        /// <summary>
        /// The width in pixels.
        /// </summary>
        public float Width
        {
            get
            {
                return XMax - XMin;
            }
        }

        /// <summary>
        /// Construct a new rectangle
        /// </summary>
        /// <param name="xMin">Left-hand edge, in pixels</param>
        /// <param name="xMax">Right-hand edge, in pixels</param>
        /// <param name="yMin">Top edge, in pixels</param>
        /// <param name="yMax">Bottom edge, in pixels</param>
        public Rect(float xMin, float xMax, float yMin, float yMax)
        {
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[Rect " + XMin + "," + YMin + "->" + XMax + "," + YMax + "]";
        }
    }
}
