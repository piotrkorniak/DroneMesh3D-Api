namespace DroneMesh3D.Api.DTOs;

public sealed record GridModeParametersDto(
    double AltitudeM,
    CameraParametersDto Camera,
    double FrontOverlapPercent,
    double SideOverlapPercent,
    double? HeadingDegrees);

public sealed record CameraParametersDto(
    double SensorWidthMm,
    double FocalLengthMm,
    int ImageWidthPx,
    int ImageHeightPx);
