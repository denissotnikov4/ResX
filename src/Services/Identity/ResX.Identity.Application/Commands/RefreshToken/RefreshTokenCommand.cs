using MediatR;
using ResX.Identity.Application.DTOs;

namespace ResX.Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokensDto>;
