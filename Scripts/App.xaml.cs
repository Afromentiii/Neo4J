using System.Configuration;
using System.Data;
using System.Windows;
using Neo4J.Properties;

namespace Neo4J

{
    public partial class App : Application
    {
        public void ShowWindow(Window windowObject)
        {
            Application.Current.MainWindow = windowObject;
            windowObject.Show();
        }

        public void CloseWindow(Window windowObject)
        {
            windowObject.Close();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (Settings.Default.IsInstalled == false)
            {
                ShowWindow(new InstallWindow());
            }
        }
    }
}
