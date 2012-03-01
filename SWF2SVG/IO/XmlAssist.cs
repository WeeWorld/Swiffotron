//-----------------------------------------------------------------------
// XmlAssist.cs
//
//
//-----------------------------------------------------------------------


namespace SWFProcessing.SWF2SVG.IO
{
    using System.Text;

    internal class XmlAssist
    {
        public static void OpenTag(StringBuilder buf, string tag, string[][]attribs)
        {
            buf.Append('<')
                .Append(tag);

            if (attribs != null && attribs.Length > 0)
            {
                foreach (string[] attrib in attribs)
                {
                    if (attrib[1] != null)
                    {
                        buf.Append(' ')
                            .Append(attrib[0])
                            .Append("=\"")
                            .Append(attrib[1])
                            .Append('\"');
                    }
                }
            }

            buf.AppendLine(">");
        }
    }
}
