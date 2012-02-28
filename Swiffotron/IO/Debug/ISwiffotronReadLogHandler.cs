//-----------------------------------------------------------------------
// ISwiffotronReadLogHandler.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.IO.Debug
{
    using SWFProcessing.SWFModeller;

    /// <summary>
    /// When testing swiffotron, we'd like access to the debug logs intended for
    /// the SWFModeller tests, so swiffotron handily passes them back to a test
    /// class via this interface.
    /// </summary>
    public interface ISwiffotronReadLogHandler
    {
        /// <summary>
        /// Callback method called when the swiffotron reads a SWF from a store.
        /// </summary>
        /// <param name="name">The SWF name</param>
        /// <param name="swf">The swf that was read.</param>
        /// <param name="log">A log of read activity</param>
        void OnSwiffotronReadSWF(string name, SWF swf, string log);
    }
}
