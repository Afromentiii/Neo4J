using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Neo4J.Models
{
    public class User : INotifyPropertyChanged
    {
        public string Username { get; }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Role { get; }

        private bool _isFollowed;
        public bool IsFollowed 
        { 
            get => _isFollowed; 
            set { _isFollowed = value; OnPropertyChanged(); } 
        }

        public User(string username, string email = null, string firstName = null, string lastName = null, string role = null)
        {
            Username = username;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
