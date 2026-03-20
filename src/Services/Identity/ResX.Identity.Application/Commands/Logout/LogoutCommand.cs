using MediatR;

namespace ResX.Identity.Application.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Unit>;
