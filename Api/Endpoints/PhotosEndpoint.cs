using DroneMesh3D.Api.Services;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Api.Endpoints;

public static class PhotosEndpoint
{
    public static void MapPhotosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/photos").WithTags("Photos").RequireAuthorization();

        group.MapPost("/upload-urls", GenerateUploadUrls)
            .Produces<UploadUrlsResponse>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/confirm", ConfirmUpload)
            .Produces<ConfirmUploadResponse>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> GenerateUploadUrls(
        UploadUrlsRequest request,
        IObjectStorageService storage,
        ICurrentUserAccessor user,
        AppDbContext db,
        CancellationToken ct)
    {
        if (request.FileNames is not { Count: > 0 and <= 500 })
        {
            return Results.UnprocessableEntity(new { error = "FileNames must contain 1-500 items." });
        }

        var flightPlanExists = await db.FlightPlans.AnyAsync(
            fp => fp.Id == request.FlightPlanId, ct);

        if (!flightPlanExists)
        {
            return Results.UnprocessableEntity(new { error = "FlightPlan not found." });
        }

        await storage.EnsureBucketExistsAsync();

        var jobId = Guid.NewGuid();
        var userId = user.UserId;
        var urls = new List<UploadUrlItem>(request.FileNames.Count);

        foreach (var fileName in request.FileNames)
        {
            var objectKey = $"users/{userId}/jobs/{jobId}/{fileName}";
            var uploadUrl = await storage.GeneratePresignedUploadUrlAsync(objectKey);
            urls.Add(new UploadUrlItem(fileName, uploadUrl, objectKey));
        }

        return Results.Ok(new UploadUrlsResponse(urls));
    }

    private static async Task<IResult> ConfirmUpload(
        ConfirmUploadRequest request,
        IObjectStorageService storage,
        ICurrentUserAccessor user,
        AppDbContext db,
        CancellationToken ct)
    {
        if (request.ObjectKeys is not { Count: > 0 and <= 500 })
        {
            return Results.UnprocessableEntity(new { error = "ObjectKeys must contain 1-500 items." });
        }

        var flightPlanExists = await db.FlightPlans.AnyAsync(
            fp => fp.Id == request.FlightPlanId, ct);

        if (!flightPlanExists)
        {
            return Results.UnprocessableEntity(new { error = "FlightPlan not found." });
        }

        long totalSize = 0;
        foreach (var key in request.ObjectKeys)
        {
            if (!await storage.ObjectExistsAsync(key))
            {
                return Results.UnprocessableEntity(new { error = $"Object not found: {key}" });
            }

            totalSize += await storage.GetObjectSizeAsync(key);
        }

        var userId = user.UserId;
        var bucketPath = $"users/{userId}/jobs/{request.ObjectKeys[0].Split('/')[3]}/";

        var batch = new PhotoBatchEntity
        {
            FlightPlanId = request.FlightPlanId,
            UserId = userId,
            PhotoCount = request.ObjectKeys.Count,
            TotalSizeBytes = totalSize,
            BucketPath = bucketPath,
        };

        db.PhotoBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new ConfirmUploadResponse(batch.Id, batch.PhotoCount, batch.TotalSizeBytes));
    }
}

public sealed record UploadUrlsRequest(Guid FlightPlanId, List<string> FileNames);
public sealed record UploadUrlItem(string FileName, string UploadUrl, string ObjectKey);
public sealed record UploadUrlsResponse(List<UploadUrlItem> Urls);
public sealed record ConfirmUploadRequest(Guid FlightPlanId, List<string> ObjectKeys);
public sealed record ConfirmUploadResponse(Guid BatchId, int PhotoCount, long TotalSizeBytes);
