using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record CreateProfileCommand(
        string Name,
        int? PersonnelNumber,
        string? CodePerson,
        string? Email,
        string? Login,
        string? AdSid,
        DateTime? DateBirth,
        DateTime? DateHire,
        DateTime? DateDismiss,
        int? Sex,
        bool? IsBoss,
        string? TypeEmployment,
        string? Staffing,
        string? City,
        string? Category,
        string? PhoneMobile,
        bool? PhoneMobileAllowShow,
        string? PhoneWork,
        string? Division,
        string? Place,
        string? WtHcmId,
        bool? IsDecret,
        bool? IsMobilization,
        int? Subordinates,
        string? Information,
        string? Description,
        Guid PositionId,
        Guid SubdivisionId,
        Guid HierarchyId) : ICommand<Guid>;
