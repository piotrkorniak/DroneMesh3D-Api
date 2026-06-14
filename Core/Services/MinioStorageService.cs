using DroneMesh3D.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace DroneMesh3D.Core.Services;

public sealed class MinioStorageService : IObjectStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;

    public MinioStorageService(IConfiguration configuration)
    {
        var section = configuration.GetSection("MinIO");
        var endpoint = section["Endpoint"] ?? "localhost:9000";
        var accessKey = section["AccessKey"] ?? "minioadmin";
        var secretKey = section["SecretKey"] ?? "minioadmin";
        var useSsl = section.GetValue<bool>("UseSSL");
        _bucketName = section["BucketName"] ?? "dronemesh-photos";

        var builder = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey);

        if (useSsl)
        {
            builder = builder.WithSSL();
        }

        _client = builder.Build();
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string objectKey, int expirySeconds = 900)
    {
        var args = new PresignedPutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithExpiry(expirySeconds);

        return await _client.PresignedPutObjectAsync(args);
    }

    public async Task<bool> ObjectExistsAsync(string objectKey)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<long> GetObjectSizeAsync(string objectKey)
    {
        var stat = await _client.StatObjectAsync(new StatObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey));
        return stat.Size;
    }

    public async Task EnsureBucketExistsAsync()
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }
    }
}
