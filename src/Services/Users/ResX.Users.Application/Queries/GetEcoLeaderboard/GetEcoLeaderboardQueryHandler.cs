using MediatR;
using ResX.Common.Models;
using ResX.Users.Application.DTOs;
using ResX.Users.Application.Repositories;

namespace ResX.Users.Application.Queries.GetEcoLeaderboard;

public class GetEcoLeaderboardQueryHandler : IRequestHandler<GetEcoLeaderboardQuery, PagedList<UserProfileDto>>
{
    private readonly IUserProfileRepository _repository;

    public GetEcoLeaderboardQueryHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedList<UserProfileDto>> Handle(GetEcoLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var leaderboard =
            await _repository.GetLeaderboardAsync(request.PageNumber, request.PageSize, cancellationToken);

        var userProfileDtos = leaderboard.Items
            .Select(p => new UserProfileDto(
                p.Id,
                p.FirstName,
                p.LastName,
                p.AvatarUrl,
                p.Bio,
                p.City,
                p.Rating,
                p.ReviewCount,
                new EcoStatsDto(
                    p.EcoStats.ItemsGifted,
                    p.EcoStats.ItemsReceived,
                    p.EcoStats.Co2SavedKg, p.EcoStats.WasteSavedKg),
                p.CreatedAt))
            .ToList()
            .AsReadOnly();

        return new PagedList<UserProfileDto>(
            userProfileDtos,
            leaderboard.TotalCount,
            request.PageNumber,
            request.PageSize);
    }
}