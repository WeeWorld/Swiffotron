using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using WeeWorld.SWFProcessing.SWFModeller.PrimitiveGraphics.Debug;

namespace WeeWorld.SWFProcessing.SWFModeller
{
    /// <summary>
    /// A morph shape in its unparsed form. We gamble that we'll
    /// probably never have to parse it, and so it becomes quicker to
    /// save out when we re-save the SWF.
    /// </summary>
    public class UnparsedMorphShape : MorphShape
    {
        private byte[] data;

        /// <summary>
        /// Construct an unparsed shape
        /// </summary>
        /// <param name="data">The unparsed binary shape data.</param>
        public UnparsedMorphShape(byte[] data)
        {
            this.data = data;
        }

        /// <inheritdoc />
        public Rect GetEdgeBounds()
        {
            return null;
        }

        /// <inheritdoc />
        public Rect GetBounds()
        {
            return null;
        }

        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, ref StringBuilder sb)
        {
            string indent = new String(' ', nest * 4);
            sb.Append(indent + "Unparsed morph shape (" + data.Length + " bytes)\n");
        }

    }
}
