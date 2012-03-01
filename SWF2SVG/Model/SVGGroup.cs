//-----------------------------------------------------------------------
// SVGGroup.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2SVG.Model
{
    using System.Text;
    using SWFProcessing.SWF2SVG.IO;
    using SWFProcessing.ModellingUtils.Geom;

    class SVGGroup : ISVGElement
    {
        public Matrix Position { get; set; }
        public string ID { get; set; }

        public void Render(StringBuilder buf)
        {
            XmlAssist.OpenTag(buf, "g", new string[][]{
                new string[] {"position", Position==null?"0":Position.ToString()},
                new string[] {"id", ID}
            });

            buf.AppendLine("</g>");
        }
    }
}
