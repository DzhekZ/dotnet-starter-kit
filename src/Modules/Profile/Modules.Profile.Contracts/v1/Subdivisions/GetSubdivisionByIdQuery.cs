using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record GetSubdivisionByIdQuery(Guid SubdivisionId) : IQuery<SubdivisionDto>;
