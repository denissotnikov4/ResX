using MediatR;

namespace ResX.Users.Application.Commands.CreateUserProfile;

public record CreateUserProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? City) : IRequest<Unit>;