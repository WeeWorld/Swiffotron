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
    public class ShapeProxy : Shape
    {
        private Shape realShape;

        /// <summary>
        /// Construct a proxy for a shape object
        /// </summary>
        /// <param name="real">The shape to proxy</param>
        public ShapeProxy(Shape real)
        {
            this.realShape = real;
        }

        /// <inheritdoc />
        public Rect GetBounds()
        {
            return realShape.GetBounds();
        }

        /// <inheritdoc />
        public Rect GetEdgeBounds()
        {
            return realShape.GetEdgeBounds();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return realShape.ToString();
        }

        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, ref StringBuilder sb)
        {
            ShapeDump.ToStringModelView(realShape, nest, ref sb);
        }
    }
}
