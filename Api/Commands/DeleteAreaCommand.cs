using MediatR;

namespace DroneMesh3D.Api.Commands;

public record DeleteAreaCommand(Guid Id) : IRequest<bool>;
