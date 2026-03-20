using MediatR;
using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Queries.GetListingById;

public record GetListingByIdQuery(Guid ListingId) : IRequest<ListingDto>;
