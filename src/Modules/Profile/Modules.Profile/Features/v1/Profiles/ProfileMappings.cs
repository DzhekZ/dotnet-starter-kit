using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Domain;

namespace FSH.Modules.Profile.Features.v1.Profiles;

internal static class ProfileMappings
{
    public static ProfileDto ToDto(this ProfileItem p) => new(
        p.Id,
        p.Name,
        p.Slug,
        p.LastName,
        p.FirstName,
        p.MiddleName,
        p.PersonnelNumber,
        p.CodePerson,
        p.Email,
        p.Login,
        p.AdSid,
        p.DateBirth,
        p.DateHire,
        p.DateDismiss,
        p.Sex,
        p.IsBoss,
        p.TypeEmployment,
        p.Staffing,
        p.City,
        p.Category,
        p.PhoneMobile,
        p.PhoneMobileAllowShow,
        p.PhoneWork,
        p.Division,
        p.Place,
        p.WtHcmId,
        p.IsDecret,
        p.IsMobilization,
        p.Subordinates,
        p.Information,
        p.Description,
        p.PositionId,
        p.SubdivisionId,
        p.HierarchyId,
        p.IsActive,
        p.ThumbnailUrl,
        p.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProfileImageDto(i.Id, i.FileAssetId, i.Url, i.IsThumbnail, i.SortOrder, i.CreatedAtUtc))
            .ToList(),
        p.CreatedAtUtc,
        p.UpdatedAtUtc,
        p.DeletedOnUtc,
        p.DeletedBy);
}
