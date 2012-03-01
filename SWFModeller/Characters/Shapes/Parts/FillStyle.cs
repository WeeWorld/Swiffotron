//-----------------------------------------------------------------------
// FillStyle.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System.Drawing;
    using SWFProcessing.ModellingUtils.Geom;
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
            switch (this.Type)
            {
                case FillType.Solid:
                    return "[" + this.Type.ToString() + ": Colour=" + this.Colour.ToString() + "]";

                case FillType.LinearGradient:
                case FillType.RadialGradient:
                case FillType.FocalGradient:
                    return "[" + this.Type.ToString() + ": Gradient=" + this.Gradient.ToString() + ", Pos=" + this.FillMatrix + "]";

                case FillType.RepeatingBitmap:
                case FillType.ClippedBitmap:
                case FillType.NonSmoothRepeatingBitmap:
                case FillType.NonSmoothClippedBitmap:
                    if (this.Bitmap == null)
                    {
                        return "[" + this.Type.ToString() + ": Bitmap=(none), Pos=" + this.FillMatrix + "]";
                    }
                    else
                    {
                        return "[" + this.Type.ToString() + ": Bitmap=" + this.Bitmap.ToString() + ", Pos=" + this.FillMatrix + "]";
                    }

                default:
                    /* ISSUE 73 */
                    throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Bad fill type in shape dump.");
            }
        }
    }
}
