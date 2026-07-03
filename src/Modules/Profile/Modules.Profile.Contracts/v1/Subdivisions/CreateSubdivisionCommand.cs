using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record CreateSubdivisionCommand(
    string Name,
    string TypeSubdivision,
    string? Description = null,
    Guid? ParentSubdivisionId = null) : ICommand<Guid>;
