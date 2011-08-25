//-----------------------------------------------------------------------
// IABCLoadInterceptor.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Debug
{
    /// <summary>
    /// Externally implemented interface which gets posted all blocks of
    /// bytecode that pass past the SWFReader. Namely this means the unit test
    /// classes which can then dump the bytecode into the test output folders
    /// so that we can load them into hex editors, disassemblers and then be
    /// none the wiser.
    /// </summary>
    public interface IABCLoadInterceptor
    {
        /// <summary>
        /// Callback sent when bytecode is loaded from a SWF.
        /// </summary>
        /// <param name="lazyInit">Is the code lazy initialized?</param>
        /// <param name="swfName">The name of the SWF (For debug identification)</param>
        /// <param name="abcName">The name of the ABC (For debug identification)</param>
        /// <param name="doAbcCount">How many ABC blocks are loaded from its SWF so far?</param>
        /// <param name="bytecode">The unparsed ABC data.</param>
        void OnLoadAbc(bool lazyInit, string swfName, string abcName, int doAbcCount, byte[] bytecode);
    }
}
