using DroneMesh3D.Core.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI + Scalar
builder.Services.AddOpenApi();

// MediatR
builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(Program).Assembly); });

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// EF Core with spatial types
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        x => x.UseNetTopologySuite()));

// CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();
