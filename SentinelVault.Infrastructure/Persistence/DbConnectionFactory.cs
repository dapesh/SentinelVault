using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace SentinelVault.Infrastructure.Persistence
{
    public class DbConnectionFactory(IConfiguration configuration)
    {
        public IDbConnection CreateConnection()
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration["DATABASE_URL"] // Support Render/Fly.io default env var
                ?? throw new InvalidOperationException("Connection string not found.");

            // If it's a postgresql:// URI, we need to convert it or use Npgsql's URI parser
            if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                return new NpgsqlConnection(builder.ConnectionString);
            }

            return new NpgsqlConnection(connectionString);
        }
    }
}
