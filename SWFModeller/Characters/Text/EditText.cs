//-----------------------------------------------------------------------
// EditText.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Text
{
    using System.Drawing;
    using System.Text;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Text;

    public class EditText : ICharacter, IFontUser, IFontUserProcessor, IText
    {
        public enum Alignment
        {
            Left = 0,
            Right = 1,
            Center = 2,
            Justify = 3
        }

        public class Layout
        {
            public Alignment Align { get; set; }

            public int LeftMargin { get; set; }
            public int RightMargin { get; set; }
            public int Indent { get; set; }
            public int Leading { get; set; }
        }

        private SWF _root;

        public EditText(SWF root)
        {
            this._root = root;
        }

        public Layout LayoutInfo { get; set; }

        /* TODO: A text field has a font, but it can also contain HTML which can
         * reference other fonts. This is a pain. Deal with it. */

        public Rect Bounds { get; set; }

        public bool WordWrapEnabled { get; set; }

        public bool IsMultiline { get; set; }

        public bool IsPassword { get; set; }

        public bool IsReadOnly { get; set; }

        public bool IsAutoSized { get; set; }

        public bool IsNonSelectable { get; set; }

        public bool HasBorder { get; set; }

        public bool IsStatic { get; set; }

        public bool IsHTML { get; set; }

        public bool UseOutlines { get; set; }

        public bool HasFont
        {
            get
            {
                return Font != null;
            }
        }

        public SWFFont Font { get; set; }

        public bool HasText
        {
            get
            {
                return this.Text != null && this.Text != string.Empty;
            }
        }

        public string Text { get; set; }

        public int? MaxLength { get; set; }

        public int FontHeight { get; set; }

        public bool HasTextColor
        {
            get
            {
                return this.Color != null;
            }
        }

        public Color Color { get; set; }

        public bool HasMaxLength
        {
            get
            {
                return MaxLength != null;
            }
        }

        public bool HasFontClass
        {
            get
            {
                /* TODO: Support font classes. */
                return false;
            }
        }

        public bool HasLayout
        {
            get
            {
                return this.LayoutInfo != null;
            }
        }

        public string VarName { get; set; }

        internal void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner)
        {
            oneLiner = false;
            string indent = new string(' ', nest * 4);

            sb.Append(indent + "EditText: '" + (this.HasText ? this.Text : string.Empty) + "'\n");

            if (this.HasFont)
            {
                sb.Append(indent + "Font: '" + this.Font.Name + "'\n");
                sb.Append(indent + "FontHeight: '" + this.FontHeight+ "'\n");
            }
            else
            {
                sb.Append(indent + "No specified font.\n");
            }
        }

        public override string ToString()
        {
            return "[EditText '" + (this.HasText ? this.Text : string.Empty) + "']";
        }

        public void FontUserProc(FontUserProcessor fup)
        {
            fup(this); /* I just wanted to say that I like this line. */
        }
    }
}
