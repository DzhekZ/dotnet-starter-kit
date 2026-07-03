using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record DeleteSubdivisionCommand(Guid SubdivisionId) : ICommand<Unit>;
