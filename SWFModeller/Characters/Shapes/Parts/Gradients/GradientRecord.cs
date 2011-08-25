//-----------------------------------------------------------------------
// GradientRecord.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients
{
    using System.Drawing;

    public class GradientRecord
    {
        public int Ratio { get; set; }

        public Color Colour { get; set; }

        public override string ToString()
        {
            return "[" + Colour + ", " + Ratio + "]";
        }
    }
}
