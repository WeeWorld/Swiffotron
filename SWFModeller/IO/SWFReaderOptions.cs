//-----------------------------------------------------------------------
// SWFReaderOptions.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.IO
{
    public class SWFReaderOptions
    {
        public SWFReaderOptions()
        {
            this.StrictTagLength = false;
        }

        /// <summary>
        /// Set this to true if exceptions should be thrown if tag lengths in SWF files
        /// should be strictly adhered to. Exceptions will be thrown if there are spare
        /// bytes at the end of tags. Set to false so skip extra bytes, though
        /// exceptions will still be thrown if tags overrun their lengths.
        /// </summary>
        public bool StrictTagLength { get; set; }
    }
}
