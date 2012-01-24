//-----------------------------------------------------------------------
// StyleChange.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System.Text;
    using SWFProcessing.SWFModeller.Characters.Images;

    internal class StyleChange : IShapeRecord
    {
        public int? DX { get; set; }

        public int? DY { get; set; }

        public IFillStyle FillStyle0 { get; set; }

        public IFillStyle FillStyle1 { get; set; }

        /* ISSUE 23: Not sure how flash uses the new styles, or how they're indexed. Not sure
         * we'll be able to find images here when parsing shapes, or tie them back together 
         * again. All this points to a massive refactoring of fill/line style recording and
         * management somewhere along the line. A hackity fix may be to store everything in the
         * main style arrays and to hell with style change records. Feels too easy though. Something
         * is amiss.
         */
        public IFillStyle[] NewFillStyles { get; set; }

        public ILineStyle[] NewLineStyles { get; set; }

        public int? LineStyle { get; set; }

        public IImage[] GetImages()
        {
            /* ISSUE 23: Collate all the images from all our fill styles. */
            return null;
        }

        public override string ToString()
        {
            string fills = null;
            string lines = null;

            if (NewFillStyles != null && NewFillStyles.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(" {new fills ");
                foreach (FillStyle fs in this.NewFillStyles)
                {
                    sb.Append(fs.ToString());
                }
                sb.Append("}");

                fills = sb.ToString();
            }

            if (this.NewLineStyles != null && this.NewLineStyles.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(" {new lines ");
                foreach (LineStyle ls in this.NewLineStyles)
                {
                    sb.Append(ls.ToString());
                }
                sb.Append("}");

                lines = sb.ToString();
            }

            return "[style change move=" + this.DX + "," + this.DY + " fill0/1=" + this.FillStyle0 + "," + this.FillStyle1 + " line=" + this.LineStyle + (lines == null ? string.Empty : lines) + (fills == null ? string.Empty : fills) + "]";
        }
    }
}
