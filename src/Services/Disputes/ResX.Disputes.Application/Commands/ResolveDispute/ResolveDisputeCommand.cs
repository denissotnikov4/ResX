using MediatR;

namespace ResX.Disputes.Application.Commands.ResolveDispute;

public record ResolveDisputeCommand(
    Guid DisputeId,
    Guid ModeratorId,
    string Resolution) : IRequest<Unit>;