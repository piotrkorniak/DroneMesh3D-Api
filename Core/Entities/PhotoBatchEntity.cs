namespace DroneMesh3D.Core.Entities;

public sealed class PhotoBatchEntity
{
    public Guid Id { get; set; }
    public Guid FlightPlanId { get; set; }
    public Guid UserId { get; set; }
    public int PhotoCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public required string BucketPath { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public FlightPlanEntity FlightPlan { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}
