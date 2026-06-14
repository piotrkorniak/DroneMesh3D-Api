using DroneMesh3D.Core.Entities;
using MediatR;

namespace DroneMesh3D.Api.Commands;

public record UpsertGoogleUserCommand(
    string GoogleId,
    string Email,
    string? Name,
    string? AvatarUrl) : IRequest<UserEntity>;
