namespace DroneMesh3D.Core.FlightPath;

public sealed record PoiModeParameters(
    double CenterLatitude,
    double CenterLongitude,
    double RadiusM,
    double AltitudeM,
    double GimbalPitchDegrees,
    int? PhotoCount,
    double? OverlapPercent,
    double? CameraHorizontalFovDegrees,
    double? StructureHeightM);
