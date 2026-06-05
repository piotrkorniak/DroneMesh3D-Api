using DroneMesh3D.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneMesh3D.Api.Tests.Integration;

/// <summary>
///     Shared WebApplicationFactory for all integration tests.
///     Initialized once per test run via xUnit collection fixture.
/// </summary>
public sealed class DroneMesh3DApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Ensure a clean database at the start of the test run
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType == typeof(AppDbContext)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                    || d.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var d in descriptorsToRemove)
            {
                services.Remove(d);
            }

            // Remove Npgsql provider services
            var efInternalDescriptors = services
                .Where(d =>
                    d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                    || d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                .ToList();

            foreach (var d in efInternalDescriptors)
            {
                services.Remove(d);
            }

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                                   ?? "Host=localhost;Database=dronemesh3d_test;Username=postgres;Password=YourStr0ngP@ssword";

            services.AddDbContext<AppDbContext>(options => { options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()); });
        });

        builder.UseEnvironment("Testing");
    }
}
