using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4J.Models
{
    public class User
    {
        public string Username { get; }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Role { get; }

        public User(string username, string email, string firstName, string lastName, string role)
        {
            Username = username;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
        }
    }
}
