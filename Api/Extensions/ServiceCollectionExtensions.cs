using System.Text.Json.Serialization;
using DroneMesh3D.Api.Behaviors;
using DroneMesh3D.Api.Middleware;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.Repositories;
using DroneMesh3D.Core.Validation;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddMediatrPipeline(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAreaValidator, AreaValidator>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IFlightPlanRepository, FlightPlanRepository>();
        services.AddTransient<IFlightPathCalculator, FlightPathCalculator>();
        services.AddTransient<GridFlightPathStrategy>();
        services.AddTransient<PoiFlightPathStrategy>();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                x => x.UseNetTopologySuite()));

        return services;
    }

    public static IServiceCollection AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200"];

        services.AddCors(o => o.AddDefaultPolicy(p =>
            p.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader()));

        return services;
    }
}
