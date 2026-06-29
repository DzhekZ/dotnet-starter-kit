using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.GetPositionById;

public sealed class GetPositionByIdQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<GetPositionByIdQuery, PositionDto>
{
    public async ValueTask<PositionDto> Handle(GetPositionByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var position = await dbContext.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == query.PositionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Position {query.PositionId} not found.");

        return new PositionDto(
            position.Id,
            position.Name,
            position.Slug,
            position.Description,
            position.CreatedAtUtc,
            position.UpdatedAtUtc,
            position.DeletedOnUtc,
            position.DeletedBy);
    }
}
