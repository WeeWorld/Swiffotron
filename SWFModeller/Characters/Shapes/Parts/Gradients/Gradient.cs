//-----------------------------------------------------------------------
// Gradient.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients
{
    using System.Linq;

    public class Gradient : IGradient
    {
        public GradientSpread Spread { get; set; }

        public GradientInterpolation Interpolation { get; set; }

        public GradientRecord[] Records { get; set; }

        public override string ToString()
        {
            return "[Gradient Interp:" + Interpolation + " Spread:" + Spread + " Records:{" + string.Join(", ", Records.Select(p => p.ToString()).ToArray()) +"}]";
        }
    }
}
