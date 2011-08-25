//-----------------------------------------------------------------------
// MorphGradientRecord.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients
{
    using System.Drawing;

    public class MorphGradientRecord
    {
        public int StartRatio { get; set; }

        public Color StartColour { get; set; }

        public int EndRatio { get; set; }

        public Color EndColour { get; set; }
    }
}
