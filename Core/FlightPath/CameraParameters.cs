namespace DroneMesh3D.Core.FlightPath;

public sealed record CameraParameters(
    double SensorWidthMm,
    double FocalLengthMm,
    int ImageWidthPx,
    int ImageHeightPx);
