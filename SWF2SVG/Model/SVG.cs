//-----------------------------------------------------------------------
// SVG.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2SVG.Model
{
    using System.Text;
    using System.Drawing;
    using System.Collections.Generic;
    using SWFProcessing.SWF2SVG.IO;

    class SVG
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Color BackgroundColor { get; set; }

        List<ISVGElement> Elements;

        public SVG()
        {
            Elements = new List<ISVGElement>();
        }

        public byte[] Render()
        {
            StringBuilder buf = new StringBuilder(16 * 1024);

            buf.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");

            XmlAssist.OpenTag(buf, "svg", new string[][] {
                new string[] {"xmlns", "http://www.w3.org/2000/svg"},
                new string[] {"xmlns:xlink", "http://www.w3.org/1999/xlink"},
                new string[] {"width", Width.ToString()},
                new string[] {"height", Height.ToString()},
                new string[] {"style", "background-color:" + ColorTranslator.ToHtml(BackgroundColor)},
            });

            foreach (ISVGElement element in Elements)
            {
                element.Render(buf);
            }

            buf.AppendLine("</svg>");

            return UTF8Encoding.Default.GetBytes(buf.ToString());
        }
    }
}
