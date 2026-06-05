namespace DroneMesh3D.Api.DTOs;

public sealed record PoiModeParametersDto(
    double CenterLatitude,
    double CenterLongitude,
    double RadiusM,
    double AltitudeM,
    double GimbalPitchDegrees,
    int? PhotoCount,
    double? OverlapPercent,
    double? CameraHorizontalFovDegrees,
    double? StructureHeightM);
