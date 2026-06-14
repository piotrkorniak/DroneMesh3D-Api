using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Api.Handlers;

public sealed class UpsertGoogleUserCommandHandler(AppDbContext db)
    : IRequestHandler<Commands.UpsertGoogleUserCommand, UserEntity>
{
    public async Task<UserEntity> Handle(Commands.UpsertGoogleUserCommand command, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == command.GoogleId, ct);

        if (user is null)
        {
            user = new UserEntity
            {
                Id = Guid.CreateVersion7(),
                GoogleId = command.GoogleId,
                Email = command.Email,
                Name = command.Name,
                AvatarUrl = command.AvatarUrl,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
        }
        else
        {
            user.Email = command.Email;
            user.Name = command.Name;
            user.AvatarUrl = command.AvatarUrl;
        }

        await db.SaveChangesAsync(ct);
        return user;
    }
}
