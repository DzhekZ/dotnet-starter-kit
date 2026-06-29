using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Positions;

public sealed record UpdatePositionCommand(
    Guid PositionId,
    string Name,
    string? Description = null) : ICommand<Guid>;
