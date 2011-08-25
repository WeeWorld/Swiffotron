//-----------------------------------------------------------------------
// Matrix.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Geom
{
    using System; /* Gotta have a system. */

    /// <summary>
    /// A 2D matrix
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// Initializes a new instance of an identity matrix
        /// </summary>
        public Matrix()
        {
            this.TransX = 0.0f;
            this.TransY = 0.0f;
            this.ScaleX = 1.0f;
            this.ScaleY = 1.0f;
            this.SkewX = 0.0f;
            this.SkewY = 0.0f;
        }

        /// <summary>
        /// Initializes a new instance of a matrix
        /// </summary>
        /// <param name="tx">Translate X value</param>
        /// <param name="ty">Translate Y value</param>
        /// <param name="sx">Scale X value</param>
        /// <param name="sy">Scale Y value</param>
        /// <param name="rx">Rotate (Skew) X value</param>
        /// <param name="ry">Rotate (Skew) Y value</param>
        public Matrix(float tx, float ty, float sx, float sy, float rx, float ry)
        {
            this.TransX = tx;
            this.TransY = ty;
            this.ScaleX = sx;
            this.ScaleY = sy;
            this.SkewX = rx;
            this.SkewY = ry;
        }

        /// <summary>
        /// Gets or sets the transpose X value
        /// </summary>
        public float TransX { get; set; }

        /// <summary>
        /// Gets or sets the transpose Y value
        /// </summary>
        public float TransY { get; set; }

        /// <summary>
        /// Gets or sets the scale factor for X axis
        /// </summary>
        public float ScaleX { get; set; }

        /// <summary>
        /// Gets or sets the scale factor for Y axis
        /// </summary>
        public float ScaleY { get; set; }

        /// <summary>
        /// Gets or sets the sheer amount for X axis
        /// </summary>
        public float SkewX { get; set; }

        /// <summary>
        /// Gets or sets the sheer amount for Y axis
        /// </summary>
        public float SkewY { get; set; }

        /// <summary>
        /// True if the transform scales.
        /// </summary>
        public bool HasScale
        {
            get
            {
                return this.ScaleX != 1.0f || this.ScaleY != 1.0f;
            }
        }

        /// <summary>
        /// True if the transform rotates
        /// </summary>
        public bool HasSkew
        {
            get
            {
                return this.SkewX != 0.0f || this.SkewY != 0.0f;
            }
        }

        /// <summary>
        /// True if the transform is a simple translation with no rotate or scale
        /// </summary>
        public bool IsSimpleTranslate
        {
            get
            {
                return this.SkewX == 0.0f && this.SkewY == 0.0f && this.ScaleX == 1.0f && this.ScaleY == 1.0f;
            }
        }

        /// <summary>
        /// Renders the matrix as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this matrix.</returns>
        public override string ToString()
        {
            return "[t:" + this.TransX + "," + this.TransY + ", sc:" + this.ScaleX + "," + this.ScaleY + ", sk:" + this.SkewX + "," + this.SkewY + "]";
        }

        public Matrix Copy()
        {
            return new Matrix(this.TransX, this.TransY, this.ScaleX, this.ScaleY, this.SkewX, this.SkewY);
        }

        public void Translate(float tx, float ty)
        {
            this.TransX += tx;
            this.TransY += ty;
        }

        public void RotateToDegrees(double angle)
        {
            angle = Math.PI * angle / 180.0;

            float cosa = (float)Math.Cos(angle);
            float sina = (float)Math.Sin(angle);

            this.ScaleX = cosa;
            this.SkewX = sina;
            this.SkewY = -sina;
            this.ScaleY = cosa;
        }

        public void ScaleNoTranslate(float xscale, float yscale)
        {
            this.ScaleX *= xscale;
            this.SkewX *= yscale;
            this.SkewY *= xscale;
            this.ScaleY *= yscale;
        }

        public void Apply(Matrix m)
        {
            if (m.IsSimpleTranslate)
            {
                this.TransX += m.TransX;
                this.TransY += m.TransY;
                return;
            }
            else
            {
                float temp;

                temp = (this.ScaleX * m.ScaleX) + (this.SkewX * m.SkewY);
                this.SkewX = (this.ScaleX * m.SkewX) + (this.SkewX * m.ScaleY);
                this.ScaleX = temp;

                temp = (this.SkewY * m.ScaleX) + (this.ScaleY * m.SkewY);
                this.ScaleY = (this.SkewY * m.SkewX) + (this.ScaleY * m.ScaleY);
                this.SkewY = temp;

                temp = (this.TransX * m.ScaleX) + (this.TransY * m.SkewY) + m.TransX;
                this.TransY = (this.TransX * m.SkewX) + (this.TransY * m.ScaleY) + m.TransY;
                this.TransX = temp;
            }
        }
    }
}
