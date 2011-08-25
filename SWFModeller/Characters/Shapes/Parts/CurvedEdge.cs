//-----------------------------------------------------------------------
// CurvedEdge.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    class CurvedEdge : IShapeRecord
    {
        /* TODO: Since we never actually use this info, perhaps we'd be better storing
         * a block of bytes. In similar shape structures too. Speed and all that.. */

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
