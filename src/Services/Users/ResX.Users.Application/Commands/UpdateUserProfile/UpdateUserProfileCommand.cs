using MediatR;

namespace ResX.Users.Application.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Bio,
    string? City) : IRequest<Unit>;