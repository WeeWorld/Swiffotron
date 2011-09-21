//-----------------------------------------------------------------------
// FillStyle.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System.Drawing;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Images;
    using SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients;

    public class FillStyle : IFillStyle
    {
        public FillType Type { get; set; }

        public Color Colour { get; set; }

        /// <summary>
        /// If this is a gradient fill, then this is the gradient matrix. If this is
        /// a bitmap fill, then it's the bitmap matrix.
        /// </summary>
        public Matrix FillMatrix { get; set; }

        public IImage Bitmap { get; set; }

        public IGradient Gradient { get; set; }

        public override string ToString()
        {
            switch (Type)
            {
                case FillType.Solid:
                    return "[" + Type.ToString() + ": Colour=" + this.Colour.ToString() + "]";

                case FillType.LinearGradient:
                case FillType.RadialGradient:
                case FillType.FocalGradient:
                    return "[" + Type.ToString() + ": Gradient=" + this.Gradient.ToString() + ", Pos=" + FillMatrix + "]";

                case FillType.RepeatingBitmap:
                case FillType.ClippedBitmap:
                case FillType.NonSmoothRepeatingBitmap:
                case FillType.NonSmoothClippedBitmap:
                    if (this.Bitmap == null)
                    {
                        return "[" + Type.ToString() + ": Bitmap=(none), Pos=" + FillMatrix + "]";
                    }
                    else
                    {
                        return "[" + Type.ToString() + ": Bitmap=" + this.Bitmap.ToString() + ", Pos=" + FillMatrix + "]";
                    }

                default:
                    /* ISSUE 73 */
                    throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Bad fill type in shape dump.");
            }
        }
    }
}
