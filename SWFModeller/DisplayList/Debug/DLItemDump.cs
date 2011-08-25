//-----------------------------------------------------------------------
// DLItemDump.cs
//
//
//-----------------------------------------------------------------------

#if DEBUG
namespace SWFProcessing.SWFModeller.Debug
{
    using System;
    using System.Text;

    /// <summary>
    /// Static class that knows how to turn display list items into text renditions for
    /// unit test output.
    /// </summary>
    public static class DLItemDump
    {
        /// <summary>
        /// Turns a display list item into text.
        /// </summary>
        /// <param name="dli">The item</param>
        /// <param name="nest">Any current log output nesting level</param>
        /// <param name="sb">The string buffer to write the text into.</param>
        public static void ToStringModelView(IDisplayListItem dli, int nest, StringBuilder sb)
        {
            if (dli is PlaceObject)
            {
                ((PlaceObject)dli).ToStringModelView(nest, sb);
            }
            else if (dli is RemoveObject)
            {
                ((RemoveObject)dli).ToStringModelView(nest, sb);
            }
            else
            {
                throw new Exception("*** ERROR: CAN'T DUMP: " + dli.ToString());
            }
        }
    }
}
#endif
