//-----------------------------------------------------------------------
// MorphFillStyle.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System.Drawing;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Images;
    using SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients;

    public class MorphFillStyle : IFillStyle
    {
        public FillType Type { get; set; }

        public Color StartColour { get; set; }

        public Color EndColour { get; set; }

        /// <summary>
        /// If this is a gradient fill, then this is the gradient matrix. If this is
        /// a bitmap fill, then it's the bitmap matrix.
        /// </summary>
        public Matrix StartFillMatrix { get; set; }

        /// <summary>
        /// If this is a gradient fill, then this is the gradient matrix. If this is
        /// a bitmap fill, then it's the bitmap matrix.
        /// </summary>
        public Matrix EndFillMatrix { get; set; }

        public MorphGradient Gradient { get; set; }

        public IImage Bitmap { get; set; }

        public override string ToString()
        {
            switch (Type)
            {
                case FillType.Solid:
                    return "[" + Type.ToString() + ": StartColour=" + this.StartColour.ToString() + ", EndColour=" + this.EndColour.ToString() + "]";

                case FillType.LinearGradient:
                case FillType.RadialGradient:
                case FillType.FocalGradient:
                    return "[" + Type.ToString() + ": Gradient=" + this.Gradient.ToString() + ", StartPos=" + StartFillMatrix + ", EndPos=" + EndFillMatrix + "]";

                case FillType.RepeatingBitmap:
                case FillType.ClippedBitmap:
                case FillType.NonSmoothRepeatingBitmap:
                case FillType.NonSmoothClippedBitmap:
                    if (this.Bitmap == null)
                    {
                        return "[" + Type.ToString() + ": Bitmap=(none), StartPos=" + StartFillMatrix + ", EndPos=" + EndFillMatrix + "]";
                    }
                    else
                    {
                        return "[" + Type.ToString() + ": Bitmap=" + this.Bitmap.ToString() + ", StartPos=" + StartFillMatrix + ", EndPos=" + EndFillMatrix + "]";
                    }

                default:
                    throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Bad fill type in shape dump.");
            }
        }

    }
}
