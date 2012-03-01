//-----------------------------------------------------------------------
// Shape.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes
{
    using System.Text;
    using SWFProcessing.ModellingUtils.Geom;
    using SWFProcessing.ModellingUtils.Util;
    using SWFProcessing.SWFModeller.Characters.Images;
    using SWFProcessing.SWFModeller.Characters.Shapes.Parts;

    public class Shape : IShape
    {
        public byte[] OriginalBytes { get; set; }

        public Tag OriginalFormat { get; set; }

        public bool UsesFillWinding { get; set; }

        public bool UsesNonScalingStrokes { get; set; }

        public bool UsesScalingStrokes { get; set; }

        public Rect Bounds { get; set; }

        public ShapeDef ShapeDef { get; set; }

        /// <param name="shapeData">The shape data.</param>
        /// <param name="format">The data format</param>
        public void SetOriginalBytes(byte[] shapeData, Tag format)
        {
            this.OriginalBytes = shapeData;
            this.OriginalFormat = format;
        }

        public IImage[] GetImages()
        {
            ListSet<IImage> images = new ListSet<IImage>();

            if (this.ShapeDef == null)
            {
                return new IImage[0];
            }

            foreach (IFillStyle fs in this.ShapeDef.FillStyles)
            {
                if (fs.Bitmap != null)
                {
                    images.AddIfNotAlredy(fs.Bitmap);
                }
            }

            foreach (IShapeRecord sr in this.ShapeDef.Records)
            {
                if (sr is StyleChange)
                {
                    StyleChange sc = (StyleChange)sr;
                    IFillStyle fs = sc.FillStyle0;
                    if (fs != null && fs.Bitmap != null)
                    {
                        images.AddIfNotAlredy(fs.Bitmap);
                    }

                    fs = sc.FillStyle1;
                    if (fs != null && fs.Bitmap != null)
                    {
                        images.AddIfNotAlredy(fs.Bitmap);
                    }
                }
            }

            return images.ToArray();
        }

#if DEBUG
        public void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner)
        {
            oneLiner = false;

            string indent = new string(' ', nest * 4);

            sb.Append(indent + "Bounds: " + this.Bounds + "\n");
            sb.Append(indent + "Original format: " + this.OriginalFormat + "\n");
            if (ShapeDef != null)
            {
                sb.Append(indent + "Shape; WithStyle? "+this.ShapeDef.IsShapeWithStyle+"\n");
                sb.Append(indent + "{\n");

                ShapeDef.ToStringModelView(nest + 1, sb);

                sb.Append(indent + "}\n");
            }
        }
#endif

        public override string ToString()
        {
            return "[Shape " + OriginalFormat.ToString() + ":" + OriginalBytes.Length + " bytes]";
        }
    }
}
