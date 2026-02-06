using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace Neo4J
{
    /// <summary>
    /// Logika interakcji dla klasy LoginWindow.xaml
    /// </summary>
    public partial class InstallWindow : Window
    {
        public InstallWindow()
        {
            InitializeComponent();
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowWindow(new MainWindow());
            ((App)Application.Current).CloseWindow(this);
        }
    }
}

