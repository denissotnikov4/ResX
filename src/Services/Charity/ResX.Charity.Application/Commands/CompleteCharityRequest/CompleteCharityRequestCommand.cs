using MediatR;

namespace ResX.Charity.Application.Commands.CompleteCharityRequest;

public record CompleteCharityRequestCommand(Guid RequestId) : IRequest<Unit>;