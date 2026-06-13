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

        group.MapGet("/", ListAreas)
            .Produces<List<AreaResponse>>();

        group.MapPost("/", CreateArea)
            .Produces<AreaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetArea)
            .Produces<AreaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteArea)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}", UpdateAreaName)
            .Produces<AreaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListAreas(
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ListAreasQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateArea(
        CreateAreaRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Type))
        {
            return Results.BadRequest(new ErrorResponse("Invalid GeoJSON geometry type."));
        }

        var command = new CreateAreaCommand(request.Type, request.Coordinates, request.Name);
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

    private static async Task<IResult> DeleteArea(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteAreaCommand(id), ct);
        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> UpdateAreaName(
        Guid id,
        UpdateAreaNameRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateAreaNameCommand(id, request.Name), ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}
