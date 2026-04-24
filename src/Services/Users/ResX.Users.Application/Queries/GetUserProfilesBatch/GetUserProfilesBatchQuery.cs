using MediatR;
using ResX.Users.Application.DTOs;

namespace ResX.Users.Application.Queries.GetUserProfilesBatch;

public record GetUserProfilesBatchQuery(IReadOnlyCollection<Guid> UserIds)
    : IRequest<IReadOnlyList<UserProfileBriefDto>>;