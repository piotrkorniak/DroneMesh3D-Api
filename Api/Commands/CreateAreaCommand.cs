using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Models;
using MediatR;
using OneOf;

namespace DroneMesh3D.Api.Commands;

public record CreateAreaCommand(GeoJsonType Type, double[][][] Coordinates, string? Name = null)
    : IRequest<OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>>;
