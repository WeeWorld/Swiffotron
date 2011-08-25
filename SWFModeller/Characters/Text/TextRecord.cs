//-----------------------------------------------------------------------
// GlyphEntry.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Text
{
    using SWFProcessing.SWFModeller.Text;
    using System.Drawing;
    using System.Text;
    using System.Linq;

    public class TextRecord : IFontUser
    {
        public string Text { get; set; }
        public int[] Advances { get; set; }

        public SWFFont Font { get; set; }

        public int XOffset { get; set; }

        public int YOffset { get; set; }

        public int FontHeight { get; set; }

        public bool HasFont
        {
            get
            {
                return Font != null;
            }
        }

        public bool HasColour
        {
            get
            {
                return this.Colour != null;
            }
        }

        public bool HasAlpha
        {
            get
            {
                return (this.Colour != null && this.Colour.A != 0);
            }
        }

        public Color Colour { get; set; }

        public override string ToString()
        {
            return Text + ", [" + string.Join(",", Advances.Select(p => p.ToString()).ToArray()) + "]";
        }

        internal void ToStringModelView(int nest, StringBuilder sb)
        {
            string indent = new string(' ', nest * 4);

            sb.Append(indent + "Text: '" + this.ToString() + "'\n");
            if (this.HasFont)
            {
                sb.Append(indent + "Font: '" + this.Font.Name + "'\n");
                sb.Append(indent + "FontHeight: '" + this.FontHeight + "'\n");
            }
            else
            {
                sb.Append(indent + "No specified font.\n");
            }

            if (this.HasColour)
            {
                sb.Append(indent + "Colour: '" + this.Colour + "'\n");
            }

            sb.Append(indent + "Offsets: " + XOffset + ", " + YOffset + "\n");
        }

        public bool HasYOffset
        {
            get
            {
                return YOffset != 0;
            }
        }

        public bool HasXOffset
        {
            get
            {
                return XOffset != 0;
            }
        }
    }
}
