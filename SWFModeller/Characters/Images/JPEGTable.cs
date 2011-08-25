//-----------------------------------------------------------------------
// JPEGTable.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Images
{
    /// <summary>
    /// JPEGs can be stored in two parts - the tables and the image data. The
    /// key point being that multiple images in the SWF can all reference the same
    /// JPEG table to save space. There is only one of these things per SWF, so
    /// when writing images we need to take care to check that they all
    /// reference the same table.
    /// TODO: When writing, if we encounter a second table, then the image should
    /// be converted from a DefineBits to a DefineJPEG2 tag by inlining the table.
    /// </summary>
    public class JPEGTable
    {
        public JPEGTable(byte[] data)
        {
            this.TableData = data;
        }

        public byte[] TableData { get; set; }
    }
}
