using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.MissionExport;
using MediatR;
using OneOf;

namespace DroneMesh3D.Api.Queries;

public sealed record ExportMissionFileQuery(Guid FlightPlanId, ExportFormat Format, Guid UserId)
    : IRequest<OneOf<MissionFileResult, ValidationErrorResponse, ErrorResponse>>;
