using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Positions;

public sealed record CreatePositionCommand(
    string Name,
    string? Description = null) : ICommand<Guid>;
