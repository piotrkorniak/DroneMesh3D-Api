using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.Services;
using DroneMesh3D.Core.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Api.Handlers;

public sealed class DeleteFlightPlanCommandHandler(AppDbContext context, ICurrentUserAccessor currentUser)
    : IRequestHandler<DeleteFlightPlanCommand, bool>
{
    public async Task<bool> Handle(DeleteFlightPlanCommand command, CancellationToken ct)
    {
        var deleted = await context.FlightPlans
            .Where(fp => fp.Id == command.Id && fp.Area.UserId == currentUser.UserId)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
