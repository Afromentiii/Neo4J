using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Neo4j.Driver;

namespace Neo4J
{
    public partial class InstallWindow : Window
    {
        public InstallWindow()
        {
            InitializeComponent();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            string connectionUrl = TxtUrl.Text;
            string userName = TxtUser.Text;
            string password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(connectionUrl))
            {
                MessageBox.Show("Please enter a valid Connection URL (for example, bolt://localhost:7687)", "Validation Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password cannot be empty.", "Validation Error");
                return;
            }

            try
            {
                BtnInstall.IsEnabled = false;
                await Neo4jDriver.Instance.Initialize(connectionUrl, userName, password);

                ((App)Application.Current).ShowWindow(new MainWindow());
                ((App)Application.Current).CloseWindow(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                BtnInstall.IsEnabled = true;
            }
        }
    }
}

