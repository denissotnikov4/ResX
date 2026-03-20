using MediatR;
using ResX.Identity.Application.DTOs;

namespace ResX.Identity.Application.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;
