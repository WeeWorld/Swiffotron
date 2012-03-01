//-----------------------------------------------------------------------
// SWF2HTML.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML
{
    using SWFProcessing.SWFModeller;
    using System.IO;
    using System.Text;

    public class SWF2HTML
    {
        public SWF2HTML(SWF swf)
        {
            this.Swf = swf;
        }

        public SWF Swf { get; set; }

        public Stream GetHTML()
        {
            return new MemoryStream(GetHTMLAsBytes());
        }

        public byte[] GetHTMLAsBytes()
        {
            return UTF8Encoding.Default.GetBytes("<!doctype html>\nHello, world.");
        }
    }
}
