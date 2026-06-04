namespace DroneMesh3D.Api.DTOs;

public record ValidationErrorResponse(List<string> Errors)
{
    public string Message { get; init; } = "Validation failed.";
}
