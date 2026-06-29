namespace FSH.Modules.Profile.Contracts.Dtos;

public sealed record PositionDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
