using MediatR;
using ResX.Common.Models;
using ResX.Users.Application.DTOs;

namespace ResX.Users.Application.Queries.GetUserReviews;

public record GetUserReviewsQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<ReviewDto>>;