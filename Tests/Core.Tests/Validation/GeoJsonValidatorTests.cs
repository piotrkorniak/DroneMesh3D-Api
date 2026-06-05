using DroneMesh3D.Core.Models;
using DroneMesh3D.Core.Validation;

namespace DroneMesh3D.Core.Tests.Validation;

public sealed class GeoJsonValidatorTests
{
    private static readonly double[][] ValidRing =
    [
        [21.0122, 52.2297],
        [21.0130, 52.2297],
        [21.0130, 52.2290],
        [21.0122, 52.2290],
        [21.0122, 52.2297]
    ];

    [Fact]
    public void IsValidPolygon_ValidPolygon_ReturnsTrue()
    {
        var coordinates = new[] { ValidRing };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.True(result);
    }

    [Fact]
    public void IsValidPolygon_NullCoordinates_ReturnsFalse()
    {
        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, null);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_EmptyCoordinates_ReturnsFalse()
    {
        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, []);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_NullOuterRing_ReturnsFalse()
    {
        double[][]?[] coordinates = [null];

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_EmptyOuterRing_ReturnsFalse()
    {
        var coordinates = new[] { Array.Empty<double[]>() };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_PointWithLessThanTwoCoordinates_ReturnsFalse()
    {
        double[][] ring = [[21.0]]; // only one coordinate value
        var coordinates = new[] { ring };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_NullPointInRing_ReturnsFalse()
    {
        double[][] ring = [[21.0, 52.0], null!, [21.1, 52.1]];
        double[][]?[] coordinates = [ring];

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_MultipleRingsWithValidCoordinates_ReturnsTrue()
    {
        double[][] outerRing =
        [
            [0.0, 0.0],
            [10.0, 0.0],
            [10.0, 10.0],
            [0.0, 10.0],
            [0.0, 0.0]
        ];
        double[][] innerRing =
        [
            [2.0, 2.0],
            [8.0, 2.0],
            [8.0, 8.0],
            [2.0, 8.0],
            [2.0, 2.0]
        ];
        var coordinates = new[] { outerRing, innerRing };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.True(result);
    }

    [Fact]
    public void IsValidPolygon_SecondRingIsNull_ReturnsFalse()
    {
        double[][]?[] coordinates = [ValidRing, null];

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_SecondRingIsEmpty_ReturnsFalse()
    {
        var coordinates = new[] { ValidRing, Array.Empty<double[]>() };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPolygon_SinglePointRing_ReturnsTrue()
    {
        // Structurally valid (has at least 2 coords per point),
        // geometry validation (min vertices, closure) is AreaValidator's job
        double[][] ring = [[21.0, 52.0]];
        var coordinates = new[] { ring };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.True(result);
    }

    [Fact]
    public void IsValidPolygon_PointWithThreeCoordinates_ReturnsTrue()
    {
        // GeoJSON allows optional altitude (3rd coordinate)
        double[][] ring = [[21.0, 52.0, 100.0], [21.1, 52.1, 200.0], [21.0, 52.0, 100.0]];
        var coordinates = new[] { ring };

        var result = GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

        Assert.True(result);
    }
}
