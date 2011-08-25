using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using WeeWorld.SWFProcessing.SWFModeller.PrimitiveGraphics.Debug;

namespace WeeWorld.SWFProcessing.SWFModeller
{
    /// <summary>
    /// A stand-in for something which may be a parsed or unparsed shape.
    /// </summary>
    public class MorphShapeProxy : MorphShape
    {
        private MorphShape morphShape;

        /// <summary>
        /// Construct a proxy for a shape implementation
        /// </summary>
        /// <param name="morphShape">The shape to proxy</param>
        public MorphShapeProxy(MorphShape morphShape)
        {
            this.morphShape = morphShape;
        }

        /* TODO: If the internal shape is unparsed, parse it before
         * calling into these methods... */

        /// <inheritdoc />
        public Rect GetEdgeBounds()
        {
            return morphShape.GetEdgeBounds();
        }

        /// <inheritdoc />
        public Rect GetBounds()
        {
            return morphShape.GetBounds();
        }

        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, ref StringBuilder sb)
        {
            ShapeDump.ToStringModelView(morphShape, nest, ref sb);
        }
    }
}
