using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using MediatR;

namespace DroneMesh3D.Api.Endpoints;

public static class AreasEndpoint
{
    public static void MapAreasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/areas").WithTags("Areas");

        group.MapPost("/", CreateArea)
            .Produces<AreaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetArea)
            .Produces<AreaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateArea(
        CreateAreaRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateAreaCommand(request.Type, request.Coordinates);
        var result = await mediator.Send(command, ct);

        return result.Match(
            success => Results.Created($"/api/areas/{success.Id}", success),
            validationFailure => Results.UnprocessableEntity(validationFailure),
            badRequest => Results.BadRequest(badRequest)
        );
    }

    private static async Task<IResult> GetArea(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetAreaQuery(id);
        var result = await mediator.Send(query, ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}
