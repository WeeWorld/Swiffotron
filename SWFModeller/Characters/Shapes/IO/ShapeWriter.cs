//-----------------------------------------------------------------------
// ShapeWriter.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.IO
{
    using SWFProcessing.SWFModeller.Util;

    /* TODO: I think the little fragments of IO scattered around the project should be under
     * the main IO package, e.g. SWFModeller.IO.Shapes. Go on a refactoring exercise please. */

    internal class ShapeWriter
    {
        /// <summary>
        /// Write a shape as bytes.
        /// </summary>
        /// <param name="shape">The shape to write.</param>
        /// <returns>A byte array. This is the portion of the tag after the character ID which is
        /// written by the SWFWriter class.</returns>
        public static byte[] ShapeToBytes(IShape shape, out Tag dataFormat)
        {
            if (shape.OriginalBytes != null)
            {
                dataFormat = shape.OriginalFormat;
                return shape.OriginalBytes;
            }

            if (shape is MorphShape)
            {
                throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Can't write morph shapes.");
            }
            else if (shape is Shape)
            {
                throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Can't write shapes.");
            }
            else
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Unrecognised shape kind.");
            }
        }
    }
}
