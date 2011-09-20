//-----------------------------------------------------------------------
// ProjectInstaller.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SwiffotronService
{
    using System.ComponentModel;

    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller_AfterInstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {
            /* ISSUE 33: Somehow we need to copy the default config for Swiffotron into the
             * installation folder. It's an embedded resource though, and I have no
             * idea how this crazy setup stuff is meant to find it. It does get copied
             * into the project output though, along with the XSD files. You'd think 
             * that'd be enough.
             * Maybe we can hack the primary output list in the setup project file, or something. */
        }

        private void serviceInstaller_AfterUninstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {

        }
    }
}
