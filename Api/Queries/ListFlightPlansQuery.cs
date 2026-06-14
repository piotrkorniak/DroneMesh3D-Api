using DroneMesh3D.Api.DTOs;
using MediatR;

namespace DroneMesh3D.Api.Queries;

public record ListFlightPlansQuery(Guid? AreaId, int Limit = 100, int Offset = 0) : IRequest<List<FlightPlanResponse>>;
