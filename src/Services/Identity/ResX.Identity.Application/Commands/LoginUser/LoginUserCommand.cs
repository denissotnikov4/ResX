using MediatR;
using ResX.Identity.Application.DTOs;

namespace ResX.Identity.Application.Commands.LoginUser;

public record LoginUserCommand(string Login, string Password) : IRequest<TokensDto>;
