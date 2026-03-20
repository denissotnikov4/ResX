using MediatR;

namespace ResX.Identity.Application.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string OldPassword, string NewPassword) : IRequest<Unit>;
