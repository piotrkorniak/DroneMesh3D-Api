using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Api.Services;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.MissionExport;
using MediatR;
using OneOf;

namespace DroneMesh3D.Api.Handlers;

public sealed class ExportMissionFileQueryHandler(
    IFlightPlanRepository flightPlanRepository,
    IMissionFileGeneratorFactory generatorFactory,
    ICurrentUserAccessor currentUser,
    ILogger<ExportMissionFileQueryHandler> logger)
    : IRequestHandler<ExportMissionFileQuery, OneOf<MissionFileResult, ValidationErrorResponse, ErrorResponse>>
{
    public async Task<OneOf<MissionFileResult, ValidationErrorResponse, ErrorResponse>> Handle(
        ExportMissionFileQuery query,
        CancellationToken ct)
    {
        // 1. Load FlightPlanEntity by ID
        var entity = await flightPlanRepository.GetByIdAsync(query.FlightPlanId, currentUser.UserId, ct);
        if (entity is null)
        {
            logger.LogError(
                "Flight plan with ID '{FlightPlanId}' was not found",
                query.FlightPlanId);
            return new ErrorResponse($"Flight plan with ID '{query.FlightPlanId}' was not found.");
        }

        // 2. Validate waypoints list is non-empty
        if (entity.Waypoints.Count == 0)
        {
            return new ValidationErrorResponse(["Flight plan contains no waypoints to export."]);
        }

        // 3. Delegate to the appropriate generator via factory
        var generator = generatorFactory.GetGenerator(query.Format);
        var fileData = generator.Generate(query.FlightPlanId, entity.Waypoints);

        // 4. Return MissionFileResult
        return new MissionFileResult(fileData.Content, fileData.ContentType, fileData.FileName);
    }
}
