using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core.FlightPath;
using MediatR;

namespace DroneMesh3D.Api.Endpoints;

public static class FlightPlansEndpoint
{
    public static void MapFlightPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/flight-plans").WithTags("FlightPlans");

        group.MapPost("/", CalculateFlightPath)
            .Produces<FlightPlanResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", GetFlightPlan)
            .Produces<FlightPlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CalculateFlightPath(
        CalculateFlightPathRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var gridParameters = request.Grid is not null
            ? new GridModeParameters(
                request.Grid.AltitudeM,
                new CameraParameters(
                    request.Grid.Camera.SensorWidthMm,
                    request.Grid.Camera.FocalLengthMm,
                    request.Grid.Camera.ImageWidthPx,
                    request.Grid.Camera.ImageHeightPx),
                request.Grid.FrontOverlapPercent,
                request.Grid.SideOverlapPercent,
                request.Grid.HeadingDegrees)
            : null;

        var poiParameters = request.Poi is not null
            ? new PoiModeParameters(
                request.Poi.CenterLatitude,
                request.Poi.CenterLongitude,
                request.Poi.RadiusM,
                request.Poi.AltitudeM,
                request.Poi.GimbalPitchDegrees,
                request.Poi.PhotoCount,
                request.Poi.OverlapPercent,
                request.Poi.CameraHorizontalFovDegrees,
                request.Poi.StructureHeightM)
            : null;

        var command = new CalculateFlightPathCommand(
            request.AreaId,
            request.Mode,
            gridParameters,
            poiParameters);

        var result = await mediator.Send(command, ct);

        return result.Match(
            response => Results.Created($"/api/flight-plans/{response.Id}", response),
            validationError => Results.UnprocessableEntity(validationError),
            error => error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.NotFound()
                : Results.Problem(statusCode: 500, detail: error.Message)
        );
    }

    private static async Task<IResult> GetFlightPlan(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetFlightPlanQuery(id);
        var result = await mediator.Send(query, ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}
