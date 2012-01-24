//-----------------------------------------------------------------------
// Service.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SwiffotronService
{
    using System.ServiceProcess;
    using System.Diagnostics;
    using System.ServiceModel;

    public partial class Service : ServiceBase
    {
        ServiceHost host;

        public Service()
        {
            this.InitializeComponent();
            this.AutoLog = false;
            this.EventLog.Source = "Swiffotron";
            this.host = null;
        }

        protected override void OnStart(string[] args)
        {
            this.host = new ServiceHost(typeof(SWFProcessor));
            this.host.Open();

            /* ISSUE 78 - Service still starts if the config is nonsense. */

            EventLog.WriteEntry(this.ServiceName + " has started.", EventLogEntryType.Information, (int)LogEvents.Start);
        }

        protected override void OnStop()
        {
            if (this.host != null)
            {
                this.host.Close();
            }

            EventLog.WriteEntry(this.ServiceName + " has stopped.", EventLogEntryType.Information, (int)LogEvents.Start);
        }
    }
}
