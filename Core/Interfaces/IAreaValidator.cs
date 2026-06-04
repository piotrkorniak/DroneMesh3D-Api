namespace DroneMesh3D.Core.Interfaces;

public interface IAreaValidator
{
    ValidationResult Validate(double[][] ring);
    bool HasMinimumVertices(double[][] ring);
    bool IsClosed(double[][] ring);
    bool HasSelfIntersection(double[][] ring);
    double CalculateAreaSqm(double[][] ring);
}

public record ValidationResult(bool IsValid, List<string> Errors);
