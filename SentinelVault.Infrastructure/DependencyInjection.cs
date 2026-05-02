using Microsoft.Extensions.DependencyInjection;
using SentinelVault.Application.Interfaces;
using SentinelVault.Infrastructure.Persistence;
using SentinelVault.Infrastructure.Repositories;
using SentinelVault.Infrastructure.Services;

namespace SentinelVault.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<DbConnectionFactory>();

            // Document services
            services.AddScoped<IDocumentRepository, DocumentReposiotry>();
            services.AddScoped<IDocumentService, DocumentService>();

            // Authentication services
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            return services;
        }
    }
}
