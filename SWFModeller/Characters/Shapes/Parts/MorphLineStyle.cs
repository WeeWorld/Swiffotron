//-----------------------------------------------------------------------
// MorphLineStyle.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System.Drawing;

    public class MorphLineStyle : ILineStyle
    {
        public int StartWidth { get; set; }

        public int EndWidth { get; set; }

        public Color? StartColour { get; set; }

        public Color? EndColour { get; set; }

        public CapStyle StartCap { get; set; }

        public CapStyle EndCap { get; set; }

        public JoinStyle Join { get; set; }

        public bool HasFill { get; set; }

        public bool NoHScaling { get; set; }

        public bool NoVScaling { get; set; }

        public bool HasPixelHints { get; set; }

        public int? MiterLimit { get; set; }

        public MorphFillStyle FillStyle { get; set; }

        public override string ToString()
        {
            return "[line cap " + this.StartCap.ToString()
                    + "-" + this.EndCap.ToString() + ", "
                    + this.Join.ToString()
                    + " join, startwidth=" + this.StartWidth + ", "
                    + " endwidth=" + this.EndWidth + ", "
                    + " miterlim=" + this.MiterLimit + ", "
                    + " NoHScale=" + this.NoHScaling + ", "
                    + " NoVScale=" + this.NoVScaling + ", "
                    + " HasPixelHints=" + this.HasPixelHints + ", "
                    + " startcolour=" + this.StartColour + ", "
                    + " endcolour=" + this.EndColour + "]";
        }
    }
}
