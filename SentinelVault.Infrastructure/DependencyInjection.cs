using Microsoft.Extensions.DependencyInjection;
using SentinelVault.Application.Interfaces;
using SentinelVault.Infrastructure.Persistence;
using SentinelVault.Infrastructure.Repositories;

namespace SentinelVault.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<DbConnectionFactory>();
            services.AddScoped<IDocumentRepository, DocumentReposiotry>();
            return services;
        }
    }
}
