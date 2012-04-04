//-----------------------------------------------------------------------
// HTMLAssist.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML.IO
{
    using System.Text;
    using System.Collections.Generic;

    class HTMLAssist
    {
        public string BaseID { get; private set; }

        private Dictionary<string, int> PriorIDS;

        public HTMLAssist(string baseID)
        {
            this.BaseID = baseID + '_';

            this.BaseID = this.BaseID.Replace('.', '_');
            this.BaseID = this.BaseID.Replace(',', '_');
            this.BaseID = this.BaseID.Replace(' ', '_');
            this.BaseID = this.BaseID.Replace('-', '_');

            PriorIDS = new Dictionary<string, int>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="tag"></param>
        /// <param name="attribs">Pass a {"id",null} in the attribs to
        /// prevent the auto-generated tag ID.</param>
        public string OpenTag(StringBuilder buff, string tag, string[][] attribs = null)
        {
            buff.Append('<')
                .Append(tag);

            bool idSet = false;

            string id = null;

            if (attribs != null && attribs.Length > 0)
            {
                foreach (string[] attrib in attribs)
                {
                    if (attrib[0] == "id")
                    {
                        idSet = true;
                        id = attrib[1];
                    }

                    if (attrib[1] != null)
                    {
                        buff.Append(' ')
                            .Append(attrib[0])
                            .Append("=\"")
                            .Append(attrib[1])
                            .Append('\"');
                    }
                }
            }

            if (!idSet)
            {
                id = GenerateID(tag);
                buff.Append(" id=\"")
                    .Append(id)
                    .Append('\"');
            }

            buff.AppendLine(">");

            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="tag"></param>
        /// <param name="attribs">Pass a {"id",null} in the attribs to
        /// prevent the auto-generated tag ID.</param>
        public string JQueryAppendNew(StringBuilder buff, string nest, string parent, string tag, string[][] attribs = null)
        {
            buff.Append(nest)
                .Append(parent)
                .Append(".append(jQuery('<")
                .Append(tag)
                .Append(">')");

            bool idSet = false;

            string id = null;

            if (attribs != null && attribs.Length > 0)
            {
                foreach (string[] attrib in attribs)
                {
                    if (attrib[0] == "id")
                    {
                        idSet = true;
                        id = attrib[1];
                    }

                    if (attrib[1] != null)
                    {
                        buff.Append(".attr('")
                            .Append(attrib[0])
                            .Append("','")
                            .Append(attrib[1])
                            .Append("')");
                    }
                }
            }

            if (!idSet)
            {
                id = GenerateID(tag);
                buff.Append(".attr('id','")
                    .Append(id)
                    .Append("')");
            }

            buff.AppendLine(");");

            return id;
        }

        private string GenerateID(string tag)
        {
            int val;

            if (PriorIDS.ContainsKey(tag))
            {
                val = PriorIDS[tag] + 1;
            }
            else
            {
                val = 1;
            }

            PriorIDS[tag] = val;

            return BaseID + tag + val;
        }
    }
}
