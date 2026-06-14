namespace DroneMesh3D.Core.Interfaces;

public interface IObjectStorageService
{
    Task<string> GeneratePresignedUploadUrlAsync(string objectKey, int expirySeconds = 900);
    Task<bool> ObjectExistsAsync(string objectKey);
    Task<long> GetObjectSizeAsync(string objectKey);
    Task EnsureBucketExistsAsync();
}
