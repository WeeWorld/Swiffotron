//-----------------------------------------------------------------------
// SWF2HTMLException.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML
{
    using System;

    public enum SWF2HTMLError
    {
        /// <summary>
        /// An error thrown when the code hits something that is unfinished, internally.
        /// ISSUE 37: Remove this once it's all settled.
        /// </summary>
        UnimplementedFeature,
    }

    /// <summary>
    /// Exception class for exceptions originating in the swiffotron.
    /// </summary>
    public class SWF2HTMLException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of an exception with an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="ctx">A context object that describes what was being done when the exception occured.</param>
        /// <param name="msg">The error message</param>
        /// <param name="inner">The inner exception</param>
        public SWF2HTMLException(SWF2HTMLError error, string msg, Exception inner)
            : base(error.ToString() + "; " + msg, inner)
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of an exception with an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="ctx">A context object that describes what was being done when the exception occured.</param>
        /// <param name="msg">The error message</param>
        public SWF2HTMLException(SWF2HTMLError error, string msg)
            : base(error.ToString() + "; " + msg)
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of an exception without an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="ctx">A context object that describes what was being done when the exception occured.</param>
        public SWF2HTMLException(SWF2HTMLError error)
            : base(error.ToString())
        {
            this.Error = error;
        }

        /// <summary>
        /// An error code that describes the type of error that occured.
        /// </summary>
        public SWF2HTMLError Error { get; private set; }
    }
}
