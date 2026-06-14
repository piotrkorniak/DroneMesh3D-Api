using DroneMesh3D.Api.Endpoints;
using DroneMesh3D.Core.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace DroneMesh3D.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        app.UseExceptionHandler();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapAreasEndpoints();
        app.MapFlightPlansEndpoints();
        app.MapPhotosEndpoints();

        return app;
    }
}
