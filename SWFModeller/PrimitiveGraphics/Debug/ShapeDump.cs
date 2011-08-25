#if DEBUG
using System;
using System.Text;
using System.Diagnostics;

using WeeWorld.SWFProcessing.SWFModeller.PrimitiveGraphics;

namespace WeeWorld.SWFProcessing.SWFModeller.PrimitiveGraphics.Debug
{
    public static class ShapeDump
    {

        public static void ToStringModelView(Shape shape, int nest, ref StringBuilder sb)
        {
            if (shape is ShapeProxy)
            {
                ((ShapeProxy)shape).ToStringModelView(nest, ref sb);
            }
            else if (shape is UnparsedShape)
            {
                ((UnparsedShape)shape).ToStringModelView(nest, ref sb);
            }
            else if (shape is MorphShapeProxy)
            {
                ((MorphShapeProxy)shape).ToStringModelView(nest, ref sb);
            }
            else if (shape is UnparsedMorphShape)
            {
                ((UnparsedMorphShape)shape).ToStringModelView(nest, ref sb);
            }
            else
            {
                throw new Exception("*** ERROR: CAN'T DUMP: " + shape.ToString());
            }
        }
    }
}
#endif
