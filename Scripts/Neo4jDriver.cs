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

        public void Dispose()
        {
            Driver?.Dispose();
        }
    }
}