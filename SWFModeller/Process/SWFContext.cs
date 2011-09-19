//-----------------------------------------------------------------------
// SWFContext.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Process
{
    /// <summary>
    /// For errors and exceptions, this object represents what the modeller is currently
    /// working on and allows for better log messages to help work out precisely where
    /// things went wrong.
    /// </summary>
    public class SWFContext
    {
        /// <summary>
        /// What file are we working on?
        /// </summary>
        public string Name { get; set; }

        public string SentinelString { get; set; }

        public SWFContext(string name)
        {
            if (name == null)
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Can't have a null in a SWFContext name");
            }

            if (name == string.Empty)
            {
                name = "untitled";
            }
            this.Name = name;
        }

        public override string ToString()
        {
            return "[SWF Context: " + this.Name + "]";
        }

        /// <summary>
        /// When an exception is thrown, we can pass a sentinel string into the
        /// context. This is a secret internal string that somehow marks the point
        /// that the code has reached. This can be picked up in unit tests to
        /// determine that not only was the right type of exception was thrown, but
        /// that it was thrown for the right reason.
        /// Secret sentinel strings have the advantages of being easy to make up on
        /// the spot and immunity to internationalisation, which makes them better
        /// than methods that compare error messages, which can change on a whim.
        /// </summary>
        /// <param name="sentinelLabel">An arbitrary string.</param>
        /// <returns>The modified context.</returns>
        public SWFContext Sentinel(string sentinelLabel)
        {
            this.SentinelString = sentinelLabel;
            return this;
        }
    }
}
