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
            /* ISSUE 72 */
            throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Can't dump focal gradient");
        }
    }
}
