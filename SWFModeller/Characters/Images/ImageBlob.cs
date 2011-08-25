//-----------------------------------------------------------------------
// ImageBlob.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Images
{
    using System.Text;

    /// <summary>
    /// A big dumb class that holds onto some bytes and notes the format. At some point
    /// we might like to modify images somehow. At that point, this class will become
    /// a lot more complicated. Not today though. Not today.
    /// </summary>
    public class ImageBlob : IImage
    {
        public Tag DataFormat { get; set; }

        public byte[] FormattedBytes { get; set; }

        /// <summary>
        /// Only valid if DataFormat is DefineBits. If this is true, then FormattedBytes
        /// is not a complete JPEG.
        /// </summary>
        public JPEGTable JPEGTable { get; set; }

#if DEBUG
        public void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner)
        {
            oneLiner = true;
            string indent = new string(' ', nest * 4);
            sb.Append(indent + this.ToString() + "\n");
        }

        public override string ToString()
        {
            if (JPEGTable != null)
            {
                return "[Image (" + this.FormattedBytes.Length + " bytes, " + DataFormat.ToString() + ", JPEGTable " + this.JPEGTable.TableData.Length + " bytes)]";
            }
            else
            {
                return "[Image (" + this.FormattedBytes.Length + " bytes, " + DataFormat.ToString() + ")]";
            }
        }
#endif
    }
}
