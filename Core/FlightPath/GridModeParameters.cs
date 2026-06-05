namespace DroneMesh3D.Core.FlightPath;

public sealed record GridModeParameters(
    double AltitudeM,
    CameraParameters Camera,
    double FrontOverlapPercent,
    double SideOverlapPercent,
    double? HeadingDegrees);
