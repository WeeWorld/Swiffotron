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
        /// <summary>The configuration XML file namespace</summary>
        private const string SwiffotronRDF = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description rdf:about=\"\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"><xmp:CreatorTool>http://github.com/izb/Swiffotron</xmp:CreatorTool></rdf:Description></rdf:RDF>";

        public SWFWriterOptions()
        {
            this.RDFMetadata = SWFWriterOptions.SwiffotronRDF;
        }

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

        /// <summary>
        /// RDF metadata. Either null (default) if you don't want any, or a string of
        /// valid RDF if you do. SWFModeller makes no attempt to validate whatever you
        /// pass in here.
        /// </summary>
        public string RDFMetadata { get; set; }
    }
}
