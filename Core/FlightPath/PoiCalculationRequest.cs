namespace DroneMesh3D.Core.FlightPath;

/// <summary>
/// Full request data for POI flight path calculation.
/// Contains persisted parameters plus transient calculation inputs (orbit shape, area geometry).
/// </summary>
public sealed record PoiCalculationRequest(
    PoiModeParameters Parameters,
    OrbitShape OrbitShape = OrbitShape.Circular,
    double[][]? AreaCoordinates = null);
