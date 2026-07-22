using Neo4j.Driver;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using Neo4J.Models;

namespace Neo4J.Services
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

        private async Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> queryFunc, string errorMessage = "Database read error", T defaultValue = default)
        {
            await using var session = Driver.AsyncSession();
            try
            {
                return await session.ExecuteReadAsync(queryFunc);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return defaultValue;
            }
        }

        private async Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> queryFunc, string errorMessage = "Database write error", T defaultValue = default)
        {
            await using var session = Driver.AsyncSession();
            try
            {
                return await session.ExecuteWriteAsync(queryFunc);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return defaultValue;
            }
        }

        private async Task<T> ReadSingleAsync<T>(string query, object parameters, Func<IRecord, T> mapper, string errorMessage = "Database read error", T defaultValue = default)
        {
            return await ExecuteReadAsync(async tx =>
            {
                var cursor = parameters == null ? await tx.RunAsync(query) : await tx.RunAsync(query, parameters);
                var record = await cursor.SingleOrDefaultAsync();
                return record != null ? mapper(record) : defaultValue;
            }, errorMessage, defaultValue);
        }

        private async Task<List<T>> ReadListAsync<T>(string query, object parameters, Func<IRecord, T> mapper, string errorMessage = "Database read error")
        {
            return await ExecuteReadAsync(async tx =>
            {
                var cursor = parameters == null ? await tx.RunAsync(query) : await tx.RunAsync(query, parameters);
                var records = await cursor.ToListAsync();
                var list = new List<T>();
                foreach (var record in records)
                {
                    list.Add(mapper(record));
                }
                return list;
            }, errorMessage, new List<T>());
        }

        private async Task<bool> WriteAsync(string query, object parameters, string errorMessage = "Database write error")
        {
            return await ExecuteWriteAsync(async tx =>
            {
                var cursor = parameters == null ? await tx.RunAsync(query) : await tx.RunAsync(query, parameters);
                await cursor.ConsumeAsync();
                return true;
            }, errorMessage, false);
        }

        private async Task<T> WriteWithResultAsync<T>(string query, object parameters, Func<IRecord, T> mapper, string errorMessage = "Database write error", T defaultValue = default)
        {
            return await ExecuteWriteAsync(async tx =>
            {
                var cursor = parameters == null ? await tx.RunAsync(query) : await tx.RunAsync(query, parameters);
                var record = await cursor.SingleOrDefaultAsync();
                return record != null ? mapper(record) : defaultValue;
            }, errorMessage, defaultValue);
        }

        public async Task<bool> UserExists(string username)
        {
            var query = @"
            MATCH (u:User {username: $username})
            RETURN u
            LIMIT 1";

            return await ReadSingleAsync(query, new { username }, record => true, "Error checking if user exists", false);
        }

        public async Task<bool> CreateUser(string firstName, string lastName, string email, string password, string username)
        {
            if (await UserExists(username))
            {
                MessageBox.Show("User already exists", "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var query = @"
            CREATE (u:User {
                username: $username,
                firstName: $firstName,
                lastName: $lastName,
                email: $email,
                password: $password,
                role: 'user'
            })";

            return await WriteAsync(query, new { username, firstName, lastName, email, password }, "Error creating user");
        }

        public async Task<bool> VerifyUser(string username, string password)
        {
            if (!await UserExists(username)) return false;

            var query = @"
            MATCH (u:User { username: $username })
            RETURN u.password";

            return await ReadSingleAsync(query, new { username }, record => 
            {
                var passwordHash = record["u.password"].As<string>();
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }, "Error verifying user", false);
        }
        
        public async Task<User> GetUserData(string username)
        {
            if (!await UserExists(username)) return null;

            var query = @"
            MATCH (u:User { username: $username })
            RETURN u.firstName AS firstName, 
                   u.lastName AS lastName, 
                   u.email AS email, 
                   u.role AS role";

            return await ReadSingleAsync(query, new { username }, record => 
            {
                var firstName = record["firstName"].As<string>();
                var lastName = record["lastName"].As<string>();
                var email = record["email"].As<string>();
                var role = record["role"].As<string>();
                return new User(username, email, firstName, lastName, role);
            }, "Error getting user data", null);
        }

        public async Task<List<Movie>> GetMovies()
        {
            string query = @"MATCH (m:Movie) 
                             RETURN m.title AS title, 
                                    m.genre AS genre, 
                                    m.released AS released,
                                    m.poster AS poster";

            return await ReadListAsync(query, null, record => 
            {
                var title = record["title"].As<string>();
                return new Movie
                {
                    Title = title,
                    Genre = record["genre"].As<string>(),
                    Released = record["released"].As<int>(),
                    PosterUrl = $"/Posters/{title}.jpg",
                    DisplayInfo = $"{record["released"].As<int>()} • {record["genre"].As<string>()}"
                };
            }, "Error downloading movies");
        }

        public async Task<bool> CreateLikeMovieRelation(string username, string movieTitle)
        {
            var query = @"
            MATCH (u:User { username: $username }), 
                  (m:Movie { title: $movieTitle })
            MERGE (u)-[:LIKED]->(m)
            RETURN u, m";

            return await WriteWithResultAsync(query, new { username, movieTitle }, record => true, "Error liking movie", false);
        }

        public async Task<bool> CreateObservesRelation(string currentUserUsername, string targetUsername)
        {
            var query = @"
            MATCH (u:User { username: $currentUserUsername }), 
                  (t:User { username: $targetUsername })
            MERGE (u)-[:OBSERVES]->(t)
            RETURN u, t";

            return await WriteWithResultAsync(query, new { currentUserUsername, targetUsername }, record => true, "Error following user", false);
        }

        public async Task<HashSet<string>> GetLikedMovieTitles(string username)
        {
            var query = @"
            MATCH (u:User {username: $username})-[:LIKED]->(m:Movie)
            RETURN m.title AS Title";

            var list = await ReadListAsync(query, new { username }, record => record["Title"].As<string>(), "Error getting liked movies");
            return new HashSet<string>(list);
        }

        public async Task<bool> RemoveLikeMovieRelation(string username, string movieTitle)
        {
           var query = @"MATCH (u:User { username: $username })-[r:LIKED]->(m:Movie { title: $movieTitle })
                        DELETE r
                        RETURN count(r) AS deletedCount";

            return await WriteWithResultAsync(query, new { username, movieTitle }, record => record["deletedCount"].As<int>() > 0, "Error removing liked relation", false);
        }

        public async Task<List<object>> GetUsers(string currentUser)
        {
            string query = @"
                    MATCH (u:User)
                    WHERE u.username <> $currentUser
                    RETURN u.username AS username";

            return await ReadListAsync<object>(query, new { currentUser }, record => new { Username = record["username"].As<string>() }, "Error downloading users");
        }

        public async Task<HashSet<string>> GetFollowedUsers(string username)
        {
            var query = @"
            MATCH (u:User {username: $username})-[:OBSERVES]->(f:User)
            RETURN f.username AS FollowedUsername";

            var list = await ReadListAsync(query, new { username }, record => record["FollowedUsername"].As<string>(), "Error getting followed users");
            return new HashSet<string>(list);
        }

        public async Task<bool> RemoveObservesRelation(string followerUsername, string followedUsername)
        {
            var query = @"
            MATCH (follower:User { username: $followerUsername })-[r:OBSERVES]->(followed:User { username: $followedUsername })
            DELETE r
            RETURN count(r) AS deletedCount";

            return await WriteWithResultAsync(query, new { followerUsername, followedUsername }, record => record["deletedCount"].As<int>() > 0, "Error removing observed relation", false);
        }

        public async Task<List<Movie>> RecommendMoviesForUser(string username, int limit = 10)
        {
            var query = @"
            MATCH (u:User {username: $username})-[:OBSERVES]->(f:User)-[:LIKED]->(m:Movie)
            WHERE NOT (u)-[:LIKED]->(m)
            RETURN m.title AS title, m.genre AS genre, m.released AS released, m.poster AS poster,
                   count(*) AS score
            ORDER BY score DESC
            LIMIT $limit";

            return await ReadListAsync(query, new { username, limit }, record => 
            {
                var title = record["title"].As<string>();
                var score = record["score"].As<int>();
                return new Movie
                {
                    Title = title,
                    Genre = record["genre"].As<string>(),
                    Released = record["released"].As<int>(),
                    PosterUrl = $"/Posters/{title}.jpg",
                    DisplayInfo = $"{record["released"].As<int>()} • {record["genre"].As<string>()}",
                    RecommendationInfo = $"Recommended by {score} people"
                };
            }, "Error getting recommended movies by users");
        }

        public async Task<List<Movie>> RecommendMoviesByGenre(string username, int limit = 10)
        {
            var query = @"
            MATCH (u:User {username: $username})-[:LIKED]->(:Movie)-[:IN_GENRE]->(g:Genre)
            MATCH (g)<-[:IN_GENRE]-(m:Movie)
            WHERE NOT (u)-[:LIKED]->(m)
            RETURN m.title AS title,
                   m.genre AS genre,
                   m.released AS released,
                   m.poster AS poster,
                   count(*) AS score
            ORDER BY score DESC
            LIMIT $limit";

            return await ReadListAsync(query, new { username, limit }, record => 
            {
                var title = record["title"].As<string>();
                var score = record["score"].As<int>();
                return new Movie
                {
                    Title = title,
                    Genre = record["genre"].As<string>(),
                    Released = record["released"].As<int>(),
                    PosterUrl = $"/Posters/{title}.jpg",
                    DisplayInfo = $"{record["released"].As<int>()} • {record["genre"].As<string>()}",
                    RecommendationInfo = $"Popular in your genres {score}"
                };
            }, "Error getting recommended movies by genre");
        }

        public void Dispose()
        {
            Driver?.Dispose();
        }
    }
}