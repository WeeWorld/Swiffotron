//-----------------------------------------------------------------------
// SWFModellerException.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using SWFProcessing.SWFModeller.Process;

    public enum SWFModellerError
    {
        /// <summary>
        /// An error thrown when the code hits something that is unfinished, internally.
        /// TODO: Remove this once it's all settled.
        /// </summary>
        UnimplementedFeature,

        /// <summary>
        /// An error thrown when there is a failure that should not have been permitted to
        /// occur. Indictive of a bug.
        /// </summary>
        Internal,

        /// <summary>
        /// A character was referenced or expected, but there was no character to be found.
        /// </summary>
        CharacterNotFound,

        /// <summary>
        /// An instance was referenced or expected, but there was no instance to be found.
        /// </summary>
        InstanceNotFound,

        /// <summary>
        /// An error occured when parsing SWF data. Indicates a fault in the SWF data itself
        /// rather than our inability to parse it (Which would be an internal error.)
        /// </summary>
        SWFParsing,

        /// <summary>
        /// An error occured when parsing ABC data. Indicates a fault in the ABC data itself
        /// rather than our inability to parse it (Which would be an internal error.)
        /// </summary>
        ABCParsing,

        /// <summary>
        /// An error whilst merging code between SWFs.
        /// </summary>
        CodeMerge,

        /// <summary>
        /// An error whilst manipulating the timeline.
        /// </summary>
        Timeline
    }

    /// <summary>
    /// Exception class for exceptions originating in the swiffotron.
    /// </summary>
    public class SWFModellerException : ApplicationException
    {
        public SWFModellerError Error { get; private set; }

        /// <summary>
        /// See SWFContext for a description of sentinels.
        /// </summary>
        public string Sentinel { get; private set; }

        /// <summary>
        /// Initializes a new instance of an exception with an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="msg">The error message</param>
        public SWFModellerException(SWFModellerError error, string msg, SWFContext ctx)
            : base(error.ToString() + "; " + msg + "; " + ctx.ToString())
        {
            this.Error = error;
            this.Sentinel = ctx.SentinelString;
        }

        /// <summary>
        /// Initializes a new instance of an exception with an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="msg">The error message</param>
        public SWFModellerException(SWFModellerError error, string msg)
            : base(error.ToString() + "; " + msg)
        {
            this.Error = error;
            this.Sentinel = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of an exception without an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        public SWFModellerException(SWFModellerError error, SWFContext ctx)
            : base(error.ToString() + "; " + ctx.ToString())
        {
            this.Error = error;
            this.Sentinel = ctx.SentinelString;
        }

        /// <summary>
        /// Initializes a new instance of an exception without an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        public SWFModellerException(SWFModellerError error)
            : base(error.ToString())
        {
            this.Error = error;
            this.Sentinel = string.Empty;
        }
    }
}
