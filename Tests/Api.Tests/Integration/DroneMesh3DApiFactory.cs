using System.Security.Claims;
using System.Text.Encodings.Web;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DroneMesh3D.Api.Tests.Integration;

public sealed class DroneMesh3DApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        db.Users.Add(new UserEntity
        {
            Id = TestUserId,
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
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

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()));

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
            });

            services.PostConfigure<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>(
                Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme,
                o =>
                {
                    o.ClientId = "test-client-id";
                    o.ClientSecret = "test-client-secret";
                });
        });

        builder.UseEnvironment("Testing");
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DroneMesh3DApiFactory.TestUserId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim("avatar_url", "")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
