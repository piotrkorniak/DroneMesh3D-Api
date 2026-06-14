using System.Security.Claims;

namespace DroneMesh3D.Api.Services;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor ctx) : ICurrentUserAccessor
{
    public Guid UserId => Guid.Parse(ctx.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
