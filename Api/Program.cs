using DroneMesh3D.Api.Behaviors;
using DroneMesh3D.Api.Endpoints;
using DroneMesh3D.Api.Middleware;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.Repositories;
using DroneMesh3D.Core.Validation;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI + Scalar
builder.Services.AddOpenApi();

// Global exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Application services
builder.Services.AddScoped<IAreaValidator, AreaValidator>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();

// EF Core with spatial types
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        x => x.UseNetTopologySuite()));

// CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapAreasEndpoints();

app.Run();

// Make Program class accessible for WebApplicationFactory in integration tests
public partial class Program;
