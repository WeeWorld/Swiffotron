//-----------------------------------------------------------------------
// CurvedEdge.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    class CurvedEdge : IShapeRecord
    {
        public int CtrlDX { get; set; }
        public int CtrlDY { get; set; }
        public int AnchorDX { get; set; }
        public int AnchorDY { get; set; }

        public override string ToString()
        {
            return "[curve to " + AnchorDX + "," + AnchorDY + " via " + CtrlDX + "," + CtrlDY + "]";
        }
    }
}
