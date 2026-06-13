using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.MissionExport;
using MediatR;

namespace DroneMesh3D.Api.Endpoints;

public static class FlightPlansEndpoint
{
    public static void MapFlightPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/flight-plans").WithTags("FlightPlans");

        group.MapGet("/", ListFlightPlans)
            .Produces<List<FlightPlanResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/", CalculateFlightPath)
            .Produces<FlightPlanResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", GetFlightPlan)
            .Produces<FlightPlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/export", ExportMissionFile)
            .WithSummary("Export mission file")
            .WithDescription("Downloads the flight plan as a mission file. Supported formats: LitchiCsv, Kml, DjiWpml.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id:guid}", DeleteFlightPlan)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListFlightPlans(
        string? areaId,
        int? limit,
        int? offset,
        ISender sender,
        CancellationToken ct)
    {
        Guid? parsedAreaId = null;
        if (areaId is not null)
        {
            if (!Guid.TryParse(areaId, out var guid))
            {
                return Results.UnprocessableEntity(new ValidationErrorResponse(
                    ["areaId must be a valid GUID."]));
            }

            parsedAreaId = guid;
        }

        var clampedLimit = Math.Clamp(limit ?? 100, 1, 100);
        var clampedOffset = Math.Max(offset ?? 0, 0);

        var query = new ListFlightPlansQuery(parsedAreaId, clampedLimit, clampedOffset);
        var result = await sender.Send(query, ct);

        return Results.Ok(result);
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

        OrbitShape? orbitShape = request.Poi?.OrbitShape;

        var command = new CalculateFlightPathCommand(
            request.AreaId,
            request.Mode,
            gridParameters,
            poiParameters,
            orbitShape,
            request.Poi?.AreaCoordinates);

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

    private static async Task<IResult> ExportMissionFile(
        Guid id,
        ExportFormat? format,
        IMediator mediator,
        CancellationToken ct)
    {
        if (format is null)
        {
            var errors = new List<string>
            {
                "Format must be one of: LitchiCsv, Kml, DjiWpml"
            };
            return Results.UnprocessableEntity(new ValidationErrorResponse(errors));
        }

        var query = new ExportMissionFileQuery(id, format.Value);
        var result = await mediator.Send(query, ct);

        return result.Match(
            missionFile => Results.File(
                missionFile.Content,
                missionFile.ContentType,
                missionFile.FileName),
            validationError => Results.UnprocessableEntity(validationError),
            error => error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.NotFound()
                : Results.Problem(statusCode: 500, detail: error.Message)
        );
    }

    private static async Task<IResult> DeleteFlightPlan(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteFlightPlanCommand(id);
        var deleted = await sender.Send(command, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
