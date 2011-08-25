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
        [OperationContract]
        Dictionary<string, byte[]> Process(Stream jobXml);
    }
}
