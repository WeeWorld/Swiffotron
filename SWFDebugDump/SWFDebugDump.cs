//-----------------------------------------------------------------------
// SWFDebugDump.cs
//
//
//-----------------------------------------------------------------------

namespace SWFDebugDump
{
    using System;
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWFModeller.ABC.Debug;
    using SWFProcessing.SWFModeller.ABC.IO;
    using SWFProcessing.SWFModeller.Process;

    /// <summary>
    /// Disassembles SWF and bytecode to text files.
    /// </summary>
    class SWFDebugDump : IABCLoadInterceptor
    {
        public void Run(string[] files)
        {
            try
            {
                foreach (string swfFile in files)
                {
                    if (!File.Exists(swfFile))
                    {
                        Console.WriteLine("Not found: " + swfFile);
                        return;
                    }
                }

                foreach (string swfFile in files)
                {
                    StringBuilder binDump = new StringBuilder();
                    StringBuilder modelDump = new StringBuilder();

                    SWF swf = null;
                    using (FileStream fs = new FileStream(swfFile, FileMode.Open, FileAccess.Read))
                    {
                        swf = new SWFReader(fs, null, binDump, this).ReadSWF(new SWFContext(swfFile));
                        swf.ToStringModelView(0, modelDump);
                    }

                    using (FileStream binOut = new FileStream(swfFile + ".SWFDUMP.bin.txt", FileMode.Create))
                    {
                        byte[] ascii = new ASCIIEncoding().GetBytes(binDump.ToString());
                        binOut.Write(ascii, 0, ascii.Length);
                    }

                    using (FileStream modelOut = new FileStream(swfFile + ".SWFDUMP.model.txt", FileMode.Create))
                    {
                        byte[] ascii = new ASCIIEncoding().GetBytes(modelDump.ToString());
                        modelOut.Write(ascii, 0, ascii.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Main(string[] args)
        {
            new SWFDebugDump().Run(args);
        }

        public void OnLoadAbc(bool lazyInit, SWFContext ctx, string abcName, int doAbcCount, byte[] bytecode)
        {
            StringBuilder readLog = new StringBuilder();
            new AbcReader().Read(bytecode, readLog);

            using (FileStream modelOut = new FileStream(ctx.Name + ".ABCREAD" + "." + doAbcCount + ".txt", FileMode.Create))
            {
                byte[] ascii = new ASCIIEncoding().GetBytes(readLog.ToString());
                modelOut.Write(ascii, 0, ascii.Length);
            }
        }
    }
}
