//-----------------------------------------------------------------------
// SWFProcessor.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SwiffotronService
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using SWFProcessing.Swiffotron;

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SWFProcessor" in both code and config file together.
    public class SWFProcessor : ISWFProcessor
    {
        private EventLog EventLog { get; set; }

        public Swiffotron Swiffotron { get; set; }

        public SWFProcessor()
        {
            this.EventLog = new EventLog();
            this.EventLog.Source = "Swiffotron";
            this.EventLog.Log = "Application";

            string swiffotronDLLPath = Assembly.GetAssembly(typeof(Swiffotron)).CodeBase;
            if (swiffotronDLLPath.StartsWith("file:///"))
            {
                swiffotronDLLPath = swiffotronDLLPath.Substring(8);
            }
            FileInfo dllInfo = new FileInfo(swiffotronDLLPath);
            DirectoryInfo dllDir = dllInfo.Directory;

            string configName = ConfigurationManager.AppSettings["SwiffotronConfig"];

            if (configName == null || configName == string.Empty)
            {
                configName = @"swiffotron-config.xml";
            }

            FileInfo configFile = new FileInfo(configName);
            if (!configFile.Exists)
            {
                configFile = new FileInfo(dllDir + @"\" + configName);
            }

            if (!configFile.Exists)
            {
                this.EventLog.WriteEntry(
                        "Failed to find a Swiffotron config file at " + configFile.FullName,
                        EventLogEntryType.Error,
                        (int)LogEvents.BadConfig);
                return;
            }

            try
            {
                using (FileStream fs = new FileStream(configFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    this.Swiffotron = new Swiffotron(fs);
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)LogEvents.BadConfig);
            }

            this.EventLog.WriteEntry("Swiffotron is using configuration found at " + configFile.FullName + " (SwiffotronConfig = '" + ConfigurationManager.AppSettings["SwiffotronConfig"] + "')", EventLogEntryType.Information, (int)LogEvents.StartupInfo);
        }

        /// <returns>null if the job could not be processed. An error will be recorded in the
        /// Windows event log.</returns>
        public Dictionary<string, byte[]> Process(Stream jobXml)
        {
            try
            {
                Dictionary<string, byte[]> commits = new Dictionary<string, byte[]>();

                this.Swiffotron.Process(jobXml, commits);

                return commits;
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)LogEvents.BadJob);
                return null;
            }
        }


        public Dictionary<string, string> Interrogate()
        {
            Dictionary<string, string> info = this.Swiffotron.Interrogate();

            /* TODO: Add some service information here, perhaps. */

            return info;
        }
    }
}
