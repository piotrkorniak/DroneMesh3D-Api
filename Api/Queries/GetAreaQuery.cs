using DroneMesh3D.Api.DTOs;
using MediatR;

namespace DroneMesh3D.Api.Queries;

public record GetAreaQuery(Guid Id) : IRequest<AreaResponse?>;
