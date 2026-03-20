using MediatR;
using ResX.Common.Models;
using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Queries.GetListings;

public record GetListingsQuery(
    Guid? CategoryId = null,
    string? Condition = null,
    string? TransferType = null,
    string? City = null,
    string? SearchQuery = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<ListingPreviewDto>>;
