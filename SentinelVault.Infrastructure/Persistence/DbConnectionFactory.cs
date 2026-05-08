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

            // 1. Scrub the connection string to remove any 'sslmode' parameters that crash Npgsql
            var scrubbedString = System.Text.RegularExpressions.Regex.Replace(
                connectionString, @"sslmode\s*=[^;&]*[;&]?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd(';', '&', ' ');

            // 2. Initialize the builder
            var builder = new NpgsqlConnectionStringBuilder();
            
            if (scrubbedString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                builder = new NpgsqlConnectionStringBuilder(scrubbedString);
            }
            else
            {
                builder = new NpgsqlConnectionStringBuilder(scrubbedString);
            }

            // 3. Force SslMode to Require (standard for Neon/Render)
            builder.SslMode = SslMode.Require;
            builder.TrustServerCertificate = true;

            return new NpgsqlConnection(builder.ConnectionString);
        }
    }
}
