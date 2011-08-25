using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WeeWorld.SWFProcessing.SWFModeller
{
    /// <summary>
    /// A shape in its unparsed form. We gamble that we'll
    /// probably never have to parse it, and so it becomes quicker to
    /// save out when we re-save the SWF.
    /// </summary>
    class UnparsedShape : Shape
    {
        private Rect bounds;
        private Rect edgeBounds;
        private byte[] data;

        /// <summary>
        /// An unparsed shape, normally accessed through a ShapeProxy so that it can be
        /// exchanged for a parsed one if required.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="edgeBounds">Pass null if the shape doesn't have this specified.</param>
        /// <param name="data"></param>
        public UnparsedShape(Rect bounds, Rect edgeBounds, byte[] data)
        {
            this.bounds = bounds;
            this.edgeBounds = edgeBounds;
            this.data = data;
        }

        /// <inheritdoc />
        public Rect GetBounds()
        {
            return bounds;
        }

        /// <inheritdoc />
        public Rect GetEdgeBounds()
        {
            return edgeBounds;
        }

        /// <inheritdoc />
        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, ref StringBuilder sb)
        {
            string indent = new String(' ', nest * 4);
            sb.Append(indent + "Unparsed shape (" + data.Length + " bytes) bounds=" +
                    bounds + ", edgeBounds=" +
                    (edgeBounds==null?"none":edgeBounds.ToString()) + "\n");
        }
    }
}
