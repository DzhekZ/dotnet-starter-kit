using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.UpdateProfile;

public sealed class UpdateProfileCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<UpdateProfileCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var profile = await dbContext.ProfileItems
            .FirstOrDefaultAsync(p => p.Id == command.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProfileId} not found.");

        if (profile.PositionId != command.PositionId)
        {
            bool brandExists = await dbContext.Positions
                .AnyAsync(b => b.Id == command.PositionId, cancellationToken)
                .ConfigureAwait(false);
            if (!brandExists)
            {
                throw new NotFoundException($"Brand {command.PositionId} not found.");
            }
        }

        if (profile.SubdivisionId != command.SubdivisionId)
        {
            bool categoryExists = await dbContext.Subdivisions
                .AnyAsync(c => c.Id == command.SubdivisionId, cancellationToken)
                .ConfigureAwait(false);
            if (!categoryExists)
            {
                throw new NotFoundException($"Category {command.SubdivisionId} not found.");
            }
        }

        profile.Update(
            command.Name,
            command.PersonnelNumber,
            command.CodePerson,
            command.Email,
            command.Login,
            command.AdSid,
            command.DateBirth,
            command.DateHire,
            command.DateDismiss,
            command.Sex,
            command.IsBoss,
            command.TypeEmployment,
            command.Staffing,
            command.City,
            command.Category,
            command.PhoneMobile,
            command.PhoneMobileAllowShow,
            command.PhoneWork,
            command.Division,
            command.Place,
            command.WtHcmId,
            command.IsDecret,
            command.IsMobilization,
            command.Subordinates,
            command.Information,
            command.Description,
            command.PositionId,
            command.SubdivisionId,
            command.HierarchyId,
            command.IsActive);

        bool slugTaken = await dbContext.ProfileItems
            .AnyAsync(p => p.Slug == profile.Slug && p.PersonnelNumber == profile.PersonnelNumber && p.Id != profile.Id, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"Another profile with name '{command.Name}' ({command.PersonnelNumber}) already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return profile.Id;
    }
}
