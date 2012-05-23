//-----------------------------------------------------------------------
// ABCCatcher.cs
//
//
//-----------------------------------------------------------------------


namespace SwiffotronCLI
{
    using SWFProcessing.SWFModeller.ABC.Debug;
    using System.Collections.Generic;
    using System.IO;

    class ABCCatcher : IABCLoadInterceptor
    {
        private Dictionary<string, byte[]> abcFiles;

        public ABCCatcher()
        {
            this.abcFiles = new Dictionary<string, byte[]>();
        }

        public void OnLoadAbc(bool lazyInit, SWFProcessing.SWFModeller.Process.SWFContext ctx, string abcName, int doAbcCount, byte[] bytecode)
        {
            abcFiles.Add(abcName + "." + doAbcCount + ".abc", bytecode);
        }

        public void SaveAll(string swfName, string dir)
        {
            foreach (string name in abcFiles.Keys)
            {
                using (FileStream fsout = new FileStream(dir + @"\" + swfName + name, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = abcFiles[name];
                    fsout.Write(data, 0, data.Length);
                }
            }
        }
    }
}
