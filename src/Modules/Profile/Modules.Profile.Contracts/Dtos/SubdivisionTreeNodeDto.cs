namespace FSH.Modules.Profile.Contracts.Dtos;

public sealed record SubdivisionTreeNodeDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<SubdivisionTreeNodeDto> Children);
