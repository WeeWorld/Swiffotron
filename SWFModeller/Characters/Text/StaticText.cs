//-----------------------------------------------------------------------
// StaticText.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Text
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using SWFProcessing.SWFModeller.Characters.Geom;

    public class StaticText : ICharacter, IFontUserProcessor, IText
    {
        private SWF _root;

        public StaticText(SWF root)
        {
            this._root = root;
            Records = new List<TextRecord>();
        }

        public string Text
        {
            get
            {
                return string.Join("", Records.Select(p => p.ToString()).ToArray());
            }

            set
            {
                /* TODO */
                throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Can't set text across multiple records yet.");
            }
        }

        public Rect Bounds { get; set; }

        public Matrix Position { get; set; }

        public List<TextRecord> Records { get; private set; }

        internal void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner)
        {
            oneLiner = false;
            string indent = new string(' ', nest * 4);

            sb.Append(indent + "StaticText: Bounds:" + Bounds + ", Position:" + Position + "\n");
            sb.Append(indent + "{\n");

            for (int i = 0; i < Records.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }
                TextRecord tr = Records[i];
                tr.ToStringModelView(nest + 1, sb);
            }

            sb.Append(indent + "}\n");
        }

        public override string ToString()
        {
            return string.Join(" + ", Records.Select(p => p.ToString()).ToArray());
        }

        public void FontUserProc(FontUserProcessor fup)
        {
            foreach (IFontUser fu in Records)
            {
                fup(fu); /* Pesky speech impediment. */
            }
        }

        public bool HasAlpha
        {
            get
            {
                foreach (TextRecord tr in Records)
                {
                    if (tr.HasAlpha)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
