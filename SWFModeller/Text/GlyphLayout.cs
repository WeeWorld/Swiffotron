//-----------------------------------------------------------------------
// GlyphLayout.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Text
{
    using SWFProcessing.ModellingUtils.Geom;

    public class GlyphLayout
    {
        public int Advance { get; set; }

        public Rect Bounds { get; set; }

        public override string ToString()
        {
            return "[Adv: " + this.Advance + ", Bounds: " + this.Bounds + "]";
        }
    }
}
