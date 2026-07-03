using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using FSH.Modules.Profile.Data;
using FSH.Modules.Profile.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.CreateProfile;

public sealed class CreateProfileCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<CreateProfileCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProfileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool positionExists = await dbContext.Positions
            .AnyAsync(b => b.Id == command.PositionId, cancellationToken)
            .ConfigureAwait(false);
        if (!positionExists)
        {
            throw new NotFoundException($"Position {command.PositionId} not found.");
        }

        bool subdivisionExists = await dbContext.Subdivisions
            .AnyAsync(c => c.Id == command.SubdivisionId, cancellationToken)
            .ConfigureAwait(false);
        if (!subdivisionExists)
        {
            throw new NotFoundException($"Subdivision {command.SubdivisionId} not found.");
        }

        var profile = ProfileItem.Create(
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
            command.HierarchyId);

        bool slugTaken = await dbContext.ProfileItems
            .AnyAsync(p => p.Slug == profile.Slug && p.PersonnelNumber == profile.PersonnelNumber, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A profile with name '{command.Name}' ({command.PersonnelNumber}) already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.ProfileItems.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return profile.Id;
    }
}
