using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace SentinelVault.Infrastructure.Persistence
{
    public class DbConnectionFactory (IConfiguration configuration)
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found.");

        public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
    }
}
