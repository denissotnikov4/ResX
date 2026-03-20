using MediatR;
using ResX.Users.Application.DTOs;

namespace ResX.Users.Application.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;