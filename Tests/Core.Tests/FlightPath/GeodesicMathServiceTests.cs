using DroneMesh3D.Core.FlightPath;

namespace DroneMesh3D.Core.Tests.FlightPath;

public sealed class GeodesicMathServiceTests
{
    private const double Tolerance = 0.001;

    #region GSD Computation

    [Fact]
    public void ComputeGsd_KnownValues_ReturnsExpectedGsd()
    {
        // altitude=80m, sensorWidth=13.2mm, focalLength=8.8mm, imageWidth=5472px
        // Expected GSD = (80 * 13.2) / (8.8 * 5472) ≈ 0.02193 m/pixel
        var gsd = GeodesicMathService.ComputeGsd(80.0, 13.2, 8.8, 5472);

        var expected = 80.0 * 13.2 / (8.8 * 5472);
        Assert.Equal(expected, gsd, 6);
    }

    [Theory]
    [InlineData(50.0, 13.2, 8.8, 5472)]
    [InlineData(120.0, 13.2, 8.8, 5472)]
    [InlineData(80.0, 6.17, 4.5, 4000)]
    public void ComputeGsd_VariousInputs_MatchesFormula(
        double altitude, double sensorWidth, double focalLength, int imageWidth)
    {
        var gsd = GeodesicMathService.ComputeGsd(altitude, sensorWidth, focalLength, imageWidth);

        var expected = altitude * sensorWidth / (focalLength * imageWidth);
        Assert.Equal(expected, gsd, 10);
    }

    #endregion

    #region Photo Footprint

    [Fact]
    public void ComputePhotoFootprint_KnownGsd_ReturnsExpectedFootprint()
    {
        // GSD ≈ 0.02193, width=5472, height=3648
        // Expected footprint: (0.02193 * 5472, 0.02193 * 3648) ≈ (120m, 80m)
        var gsd = 80.0 * 13.2 / (8.8 * 5472); // ≈ 0.02193

        var (widthM, heightM) = GeodesicMathService.ComputePhotoFootprint(gsd, 5472, 3648);

        Assert.InRange(widthM, 119.0, 121.0);
        Assert.InRange(heightM, 79.0, 81.0);
    }

    [Fact]
    public void ComputePhotoFootprint_ExactComputation_MatchesGsdTimesPixels()
    {
        var gsd = 0.025;

        var (widthM, heightM) = GeodesicMathService.ComputePhotoFootprint(gsd, 4000, 3000);

        Assert.Equal(100.0, widthM, 10);
        Assert.Equal(75.0, heightM, 10);
    }

    #endregion

    #region Photo Spacing

    [Fact]
    public void ComputePhotoSpacing_KnownOverlap_ReturnsExpectedSpacing()
    {
        // footprint height = 80m, frontOverlap = 0.78
        // spacing = 80 * (1 - 0.78) = 80 * 0.22 = 17.6m
        var spacing = GeodesicMathService.ComputePhotoSpacing(80.0, 0.78);

        Assert.Equal(17.6, spacing, 6);
    }

    [Theory]
    [InlineData(80.0, 0.75, 20.0)]
    [InlineData(80.0, 0.80, 16.0)]
    [InlineData(100.0, 0.78, 22.0)]
    public void ComputePhotoSpacing_BoundaryOverlaps_ReturnsExpectedValues(
        double footprintHeight, double frontOverlap, double expectedSpacing)
    {
        var spacing = GeodesicMathService.ComputePhotoSpacing(footprintHeight, frontOverlap);

        Assert.Equal(expectedSpacing, spacing, 6);
    }

    #endregion

    #region Line Spacing

    [Fact]
    public void ComputeLineSpacing_KnownOverlap_ReturnsExpectedSpacing()
    {
        // footprint width = 120m, sideOverlap = 0.70
        // spacing = 120 * (1 - 0.70) = 120 * 0.30 = 36m
        var spacing = GeodesicMathService.ComputeLineSpacing(120.0, 0.70);

        Assert.Equal(36.0, spacing, 6);
    }

