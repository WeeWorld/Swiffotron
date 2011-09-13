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

        public SwiffotronContext(string name)
        {
            /* TODO: Assert not null. */
            this.Name = name;
        }

        public override string ToString()
        {
            return "[Name: " + this.Name + "]";
        }
    }
}
