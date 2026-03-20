using MediatR;
using ResX.Common.Models;
using ResX.Users.Application.DTOs;

namespace ResX.Users.Application.Queries.GetEcoLeaderboard;

public record GetEcoLeaderboardQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<UserProfileDto>>;