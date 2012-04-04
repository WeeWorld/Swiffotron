//-----------------------------------------------------------------------
// SWF2HTML.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML
{
    using System.IO;
    using SWFProcessing.SWF2HTML.Model;
    using SWFProcessing.SWFModeller;

    public enum FrameworkType
    {
        JQuery,
        RawJS
    }

    public class SWF2HTML
    {
        public string ID { get; set; }
        
        private FrameworkType Framework;

        public SWF2HTML(SWF swf, string ID, FrameworkType framework)
        {
            this.Swf = swf;
            this.ID = ID;
            this.Framework = framework;
        }

        public SWF Swf { get; set; }

        public Stream GetHTML(bool standalone)
        {
            return new MemoryStream(GetHTMLAsBytes(standalone));
        }

        public byte[] GetHTMLAsBytes(bool standalone)
        {
            switch (this.Framework)
            {
                case FrameworkType.JQuery:
                    JQueryCanvasApp canvasApp = new JQueryCanvasApp(this.ID, this.Swf);
                    return canvasApp.Render(standalone);

                case FrameworkType.RawJS:
                default:
                    throw new SWF2HTMLException(
                            SWF2HTMLError.UnimplementedFeature,
                            "Only jQuery-based output is currently supported.");
            }


        }
    }
}
