//-----------------------------------------------------------------------
// StraightEdge.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    internal class StraightEdge : IShapeRecord
    {
        public int DX { get; set; }

        public int DY { get; set; }

        public override string ToString()
        {
            return "[line to " + this.DX + "," + this.DY + "]";
        }
    }
}
