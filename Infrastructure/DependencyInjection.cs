using Application.Common.Abstractions;
using Domain.Entities;
using Infrastructure.ExternalServices;
using Infrastructure.ExternalServices.JsonPlaceholder;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, bool enableSensitiveDataLogging = false)
    {
        services.AddDbContext<SchoolDbContext>(options => options
            .UseSqlite(connectionString)
            .EnableSensitiveDataLogging(enableSensitiveDataLogging));
        services.Scan(scan => scan.FromAssemblyOf<SchoolDbContext>()
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal)), publicOnly: false)
            .AsMatchingInterface()
            .WithScopedLifetime());
        services.AddScoped<IUnitOfWork, SchoolUnitOfWork>();
        services.AddHealthChecks().AddDbContextCheck<SchoolDbContext>("database",
            customTestQuery: async (db, cancellationToken) =>
            {
                await db.Set<Course>().AnyAsync(cancellationToken);
                return true;
            });

        // External services: one shared timeout for all clients.
        // Not added to /health: an external outage does not mean this application is unhealthy.
        services.AddOptions<ExternalServicesOptions>().BindConfiguration(ExternalServicesOptions.SectionName);
        services.AddOptions<JsonPlaceholderOptions>().BindConfiguration(JsonPlaceholderOptions.SectionName);
        services.AddHttpClient<IJsonPlaceholderClient, JsonPlaceholderClient>((provider, client) =>
        {
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<JsonPlaceholderOptions>>().Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(
                provider.GetRequiredService<IOptions<ExternalServicesOptions>>().Value.TimeoutSeconds);
        });

        return services;
    }

    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
