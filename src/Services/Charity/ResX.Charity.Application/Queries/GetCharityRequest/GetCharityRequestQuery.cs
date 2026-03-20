using MediatR;
using ResX.Charity.Application.DTOs;

namespace ResX.Charity.Application.Queries.GetCharityRequest;

public record GetCharityRequestQuery(Guid RequestId) : IRequest<CharityRequestDto>;