    [Theory]
    [InlineData(120.0, 0.65, 42.0)]
    [InlineData(120.0, 0.75, 30.0)]
    [InlineData(100.0, 0.70, 30.0)]
    public void ComputeLineSpacing_BoundaryOverlaps_ReturnsExpectedValues(
        double footprintWidth, double sideOverlap, double expectedSpacing)
    {
        var spacing = GeodesicMathService.ComputeLineSpacing(footprintWidth, sideOverlap);

        Assert.Equal(expectedSpacing, spacing, 6);
    }

    #endregion

    #region DestinationPoint

    [Fact]
    public void DestinationPoint_BearingEast1000m_LongitudeIncreases()
    {
        // Start at (52.0, 21.0), bearing 90° (east), distance 1000m
        var (lat, lon) = GeodesicMathService.DestinationPoint(52.0, 21.0, 90.0, 1000.0);

        // Latitude should remain approximately the same
        Assert.InRange(lat, 51.999, 52.001);
        // Longitude should increase
        Assert.True(lon > 21.0, $"Expected longitude > 21.0, got {lon}");
    }

    [Fact]
    public void DestinationPoint_BearingNorth1000m_LatitudeIncreases()
    {
        var (lat, lon) = GeodesicMathService.DestinationPoint(52.0, 21.0, 0.0, 1000.0);

        Assert.True(lat > 52.0, $"Expected latitude > 52.0, got {lat}");
        Assert.InRange(lon, 20.999, 21.001);
    }

    [Fact]
    public void DestinationPoint_ZeroDistance_ReturnsSamePoint()
    {
        var (lat, lon) = GeodesicMathService.DestinationPoint(52.0, 21.0, 45.0, 0.0);

        Assert.Equal(52.0, lat, 10);
        Assert.Equal(21.0, lon, 10);
    }

    #endregion

    #region DistanceBetween

    [Fact]
    public void DistanceBetween_KnownPoints_ReturnsExpectedDistance()
    {
        // Warsaw city center to a point ~1km north
        // 1 degree lat ≈ 111,320m, so 0.009 degrees ≈ ~1001m
        var distance = GeodesicMathService.DistanceBetween(52.0, 21.0, 52.009, 21.0);

        Assert.InRange(distance, 990.0, 1010.0);
    }

    [Fact]
    public void DistanceBetween_SamePoint_ReturnsZero()
    {
        var distance = GeodesicMathService.DistanceBetween(52.0, 21.0, 52.0, 21.0);

        Assert.Equal(0.0, distance, 10);
    }

    [Fact]
    public void DistanceBetween_SymmetricProperty_SameInBothDirections()
    {
        var d1 = GeodesicMathService.DistanceBetween(52.0, 21.0, 52.01, 21.01);
        var d2 = GeodesicMathService.DistanceBetween(52.01, 21.01, 52.0, 21.0);

        Assert.Equal(d1, d2, 6);
    }

    #endregion

    #region BearingBetween

    [Fact]
    public void BearingBetween_DueEast_ReturnsApproximately90Degrees()
    {
        // Same latitude, increasing longitude = due east
        var bearing = GeodesicMathService.BearingBetween(52.0, 21.0, 52.0, 21.01);

        Assert.InRange(bearing, 89.0, 91.0);
    }

    [Fact]
    public void BearingBetween_DueNorth_ReturnsApproximately0Degrees()
    {
        var bearing = GeodesicMathService.BearingBetween(52.0, 21.0, 52.01, 21.0);

        // Bearing should be close to 0° (or equivalently 360°)
        var normalized = bearing % 360.0;
        Assert.True(normalized < 1.0 || normalized > 359.0,
            $"Expected bearing near 0°, got {bearing}°");
    }

    [Fact]
    public void BearingBetween_DueSouth_ReturnsApproximately180Degrees()
    {
        var bearing = GeodesicMathService.BearingBetween(52.01, 21.0, 52.0, 21.0);

        Assert.InRange(bearing, 179.0, 181.0);
    }

    [Fact]
    public void BearingBetween_DueWest_ReturnsApproximately270Degrees()
    {
        var bearing = GeodesicMathService.BearingBetween(52.0, 21.01, 52.0, 21.0);

        Assert.InRange(bearing, 269.0, 271.0);
    }

    #endregion
}
