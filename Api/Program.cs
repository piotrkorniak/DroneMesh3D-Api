using DroneMesh3D.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiInfrastructure()
    .AddMediatrPipeline()
    .AddApplicationServices()
    .AddPersistence(builder.Configuration)
    .AddCorsPolicies(builder.Configuration);

var app = builder.Build();

await app.ConfigurePipelineAsync();
app.MapEndpoints();

app.Run();

// Make Program class accessible for WebApplicationFactory in integration tests
public partial class Program;
