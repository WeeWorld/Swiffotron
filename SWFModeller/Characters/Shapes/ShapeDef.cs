//-----------------------------------------------------------------------
// ShapeDef.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes
{
    using System.Collections.Generic;
    using SWFProcessing.SWFModeller.Characters.Shapes.Parts;
    using System.Text;

    /// <summary>
    /// Core shape definition information shared by all kinds of shape objects, e.g.
    /// normal ones, glyphs and morph shapes.
    /// </summary>
    public class ShapeDef
    {
        public List<IFillStyle> FillStyles { get; private set; }

        public List<ILineStyle> LineStyles { get; private set; }

        public IShapeRecord[] Records { get; set; }

        public bool IsShapeWithStyle
        {
            get
            {
                return FillStyles.Count > 0 || LineStyles.Count > 0;
            }
        }

        public ShapeDef()
        {
            this.FillStyles = new List<IFillStyle>();
            this.LineStyles = new List<ILineStyle>();
        }

        internal void ToStringModelView(int nest, StringBuilder sb)
        {
            string indent = new string(' ', nest * 4);

            for (int i = 0; i < FillStyles.Count; i++)
            {
                sb.Append(indent + "Fill#" + (i + 1) + ": " + FillStyles[i].ToString() + "\n");
            }

            for (int i = 0; i < LineStyles.Count; i++)
            {
                sb.Append(indent + "Line#" + (i + 1) + ": " + LineStyles[i].ToString() + "\n");
            }

            StringBuilder recordsDump = new StringBuilder();
            for (int i = 0; i < Records.Length; i++)
            {
                if (recordsDump.Length > 0)
                {
                    recordsDump.Append(' ');
                }

                if (Records[i] is CurvedEdge)
                {
                    CurvedEdge ce = (CurvedEdge)Records[i];
                    recordsDump.Append("C" + ce.CtrlDX + "," + ce.CtrlDY + ":" + ce.AnchorDX + "," + ce.AnchorDY);
                }
                else if (Records[i] is StraightEdge)
                {
                    StraightEdge se = (StraightEdge)Records[i];
                    recordsDump.Append("L" + se.DX + "," + se.DY);
                }
                else if (Records[i] is StyleChange)
                {
                    if (recordsDump.Length > 0)
                    {
                        sb.Append(indent + recordsDump.ToString() + "\n");
                        recordsDump = new StringBuilder();
                    }
                    sb.Append(indent + Records[i].ToString() + "\n");
                }
                else
                {
                    throw new SWFModellerException(SWFModellerError.Internal, "Can't dump " + Records[i]);
                }

                if (recordsDump.Length > 100)
                {
                    sb.Append(indent + recordsDump.ToString() + "\n");
                    recordsDump = new StringBuilder();
                }
            }

            if (recordsDump.Length > 0)
            {
                sb.Append(indent + recordsDump.ToString() + "\n");
            }
        }


        /// <param name="idx">1-based fill index.</param>
        /// <returns>Will return a fill. If the fill isn't there, you get an exception. If you
        /// get null, then that means 'no fill'.</returns>
        public IFillStyle FillFromIndex(int idx)
        {
            if (idx == 0)
            {
                /* 0 means not filled. */
                return null;
            }

            if (FillStyles.Count < idx)
            {
                throw new SWFModellerException(SWFModellerError.Internal,
                        "Fill not found at index " + idx);
            }

            return FillStyles[--idx];
        }
    }
}
