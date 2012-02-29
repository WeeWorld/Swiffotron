//-----------------------------------------------------------------------
// SWF2Raster.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2Raster
{
    using SWFProcessing.SWFModeller;
    using System.IO;

    public class SWF2Raster
    {
        public SWF2Raster(SWF swf)
        {
            this.Swf = swf;
        }

        public SWF Swf { get; set; }

        public Stream GetPNG()
        {
            return new MemoryStream(GetPNGAsBytes());
        }

        public byte[] GetPNGAsBytes()
        {
            return new byte[0];
        }
    }
}
