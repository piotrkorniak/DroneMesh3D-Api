using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.Services;
using DroneMesh3D.Core.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Api.Handlers;

public sealed class DeleteAreaCommandHandler(AppDbContext context, ICurrentUserAccessor currentUser)
    : IRequestHandler<DeleteAreaCommand, bool>
{
    public async Task<bool> Handle(DeleteAreaCommand command, CancellationToken ct)
    {
        var deleted = await context.Areas
            .Where(a => a.Id == command.Id && a.UserId == currentUser.UserId)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
