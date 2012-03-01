//-----------------------------------------------------------------------
// SWF2SVG.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2SVG
{
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWF2SVG.Model;

    public class SWF2SVG
    {
        public SWF2SVG(SWF swf)
        {
            this.Swf = swf;
        }

        public SWF Swf { get; set; }

        public Stream GetSVG()
        {
            return new MemoryStream(GetSVGAsBytes());
        }

        public byte[] GetSVGAsBytes()
        {
            SVG svg = new SVG()
            {
                Width = (int)Swf.FrameWidth,
                Height = (int)Swf.FrameHeight,
                BackgroundColor = Swf.BackgroundColor
            };

            return svg.Render();
        }
    }
}
