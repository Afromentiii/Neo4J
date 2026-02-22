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
using BCrypt.Net;
namespace Neo4J
{
    public partial class LoginRegisterWindow : Window
    {

        public LoginRegisterWindow()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginUsername.Text;
            string password = LoginPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show($"Fields cannot be empty!", "Error");
                return;
            }

            try
            {
                ((System.Windows.Controls.Button)sender).IsEnabled = false;
                if (await Neo4jDriver.Instance.VerifyUser(username, password))
                {
                    MessageBox.Show("Login successful!");
                    User currentUser = await Neo4jDriver.Instance.GetUserData(username);
                    ((App)Application.Current).ShowWindow(new MainWindow(currentUser));
                    ((App)Application.Current).CloseWindow(this);
                }
                else
                {
                    MessageBox.Show("Login or password are not correct!");
                }
            }
            finally { ((System.Windows.Controls.Button)sender).IsEnabled = true; }
        }
        private async void BtnRegister_Click(object sender, RoutedEventArgs e) 
        {
            string username = RegUsername.Text;
            string email = RegEmail.Text;
            string password = RegPassword.Password;
            string confirmPass = RegConfirmPassword.Password;
            string firstName = RegFirstName.Text;
            string lastName = RegLastName.Text;

            var fields = new Dictionary<string, string>
            {
                { "Username", username },
                { "Password", password },
                { "Confirm Password", confirmPass },
                { "First Name", firstName },
                { "Last Name", lastName }
            };

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                {
                    MessageBox.Show($"The {field.Key} field has not been filled!", "Error");
                    return;
                }
            }

            if (password.Length < 8)
            {
                MessageBox.Show("The password too short! Minimum length is 8", "Error");
                return;
            }

            if (password != confirmPass)
            {
                MessageBox.Show("Passwords do not match!", "Error");
                return;
            }

            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(confirmPass);
                ((System.Windows.Controls.Button)sender).IsEnabled = false;
                await Neo4jDriver.Instance.CreateUser(firstName, lastName, email, passwordHash, username);
                MessageBox.Show($"User has been added!");
                LoginBtn.IsSelected = true;
            }
            catch (Exception ex) {MessageBox.Show($"Error: {ex.Message}");}
            finally { ((System.Windows.Controls.Button)sender).IsEnabled = true;}
            
        }
    }
}
