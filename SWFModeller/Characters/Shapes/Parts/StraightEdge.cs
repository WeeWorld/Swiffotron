//-----------------------------------------------------------------------
// StraightEdge.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    class StraightEdge : IShapeRecord
    {
        public int DX { get; set; }
        public int DY { get; set; }

        public override string ToString()
        {
            return "[line to " + DX + "," + DY + "]";
        }
    }
}
