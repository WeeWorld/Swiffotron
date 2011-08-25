//-----------------------------------------------------------------------
// FocalGradient.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients
{
    public class FocalGradient : IGradient
    {
        public override string ToString()
        {
            throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Can't dump focal gradient");
        }
    }
}
