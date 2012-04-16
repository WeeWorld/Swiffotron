//-----------------------------------------------------------------------
// Program.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SwiffotronServiceClientExample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using SWFProcessing.SwiffotronServiceClientExample.SwiffotronService;

    /// <summary>
    /// Application entry point.
    /// </summary>
    public class Program
    {
        private static string simpleJob =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<swiffotron xmlns=""urn:swiffotron-schemas:swiffotron-job/24/05/2011""
            xmlns:swf=""urn:swiffotron-schemas:swiffotron-job/24/05/2011""
            xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" id=""swf_simple"">
  <swf width=""200"" height=""300"" bgcolor=""#ff0000"" id=""simple"">
    <swfout store=""store://viscomp/redsquare.swf"" />
  </swf>
</swiffotron>";

        public static void Main(string[] args)
        {
            Console.WriteLine("Example SWF job via windows service.");

            ISWFProcessor swiffotron = new SWFProcessorClient();

            byte[] input = new ASCIIEncoding().GetBytes(simpleJob);
            Dictionary<string, byte[]> output = swiffotron.Process(new MemoryStream(input));

            if (output == null)
            {
                Console.WriteLine("No output. Check the event log.");
            }
            else if (!output.ContainsKey("redsquare.swf"))
            {
                Console.WriteLine("Output was empty. Check the event log. Output keys: " + output.Keys.Count);
            }
            else
            {
                Console.WriteLine("out " + output["redsquare.swf"].Length + " bytes");
                using (FileStream save = new FileStream("out.swf", FileMode.Create))
                {
                    save.Write(output["redsquare.swf"], 0, output["redsquare.swf"].Length);
                }
            }

            Console.ReadKey();
        }
    }
}
