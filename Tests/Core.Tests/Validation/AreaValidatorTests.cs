using DroneMesh3D.Core.Validation;

namespace DroneMesh3D.Core.Tests.Validation;

public sealed class AreaValidatorTests
{
    // A valid closed polygon in Warsaw area (~200m x 200m, well within limits)
    private static readonly double[][] ValidPolygon =
    [
        [21.0000, 52.0000],
        [21.0020, 52.0000],
        [21.0020, 52.0018],
        [21.0000, 52.0018],
        [21.0000, 52.0000]
    ];

    private readonly AreaValidator _sut = new();

    [Fact]
    public void Validate_ValidPolygon_ReturnsIsValidTrue()
    {
        var result = _sut.Validate(ValidPolygon);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_LessThanThreeVertices_ReturnsAtLeast3VerticesError()
    {
        // Two distinct vertices + closing point = only 2 distinct vertices
        double[][] ring =
        [
            [21.0, 52.0],
            [21.1, 52.1],
            [21.0, 52.0]
        ];

        var result = _sut.Validate(ring);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least 3 vertices"));
    }

    [Fact]
    public void Validate_UnclosedPolygon_ReturnsClosedError()
    {
        // Polygon where first != last
        double[][] ring =
        [
            [21.0000, 52.0000],
            [21.0020, 52.0000],
            [21.0020, 52.0018],
            [21.0000, 52.0018]
        ];

        var result = _sut.Validate(ring);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("closed"));
    }

    [Fact]
    public void Validate_SelfIntersectingPolygon_ReturnsSelfIntersectError()
    {
        // Bowtie shape: edges cross each other
        double[][] ring =
        [
            [0.0, 0.0],
            [1.0, 1.0],
            [1.0, 0.0],
            [0.0, 1.0],
            [0.0, 0.0]
        ];

        var result = _sut.Validate(ring);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("self-intersect"));
    }

    [Fact]
    public void Validate_AreaTooLarge_ReturnsMaximumError()
    {
        // Very large polygon (>50000 m²): roughly 1km x 1km
        double[][] ring =
        [
            [20.0, 52.0],
            [20.1, 52.0],
            [20.1, 52.1],
            [20.0, 52.1],
            [20.0, 52.0]
        ];

        var result = _sut.Validate(ring);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("maximum"));
    }

    [Fact]
    public void Validate_AreaTooSmall_ReturnsMinimumError()
    {
        // Tiny polygon (~1 m²): vertices very close together
        double[][] ring =
        [
            [21.000000, 52.000000],
            [21.000001, 52.000000],
            [21.000001, 52.000001],
            [21.000000, 52.000001],
            [21.000000, 52.000000]
        ];

        var result = _sut.Validate(ring);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("minimum"));
    }
}
