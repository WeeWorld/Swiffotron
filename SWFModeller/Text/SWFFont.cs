//-----------------------------------------------------------------------
// Font.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Shapes;

    /// <summary>
    /// Represents a font and all its glyphs. It implements ICharacter but doesn't seem
    /// very characterey. In the SWF though, font IDs share the same space as character IDs
    /// which makes a kinda sense being that they both live in the IDE library.
    /// </summary>
    public class SWFFont : ICharacter
    {
        private Dictionary<char, IShape> glyphs;
        private Dictionary<char, GlyphLayout> layouts;
        private Dictionary<char, PixelAlignment> pixelAlignment;

        public KerningPair[] KerningTable { get; set; }

        public int? Ascent { get; set; }
        public int? Descent { get; set; }
        public int? Leading { get; set; }

        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsSmall { get; set; }

        public string Name { get; set; }
        public string FullName { get; set; }
        public string Copyright { get; set; }

        public Language LanguageCode { get; set; }

        public bool HasExtraNameInfo
        {
            get
            {
                return FullName != null || Copyright != null;
            }
        }

        public int GlyphCount
        {
            get
            {
                return glyphs.Count;
            }
        }

        private char[] cachedCodePoints;

        private Dictionary<char, int> cachedIndexMap;

        /// <summary>
        /// All the code points sorted into ascending order.
        /// </summary>
        public char[] CodePoints
        {
            get
            {
                if (cachedCodePoints == null)
                {

                    cachedCodePoints = new char[glyphs.Count];
                    int codePos = 0;
                    using (IEnumerator<char> i = this.glyphs.Keys.GetEnumerator())
                    {
                        while (i.MoveNext())
                        {
                            cachedCodePoints[codePos++] = i.Current;
                        }
                    }

                    Array.Sort<char>(cachedCodePoints);
                }

                return cachedCodePoints;
            }
        }

        public Dictionary<char, int> IndexMap
        {
            get
            {
                if (cachedIndexMap == null)
                {
                    cachedIndexMap = new Dictionary<char, int>();
                    char[] codes = CodePoints;
                    for (int i = 0; i < codes.Length; i++)
                    {
                        cachedIndexMap.Add(codes[i], i);
                    }
                }

                return cachedIndexMap;
            }
        }

        public GlyphLayout GetLayout(char c)
        {
            return layouts[c];
        }


        /// <summary>
        /// Initializes an instance of the Font class with basic font information.
        /// </summary>
        /// <param name="language">The language of the font (Not encoding)</param>
        /// <param name="name">The font name</param>
        /// <param name="isBold">Is this a bold font?</param>
        /// <param name="isItalic">Is this an italic font?</param>
        /// <param name="numGlyphs">How many glyphs do you expect to have? Not important
        /// except for a little efficiency saving. It's fine to pass 0.</param>
        public SWFFont(Language language, string name, bool isBold, bool isItalic, bool isSmall, int numGlyphs)
        {
            glyphs = new Dictionary<char, IShape>(numGlyphs);
            layouts = new Dictionary<char, GlyphLayout>();
            pixelAlignment = new Dictionary<char, PixelAlignment>();

            this.LanguageCode = language;
            this.Name = name;
            this.IsBold = isBold;
            this.IsItalic = isItalic;
            this.IsSmall = isSmall;

            this.cachedCodePoints = null;
            this.cachedIndexMap = null;
        }

        public bool HasLayout
        {
            get
            {
                return layouts.Count > 0
                        || KerningTable != null
                        || Ascent != null
                        || Descent != null
                        || Leading != null;
            }
        }

        public bool HasPixelAlignment
        {
            get
            {
                return pixelAlignment.Count > 0;
            }
        }

        public Thickness ThicknessHint { get; set; }

        public enum Thickness
        {
            Thin = 0,
            Medium = 1,
            Thick = 2
        }

        public enum Language
        {
            Latin = 1,
            Japanese = 2,
            Korean = 3,
            SimplifiedChinese = 4,
            TraditionalChinese = 5
        }

        internal void AddGlyph(char codepoint, IShape shape)
        {
            this.cachedCodePoints = null;
            this.cachedIndexMap = null;
            this.glyphs.Add(codepoint, shape);
        }

        internal void AddLayout(char codepoint, GlyphLayout glyphLayout)
        {
            this.layouts.Add(codepoint, glyphLayout);
        }

        internal void ToStringModelView(int nest, StringBuilder sb)
        {
            string indent = new string(' ', nest * 4);

            sb.Append(indent + "Font: '" + this.Name + "'\n");
            sb.Append(indent + "{\n");

            string indentPlus = indent + "    ";

            sb.Append(indentPlus + "Bold: " + this.IsBold + ", Italic: " + this.IsItalic + ", Small: " + this.IsSmall + "\n");
            sb.Append(indentPlus + "Language: " + this.LanguageCode.ToString() + "\n");
            sb.Append(indentPlus + "Ascent: " + (this.Ascent == null ? "none" : this.Ascent.Value.ToString()) + "\n");
            sb.Append(indentPlus + "Descent: " + (this.Descent == null ? "none" : this.Descent.Value.ToString()) + "\n");
            sb.Append(indentPlus + "Leading: " + (this.Leading == null ? "none" : this.Leading.Value.ToString()) + "\n");

            char[] codes = this.CodePoints;
            sb.Append(indentPlus + codes.Length+" glyphs:\n");
            for (int i = 0; i < codes.Length; i++)
            {
                char c = codes[i];
                IShape shape = glyphs[c];
                sb.Append(indentPlus + "Glyph #" + (i + 1) + " '" + c + "' (U+" + String.Format("{0:X4}", (int)c) + "): " + shape.ToString() + "\n");
            }

            if (this.HasExtraNameInfo)
            {
                sb.Append('\n');
                sb.Append(indentPlus + "Font has extra name info...\n");
                sb.Append(indentPlus + "FullName: " + this.FullName + "\n");
                sb.Append(indentPlus + "Copyright: " + this.Copyright + "\n"); ;
            }

            if (this.HasLayout)
            {
                sb.Append('\n');
                sb.Append(indentPlus + "Font has layout info...\n");
                for (int i = 0; i < codes.Length; i++)
                {
                    char c = codes[i];
                    GlyphLayout layout = this.layouts[c];
                    sb.Append(indentPlus + "Layout #" + (i + 1) + " '" + c + "' (U+" + String.Format("{0:X4}", (int)c) + "): " + layout.ToString() + "\n");
                }
            }

            if (this.HasPixelAlignment)
            {
                sb.Append('\n');
                sb.Append(indentPlus + "Font has pixel alignment info...\n");
                sb.Append(indentPlus + "Thickness hint: " + this.ThicknessHint.ToString() + "\n");
                for (int i = 0; i < codes.Length; i++)
                {
                    char c = codes[i];
                    PixelAlignment alignment = this.pixelAlignment[c];
                    sb.Append(indentPlus + "PixelAlignment #" + (i + 1) + " '" + c + "' (U+" + String.Format("{0:X4}", (int)c) + "): " + alignment.ToString() + "\n");
                }
            }

            if (this.KerningTable.Length > 0)
            {
                sb.Append('\n');
                sb.Append(indentPlus + "Font has kerning table...\n");
                foreach (KerningPair kp in this.KerningTable)
                {
                    sb.Append(indentPlus + "Kern '"
                            + kp.LeftChar + "' (U+" + String.Format("{0:X4}", (int)kp.LeftChar) + ") + "
                            + kp.RightChar + "' (U+" + String.Format("{0:X4}", (int)kp.RightChar) + ") => "
                            + kp.Adjustment + "\n");
                }
            }

            sb.Append(indent + "}\n");
        }

        public override string ToString()
        {
            return this.Name;
        }

        internal IShape GetGlyphShape(char c)
        {
            return this.glyphs[c];
        }

        internal void AddPixelAlignment(char c, PixelAlignment alignment)
        {
            this.pixelAlignment.Add(c, alignment);
        }

        internal PixelAlignment GetPixelAligment(char c)
        {
            return this.pixelAlignment[c];
        }

        internal bool CanMergeWith(SWFFont font)
        {
            if (!font.HasExtraNameInfo || !this.HasExtraNameInfo)
            {
                /* ISSUE 77 */
                throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Fonts without full names cause us problems.");
            }

            return this.FullName == font.FullName
                    && this.IsBold == font.IsBold
                    && this.IsItalic == font.IsItalic
                    && this.IsSmall == font.IsSmall
                    && this.HasLayout == font.HasLayout
                    && this.HasPixelAlignment == font.HasPixelAlignment;
        }

        internal bool HasGlyph(char c)
        {
            return glyphs.ContainsKey(c);
        }
    }
}
