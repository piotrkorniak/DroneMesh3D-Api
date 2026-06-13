using MediatR;

namespace DroneMesh3D.Api.Commands;

public record DeleteFlightPlanCommand(Guid Id, Guid UserId) : IRequest<bool>;
