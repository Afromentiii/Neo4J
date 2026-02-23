using Neo4j.Driver;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.System;

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

        public async Task<List<Movie>> GetMovies()
        {
            string query = @"MATCH (m:Movie) 
                                 RETURN m.title AS title, 
                                        m.genre AS genre, 
                                        m.released AS released,
                                        m.poster AS poster";

            var movies = new List<Movie>();
            await using var session = Driver.AsyncSession();

            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query);
                    return await cursor.ToListAsync();
                });

                foreach (var record in result)
                {
                    var title = record["title"].As<string>();
                    movies.Add(new Movie
                    {
                        Title = record["title"].As<string>(),
                        Genre = record["genre"].As<string>(),
                        Released = record["released"].As<int>(),
                        PosterUrl = $"/Posters/{title}.jpg",
                        DisplayInfo = $"{record["released"].As<int>()} • {record["genre"].As<string>()}"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during downloading movies!: {ex.Message}");
            }

            return movies;
        }
        public async Task <bool> CreateLikeMovieRelation(string username, string movieTitle)
        {
            var query = @"
            MATCH (u:User { username: $username }), 
                  (m:Movie { title: $movieTitle })
            MERGE (u)-[:LIKED]->(m)
            RETURN u, m";
            await using var session = Driver.AsyncSession();

            var result = await session.RunAsync(query, new { username, movieTitle });
            var record = await result.SingleOrDefaultAsync();

            if (record != null)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> CreateObservesRelation(string currentUserUsername, string targetUsername)
        {
            var query = @"
            MATCH (u:User { username: $currentUserUsername }), 
                  (t:User { username: $targetUsername })
            MERGE (u)-[:OBSERVES]->(t)
            RETURN u, t";

            await using var session = Driver.AsyncSession();

            var result = await session.RunAsync(query, new { currentUserUsername, targetUsername });
            var record = await result.SingleOrDefaultAsync();

            if (record != null)
            {
                return true;
            }
            return false;
        }

        public async Task<HashSet<string>> GetLikedMovieTitles(string username)
        {
            var likedTitles = new HashSet<string>();
            var query = @"
            MATCH (u:User {username: $username})-[:LIKED]->(m:Movie)
            RETURN m.title AS Title";

            await using var session = Driver.AsyncSession();
            var result = await session.RunAsync(query, new { username });

            await result.ForEachAsync(record =>
            {
                likedTitles.Add(record["Title"].As<string>());
            });

            return likedTitles;
        }

        public async Task<bool> RemoveLikeMovieRelation(string username, string movieTitle)
        {
           var query = @"MATCH (u:User { username: $username })-[r:LIKED]->(m:Movie { title: $movieTitle })
                        DELETE r
                        RETURN count(r) AS deletedCount";

            await using var session = Driver.AsyncSession();

            try
            {
                var result = await session.RunAsync(query, new { username, movieTitle });
                var record = await result.SingleOrDefaultAsync();

                if (record != null && record["deletedCount"].As<int>() > 0)
                {
                    //MessageBox.Show(record["deletedCount"].ToString());
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during removing liked relation: {ex.Message}");
            }

            return false;
        }

        public async Task<List<object>> GetUsers(string currentUser)
        {
            string query = @"
                    MATCH (u:User)
                    WHERE u.username <> $currentUser
                    RETURN u.username AS username";

            var usernames = new List<object>();
            await using var session = Driver.AsyncSession();

            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, new { currentUser });
                    return await cursor.ToListAsync();
                });

                foreach (var record in result)
                {
                    usernames.Add(new
                    {
                        Username = record["username"].As<string>()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during downloading users: {ex.Message}");
            }

            return usernames;
        }
        public async Task<HashSet<string>> GetFollowedUsers(string username)
        {
            var followedUsers = new HashSet<string>();
            var query = @"
            MATCH (u:User {username: $username})-[:OBSERVES]->(f:User)
            RETURN f.username AS FollowedUsername";

            await using var session = Driver.AsyncSession();
            var result = await session.RunAsync(query, new { username });

            await result.ForEachAsync(record =>
            {
                followedUsers.Add(record["FollowedUsername"].As<string>());
            });

            return followedUsers;
        }
        public async Task<bool> RemoveObservesRelation(string followerUsername, string followedUsername)
        {
            var query = @"
            MATCH (follower:User { username: $followerUsername })-[r:OBSERVES]->(followed:User { username: $followedUsername })
            DELETE r
            RETURN count(r) AS deletedCount";

            await using var session = Driver.AsyncSession();

            try
            {
                var result = await session.RunAsync(query, new { followerUsername, followedUsername });
                var record = await result.SingleOrDefaultAsync();

                if (record != null && record["deletedCount"].As<int>() > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during removing OBSERVES relation: {ex.Message}");
            }

            return false;
        }

        public async Task<List<Movie>> RecommendMoviesForUser(string username, int limit = 10)
        {
            var movies = new List<Movie>();

            var query = @"
            MATCH (u:User {username: $username})-[:OBSERVES]->(f:User)-[:LIKED]->(m:Movie)
            WHERE NOT (u)-[:LIKED]->(m)
            RETURN m.title AS title, m.genre AS genre, m.released AS released, m.poster AS poster,
                   count(*) AS score
            ORDER BY score DESC
            LIMIT $limit";

            await using var session = Driver.AsyncSession();

            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, new { username, limit });
                return await cursor.ToListAsync();
            });

            foreach (var record in result)
            {
                var title = record["title"].As<string>();
                var score = record["score"].As<int>();
                movies.Add(new Movie
                {
                    Title = record["title"].As<string>(),
                    Genre = record["genre"].As<string>(),
                    Released = record["released"].As<int>(),
                    PosterUrl = $"/Posters/{title}.jpg",
                    DisplayInfo = $"{record["released"].As<int>()} • {record["genre"].As<string>()}",
                    RecommendationInfo = $"Recommended by {score} people"
                });
            }
            return movies;
        }

        public async Task<List<Movie>> RecommendMoviesByGenre(string username, int limit = 10)
        {
            var movies = new List<Movie>();

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

            await using var session = Driver.AsyncSession();

            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, new { username, limit });
                return await cursor.ToListAsync();
            });

            foreach (var record in result)
            {
                var title = record["title"].As<string>();
                var score = record["score"].As<int>();

                movies.Add(new Movie
                {
                    Title = title,
                    Genre = record["genre"].As<string>(),
                    Released = record["released"].As<int>(),
                    PosterUrl = $"/Posters/{title}.jpg",
                    DisplayInfo = $"{record["released"].As<int>()} • {record["genre"].As<string>()}",
                    RecommendationInfo = $"Popular in your genres {score}"
                });
            }

            return movies;
        }
        public void Dispose()
        {
            Driver?.Dispose();
        }
    }
}