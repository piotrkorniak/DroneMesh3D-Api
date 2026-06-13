using DroneMesh3D.Api.DTOs;
using MediatR;

namespace DroneMesh3D.Api.Commands;

public record UpdateAreaNameCommand(Guid Id, Guid UserId, string? Name) : IRequest<AreaResponse?>;
