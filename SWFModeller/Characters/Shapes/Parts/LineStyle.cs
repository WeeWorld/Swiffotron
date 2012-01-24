//-----------------------------------------------------------------------
// LineStyle.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System.Drawing;

    public class LineStyle : ILineStyle
    {
        public int Width { get; set; }

        public CapStyle StartCap { get; set; }

        public CapStyle EndCap { get; set; }

        public JoinStyle Join { get; set; }

        public bool HasFill { get; set; }

        public bool NoHScaling { get; set; }

        public bool NoVScaling { get; set; }

        public bool HasPixelHints { get; set; }

        public int? MiterLimit { get; set; }

        public Color? Colour { get; set; }

        public FillStyle FillStyle { get; set; }

        public override string ToString()
        {
            return "[line cap " + this.StartCap.ToString()
                    + "-" + this.EndCap.ToString() + ", "
                    + this.Join.ToString()
                    + " join, width=" + this.Width + ", "
                    + " miterlim=" + this.MiterLimit + ", "
                    + " NoHScale=" + this.NoHScaling + ", "
                    + " NoVScale=" + this.NoVScaling + ", "
                    + " HasPixelHints=" + this.HasPixelHints + ", "
                    + " colour=" + this.Colour + "]";
        }
    }
}
