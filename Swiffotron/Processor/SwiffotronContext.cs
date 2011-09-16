//-----------------------------------------------------------------------
// SwiffotronContext.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    public class SwiffotronContext
    {
        /// <summary>
        /// What file are we working on?
        /// </summary>
        public string Name { get; set; }

        public string SentinelString { get; set; }

        public SwiffotronContext(string name)
        {
            /* TODO: Assert not null. */
            this.Name = name;
        }

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
