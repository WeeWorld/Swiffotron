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

    public class MorphShape : IShape
    {
        /// <param name="shapeData">The shape data.</param>
        /// <param name="format">The data format</param>
        public void SetOriginalBytes(byte[] shapeData, Tag format)
        {
            this.OriginalBytes = shapeData;
            this.OriginalFormat = format;
        }

        public byte[] OriginalBytes { get; set; }

        public Tag OriginalFormat { get; set; }

        public Rect Bounds { get; set; }

        /// <summary>
        /// Contains at least 1 scaling stroke
        /// </summary>
        public bool UsesScalingStrokes { get; set; }

        /// <summary>
        /// Contains at least 1 non-scaling stroke
        /// </summary>
        public bool UsesNonScalingStrokes { get; set; }

        public MorphFillStyle[] MorphFillStyles { get; set; }

        public MorphLineStyle[] MorphLineStyles { get; set; }

        public ShapeDef StartShape { get; set; }

        public ShapeDef EndShape { get; set; }

        public Rect EndBounds { get; set; }

        public Rect StartEdgeBounds { get; set; }

        public Rect EndEdgeBounds { get; set; }

        public IImage[] GetImages()
        {
            ListSet<IImage> images = new ListSet<IImage>();

            if (this.StartShape == null || this.EndShape == null)
            {
                return new IImage[0];
            }

            foreach (ShapeDef sd in new ShapeDef[] { this.StartShape, this.EndShape })
            {
                foreach (IFillStyle fs in sd.FillStyles)
                {
                    if (fs.Bitmap != null)
                    {
                        images.AddIfNotAlredy(fs.Bitmap);
                    }
                }

                foreach (IShapeRecord sr in sd.Records)
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
            }

            return images.ToArray();
        }

#if DEBUG
        public void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner)
        {
            oneLiner = false;

            string indent = new string(' ', nest * 4);

            sb.Append(indent + "StartBounds: " + this.Bounds + "\n");
            sb.Append(indent + "EndBounds: " + this.EndBounds + "\n");
            sb.Append(indent + "StartEdgeBounds: " + this.StartEdgeBounds + "\n");
            sb.Append(indent + "EndEdgeBounds: " + this.EndEdgeBounds + "\n");

            sb.Append(indent + "Original format: " + this.OriginalFormat + "\n");

            if (StartShape != null)
            {
                sb.Append(indent + "StartShape; WithStyle? " + this.StartShape.IsShapeWithStyle + "\n");
                sb.Append(indent + "{\n");

                this.StartShape.ToStringModelView(nest + 1, sb);

                sb.Append(indent + "}\n");
            }

            if (this.EndShape != null)
            {
                sb.Append(indent + "EndShape; WithStyle? " + this.EndShape.IsShapeWithStyle + "\n");
                sb.Append(indent + "{\n");

                this.EndShape.ToStringModelView(nest + 1, sb);

                sb.Append(indent + "}\n");
            }
        }
#endif

        public override string ToString()
        {
            return "[MorphShape " + this.OriginalFormat.ToString() + ":" + this.OriginalBytes.Length + " bytes]";
        }
    }
}
