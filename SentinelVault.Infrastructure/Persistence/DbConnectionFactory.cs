using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace SentinelVault.Infrastructure.Persistence
{
    public class DbConnectionFactory(IConfiguration configuration)
    {
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

            // Normalize connection string to handle both URI and ADO.NET formats
            var builder = new NpgsqlConnectionStringBuilder();
            
            if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                // Parse URI format
                builder = new NpgsqlConnectionStringBuilder(connectionString);
            }
            else
            {
                // Handle standard ADO.NET format, but replace lowercase 'sslmode' which Npgsql dislikes
                var normalizedString = connectionString.Replace("sslmode=", "SslMode=", StringComparison.OrdinalIgnoreCase);
                builder = new NpgsqlConnectionStringBuilder(normalizedString);
            }

            return new NpgsqlConnection(builder.ConnectionString);
        }
    }
}
