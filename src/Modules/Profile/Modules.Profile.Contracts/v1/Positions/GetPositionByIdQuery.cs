using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Positions;

public sealed record GetPositionByIdQuery(Guid PositionId) : IQuery<PositionDto>;
