using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record UpdateSubdivisionCommand(
    Guid SubdivisionId,
    string Name,
    string TypeSubdivision,
    string? Description = null,
    Guid? ParentSubdivisionId = null) : ICommand<Guid>;
