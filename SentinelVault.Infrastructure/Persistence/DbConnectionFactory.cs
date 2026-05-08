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
                // Handle standard ADO.NET format
                // Aggressively replace 'sslmode' with 'SslMode' regardless of spaces to satisfy Npgsql
                var normalizedString = connectionString;
                if (normalizedString.Contains("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedString = System.Text.RegularExpressions.Regex.Replace(
                        normalizedString, @"sslmode\s*=", "SslMode=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                builder = new NpgsqlConnectionStringBuilder(normalizedString);
            }

            return new NpgsqlConnection(builder.ConnectionString);
        }
    }
}
