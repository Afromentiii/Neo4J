using Neo4j.Driver;
using System;
using System.Threading.Tasks;

namespace Neo4J
{
    public class Neo4jDriver : IDisposable
    {
        private static readonly Lazy<Neo4jDriver> _instance =
            new Lazy<Neo4jDriver>(() => new Neo4jDriver());

        public static Neo4jDriver Instance => _instance.Value;

        public IDriver Driver { get; private set; }

        public async Task Initialize(string uri, string user, string password)
        {
            Driver?.Dispose();

            Driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            await Driver.VerifyConnectivityAsync();
        }

        public async Task<bool> UserExists(string username)
        {
            var query = @"
            MATCH (u:User {username: $username})
            RETURN u
            LIMIT 1";

            await using var session = Driver.AsyncSession();
            var result = await session.RunAsync(query, new { username });
            var record = await result.SingleOrDefaultAsync();

            if (record != null) return true;
            return false;
        }

        public async Task CreateUser(string firstName, string lastName, 
                                     string email, string password, 
                                     string username)
        {
            if (await UserExists(username))
                throw new Exception("User already exists");

            var query = @"
            CREATE (u:User {
                username: $username,
                firstName: $firstName,
                lastName: $lastName,
                email: $email,
                password: $password,
                role: 'user'
            })";

            await using var session = Driver.AsyncSession();
            await session.RunAsync(query, new { username, firstName, lastName, email, password });
        }

        public async Task <bool> VerifyUser(string username, string password)
        {
            if (await UserExists(username))
            {
                var query = @"
                MATCH (u:User { username: $username })
                RETURN u.password";

                await using var session = Driver.AsyncSession();
                var result = await session.RunAsync(query, new { username });
                var record = await result.SingleOrDefaultAsync();

                if (record != null)
                {
                    var passwordHash = record["u.password"].As<string>();
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
                    return isPasswordValid;
                }
            }
            return false;
        }
        
        public async Task<User> GetUserData(string username)
        {
            if (await UserExists(username))
            {
                var query = @"
                MATCH (u:User { username: $username })
                RETURN u.firstName AS firstName, 
                       u.lastName AS lastName, 
                       u.email AS email, 
                       u.role AS role";

                await using var session = Driver.AsyncSession();
                var result = await session.RunAsync(query, new { username });
                var record = await result.SingleOrDefaultAsync();

                if(record != null)
                {
                   var firstName = record["firstName"].As<string>();
                   var lastName = record["lastName"].As<string>();
                   var email = record["email"].As<string>();
                   var role = record["role"].As<string>();
                   return new User(username, email, firstName, lastName, role);
                }
            }
            return null;
        }
        public void Dispose()
        {
            Driver?.Dispose();
        }
    }
}