namespace DroneMesh3D.Api.Services;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
}
