using DroneMesh3D.Core.Models;

namespace DroneMesh3D.Api.DTOs;

public record CreateAreaRequest(GeoJsonType Type, double[][][] Coordinates, string? Name = null);
