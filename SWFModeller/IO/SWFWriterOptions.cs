//-----------------------------------------------------------------------
// SWFWriterOptions.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    /// <summary>
    /// A bunch of properties that affect how SWFWriter writes stuff.
    /// </summary>
    public class SWFWriterOptions
    {
        /// <summary>
        /// true if the output should be deflated.
        /// </summary>
        public bool Compressed { get; set; }

        /// <summary>
        /// If true then bytecode will contain debug line numbers and file names.
        /// These are only useful when debugging this library though, so don't expect
        /// the errors to make sense in the real world. Note that the debug password
        /// will be an empty string.
        /// ISSUE 53: If this is false, any input SWF files should be stripped of their
        /// debug information.
        /// </summary>
        public bool EnableDebugger { get; set; }
    }
}
