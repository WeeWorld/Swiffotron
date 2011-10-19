//-----------------------------------------------------------------------
// ISWFProcessor.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SwiffotronService
{
    using System.Collections.Generic;
    using System.IO;
    using System.ServiceModel;

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISWFProcessor" in both code and config file together.
    [ServiceContract]
    public interface ISWFProcessor
    {
        /// <summary>
        /// Process a swiffotron job.
        /// </summary>
        /// <param name="jobXml">A stream at the start of job XML data</param>
        /// <returns>A map of store commits performed by the job</returns>
        [OperationContract]
        Dictionary<string, byte[]> Process(Stream jobXml);

        /// <summary>
        /// Interrogate the service to find out interesting information about it, such as how
        /// it is configured and how it's performing. Useful to check the service before a unit
        /// test to make sure it's set up properly.
        /// </summary>
        /// <returns>A big map of string->string arbitrary information.</returns>
        [OperationContract]
        Dictionary<string, string> Interrogate();
    }
}
