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

        public SWFContext(string name)
        {
            /* TODO: Assert not null. */
            if (name == string.Empty)
            {
                name = "untitled";
            }
            this.Name = name;
        }

        public override string ToString()
        {
            return "[Name: " + this.Name + "]";
        }
    }
}
