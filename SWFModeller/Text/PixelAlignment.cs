//-----------------------------------------------------------------------
// PixelAlignment.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Text
{
    using System.Text;

    public class PixelAlignment
    {
        public ZoneData[] ZoneInfo { get; set; }

        public bool HasX { get; set; }

        public bool HasY { get; set; }

        public struct ZoneData
        {
            /* In the SWF these are 16-bit float value, but we just keep them
             * as ints since we don't really need to interpret the values - just
             * store them. */
            public int AlignmentCoord;
            public int Range;
        }

        public override string ToString()
        {
            StringBuilder zones = new StringBuilder("{zones ");
            foreach (ZoneData zd in this.ZoneInfo)
            {
                zones.Append("(AlignCoord: " + zd.AlignmentCoord + ", Range: " + zd.Range + ")");
            }

            zones.Append("}");

            return "[HasX: " + this.HasX + ", HasY: " + this.HasY + ", " + zones.ToString() + "]";
        }
    }
}
