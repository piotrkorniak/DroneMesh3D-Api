using MediatR;

namespace DroneMesh3D.Api.Commands;

public record DeleteAreaCommand(Guid Id, Guid UserId) : IRequest<bool>;
