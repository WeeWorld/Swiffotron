//-----------------------------------------------------------------------
// SwiffotronContext.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    /// <summary>
    /// The context within which the current swiffotron job is running, so that we
    /// can create better log information.
    /// </summary>
    public class SwiffotronContext
    {
        public SwiffotronContext(string name)
        {
            if (name == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.Internal,
                        new SwiffotronContext("null"), /* Yeah, I know. This looks wrong. */
                        "Can't have a null in a SwiffotronContext name");
            }

            this.Name = name;
        }

        /// <summary>
        /// What file are we working on?
        /// </summary>
        public string Name { get; set; }

        /// <see cref="Sentinel"/>
        public string SentinelString { get; set; }

        public override string ToString()
        {
            return "[Job ID: " + this.Name + "]";
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
        /// To mark a moment in the code, access the context object via this method
        /// like this:
        /// myContext.Sentinel("My meaningful string"); // returns myContext
        /// </summary>
        /// <param name="sentinelLabel">An arbitrary string.</param>
        /// <returns>The modified context.</returns>
        public SwiffotronContext Sentinel(string sentinelLabel)
        {
            this.SentinelString = sentinelLabel;
            return this;
        }
    }
}
