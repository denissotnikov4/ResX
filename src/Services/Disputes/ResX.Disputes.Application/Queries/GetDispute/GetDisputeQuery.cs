using MediatR;
using ResX.Disputes.Application.DTOs;

namespace ResX.Disputes.Application.Queries.GetDispute;

public record GetDisputeQuery(Guid DisputeId) : IRequest<DisputeDto>;
