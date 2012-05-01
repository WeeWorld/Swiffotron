//-----------------------------------------------------------------------
// SWF2HTMLOptions.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML
{
    public class SWF2HTMLOptions
    {
        public enum FrameworkType
        {
            JQuery,
            RawJS
        }

        public SWF2HTMLOptions()
        {
            this.Framework = FrameworkType.JQuery;
            this.OutputComments = false;
            this.ConsoleLogging = false;
        }

        /// <summary>
        /// Set this to true if exceptions should be thrown if tag lengths in SWF files
        /// should be strictly adhered to. Exceptions will be thrown if there are spare
        /// bytes at the end of tags. Set to false so skip extra bytes, though
        /// exceptions will still be thrown if tags overrun their lengths.
        /// </summary>
        public FrameworkType Framework { get; set; }

        public bool OutputComments { get; set; }

        public bool ConsoleLogging { get; set; }
    }
}
