using MediatR;

namespace ResX.Charity.Application.Commands.CancelCharityRequest;

public record CancelCharityRequestCommand(Guid RequestId) : IRequest<Unit>;