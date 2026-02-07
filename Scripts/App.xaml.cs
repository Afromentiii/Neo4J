using System.Configuration;
using System.Data;
using System.Windows;
using Neo4J.Properties;

namespace Neo4J

{
    public partial class App : Application
    {
        private void CreateShowWindow(Window windowObject)
        {
            Application.Current.MainWindow = windowObject;
            windowObject.Show();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (Settings.Default.IsInstalled == false)
            {
                CreateShowWindow(new InstallWindow());
            }
        }
    }
}
