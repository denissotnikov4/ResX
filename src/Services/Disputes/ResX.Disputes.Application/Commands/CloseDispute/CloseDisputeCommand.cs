using MediatR;

namespace ResX.Disputes.Application.Commands.CloseDispute;

public record CloseDisputeCommand(Guid DisputeId) : IRequest<Unit>;