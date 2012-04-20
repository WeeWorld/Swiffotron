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

    public class SWF2HTML
    {
        public string ID { get; set; }
        
        private SWF2HTMLOptions Options;

        public SWF2HTML(SWF swf, string ID, SWF2HTMLOptions options = null)
        {
            this.Swf = swf;
            this.ID = ID;

            if (options == null)
            {
                options = new SWF2HTMLOptions(); /* Defaults */
            }

            this.Options = options;
        }

        public SWF Swf { get; set; }

        public Stream GetHTML(bool standalone)
        {
            return new MemoryStream(GetHTMLAsBytes(standalone));
        }

        public byte[] GetHTMLAsBytes(bool standalone)
        {
            switch (this.Options.Framework)
            {
                case SWF2HTMLOptions.FrameworkType.JQuery:
                    JQueryCanvasApp canvasApp = new JQueryCanvasApp(this.ID, this.Swf, this.Options);
                    return canvasApp.Render(standalone);

                case SWF2HTMLOptions.FrameworkType.RawJS:
                default:
                    throw new SWF2HTMLException(
                            SWF2HTMLError.UnimplementedFeature,
                            "Only jQuery-based output is currently supported.");
            }


        }
    }
}
