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

    public class SWF2SVG
    {
        public SWF2SVG(SWF swf)
        {
            this.Swf = swf;
        }

        public SWF Swf { get; set; }

        public Stream GetSVG()
        {
            return new MemoryStream(ASCIIEncoding.Default.GetBytes("<svg />"));
        }
    }
}
