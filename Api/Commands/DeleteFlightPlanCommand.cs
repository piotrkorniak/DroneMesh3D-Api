using MediatR;

namespace DroneMesh3D.Api.Commands;

public record DeleteFlightPlanCommand(Guid Id) : IRequest<bool>;
