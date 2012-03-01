//-----------------------------------------------------------------------
// Rect.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.ModellingUtils.Geom
{
    /// <summary>
    /// A rectangle.
    /// </summary>
    public class Rect
    {
        /// <summary>
        /// Initializes a new instance of a rectangle
        /// </summary>
        /// <param name="xmin">Left-hand edge, in pixels</param>
        /// <param name="xmax">Right-hand edge, in pixels</param>
        /// <param name="ymin">Top edge, in pixels</param>
        /// <param name="ymax">Bottom edge, in pixels</param>
        public Rect(float xmin, float xmax, float ymin, float ymax)
        {
            this.XMin = xmin;
            this.XMax = xmax;
            this.YMin = ymin;
            this.YMax = ymax;
        }

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
                return this.YMax - this.YMin;
            }
        }

        /// <summary>
        /// The width in pixels.
        /// </summary>
        public float Width
        {
            get
            {
                return this.XMax - this.XMin;
            }
        }

        /// <summary>
        /// Renders the rectangle as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this rectangle.</returns>
        public override string ToString()
        {
            return "[Rect " + this.XMin + "," + this.YMin + "->" + this.XMax + "," + this.YMax + "]";
        }
    }
}
