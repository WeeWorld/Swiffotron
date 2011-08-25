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
            InitializeComponent();
            this.AutoLog = false;
            EventLog.Source = "Swiffotron";
            host = null;
        }

        protected override void OnStart(string[] args)
        {
            host = new ServiceHost(typeof(SWFProcessor));
            host.Open();

            EventLog.WriteEntry(this.ServiceName + " has started.", EventLogEntryType.Information, (int)LogEvents.Start);
        }

        protected override void OnStop()
        {
            if (host != null)
            {
                host.Close();
            }

            EventLog.WriteEntry(this.ServiceName + " has stopped.", EventLogEntryType.Information, (int)LogEvents.Start);
        }
    }
}
