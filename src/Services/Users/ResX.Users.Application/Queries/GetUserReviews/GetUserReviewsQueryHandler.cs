using MediatR;
using ResX.Common.Models;
using ResX.Users.Application.DTOs;
using ResX.Users.Application.Repositories;

namespace ResX.Users.Application.Queries.GetUserReviews;

public class GetUserReviewsQueryHandler : IRequestHandler<GetUserReviewsQuery, PagedList<ReviewDto>>
{
    private readonly IUserProfileRepository _repository;

    public GetUserReviewsQueryHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedList<ReviewDto>> Handle(GetUserReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews =
            await _repository.GetReviewsAsync(request.UserId, request.PageNumber, request.PageSize, cancellationToken);
        
        var reviewDtos = reviews.Items
            .Select(r => new ReviewDto(r.Id, r.ReviewerId, r.ReviewerName, r.Rating, r.Comment, r.CreatedAt))
            .ToList()
            .AsReadOnly();

        return new PagedList<ReviewDto>(reviewDtos, reviews.TotalCount, request.PageNumber, request.PageSize);
    }
}