using MediatR;
using ResX.Common.Models;
using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Queries.GetMyListings;

public record GetMyListingsQuery(
    Guid DonorId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<ListingPreviewDto>>;
