using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record RestoreSubdivisionCommand(Guid SubdivisionId) : ICommand<Guid>;
