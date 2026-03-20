using MediatR;

namespace ResX.Disputes.Application.Commands.OpenDispute;

public record OpenDisputeCommand(
    Guid TransactionId,
    Guid InitiatorId,
    Guid RespondentId,
    string Reason) : IRequest<Guid>;