using DroneMesh3D.Api.DTOs;
using MediatR;
using OneOf;

namespace DroneMesh3D.Api.Commands;

public record CreateAreaCommand(string Type, double[][][] Coordinates)
    : IRequest<OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>>;
