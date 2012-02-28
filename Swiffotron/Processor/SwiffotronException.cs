//-----------------------------------------------------------------------
// SwiffotronException.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron
{
    using System;
    using SWFProcessing.Swiffotron.Processor;

    public enum SwiffotronError
    {
        /// <summary>
        /// An error thrown when the code hits something that is unfinished, internally.
        /// ISSUE 37: Remove this once it's all settled.
        /// </summary>
        UnimplementedFeature,

        /// <summary>
        /// An error thrown when there is a failure that should not have been permitted to
        /// occur. Indictive of a bug.
        /// </summary>
        Internal,

        /// <summary>
        /// There was some sort of problem with the input job XML.
        /// </summary>
        BadInputXML,

        /// <summary>
        /// Something went wrong in an input SWF, e.g. there was a search for an instance name
        /// that wasn't there. The SWF itself is valid, just not what was expected as input.
        /// </summary>
        BadPathOrID,
    }

    /// <summary>
    /// Exception class for exceptions originating in the swiffotron.
    /// </summary>
    public class SwiffotronException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of an exception with an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="ctx">A context object that describes what was being done when the exception occured.</param>
        /// <param name="msg">The error message</param>
        /// <param name="inner">The inner exception</param>
        public SwiffotronException(SwiffotronError error, SwiffotronContext ctx, string msg, Exception inner)
            : base(error.ToString() + "; " + msg + "; " + ctx.ToString(), inner)
        {
            this.Error = error;
            this.Sentinel = ctx.SentinelString;
        }

        /// <summary>
        /// Initializes a new instance of an exception with an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="ctx">A context object that describes what was being done when the exception occured.</param>
        /// <param name="msg">The error message</param>
        public SwiffotronException(SwiffotronError error, SwiffotronContext ctx, string msg)
            : base(error.ToString() + "; " + msg + "; " + ctx.ToString())
        {
            this.Error = error;
            this.Sentinel = ctx.SentinelString;
        }

        /// <summary>
        /// Initializes a new instance of an exception without an error message
        /// </summary>
        /// <param name="error">The error code.</param>
        /// <param name="ctx">A context object that describes what was being done when the exception occured.</param>
        public SwiffotronException(SwiffotronError error, SwiffotronContext ctx)
            : base(error.ToString() + "; " + ctx.ToString())
        {
            this.Error = error;
            this.Sentinel = ctx.SentinelString;
        }

        /// <summary>
        /// An error code that describes the type of error that occured.
        /// </summary>
        public SwiffotronError Error { get; private set; }

        /// <summary>
        /// See SwiffotronContext for a description of sentinels.
        /// </summary>
        public string Sentinel { get; private set; }
    }
}
