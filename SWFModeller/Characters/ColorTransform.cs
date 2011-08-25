//-----------------------------------------------------------------------
// ColorTransform.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters
{
    /// <summary>
    /// A color transform which can add and/or multiply with a pixel
    /// color.
    /// </summary>
    public class ColorTransform
    {
        /// <summary>
        /// Initializes a new instance of the ColorTransform class.
        /// </summary>
        /// <param name="add">A colour to be added. May be null</param>
        /// <param name="mult">A colour to be multiplied. May be null</param>
        public ColorTransform(HDRColor add, HDRColor mult)
        {
            this.Add = add;
            this.Mult = mult;
        }

        public HDRColor Add { get; set; }

        public HDRColor Mult { get; set; }

        public bool HasAdd
        {
            get
            {
                return this.Add != null;
            }
        }

        public bool HasMult
        {
            get
            {
                return this.Mult != null;
            }
        }

        public bool HasAlpha
        {
            get
            {
                return (this.Mult != null && this.Mult.HasAlpha) || (this.Add != null && this.Add.HasAlpha);
            }
        }

        /// <summary>
        /// Renders the colour transform as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this colour transform.</returns>
        public override string ToString()
        {
            return "[add:" + this.Add + ", mult:" + this.Mult + "]";
        }

        internal ColorTransform Clone()
        {
            return new ColorTransform(this.Add == null ? null : this.Add.Clone(), this.Mult == null ? null : this.Mult.Clone());
        }
    }
}
