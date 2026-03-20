using MediatR;

namespace ResX.Users.Application.Commands.UpdateAvatar;

public record UpdateAvatarCommand(
    Guid UserId,
    string AvatarUrl) : IRequest<Unit>;