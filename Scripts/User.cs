using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4J
{
    public class User
    {
        string username;
        string email;
        string firstName;
        string lastName;
        string role;

        public User(string username, string email, string firstName, string lastName, string role)
        {
            this.username = username;
            this.email = email;
            this.firstName = firstName;
            this.lastName = lastName;
            this.role = role;
        }
    }
}
