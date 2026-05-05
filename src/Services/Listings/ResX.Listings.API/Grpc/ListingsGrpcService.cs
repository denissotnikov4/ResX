using Grpc.Core;
using MediatR;
using ResX.Listings.Application.Commands.ChangeListingStatus;
using ResX.Listings.Application.Queries.GetListingById;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.Enums;

namespace ResX.Listings.API.Grpc;

public class ListingsGrpcService : ListingsService.ListingsServiceBase
{
    private readonly IMediator _mediator;
    private readonly IListingRepository _repository;
    private readonly ILogger<ListingsGrpcService> _logger;

    public ListingsGrpcService(IMediator mediator, IListingRepository repository, ILogger<ListingsGrpcService> logger)
    {
        _mediator = mediator;
        _repository = repository;
        _logger = logger;
    }

    public override async Task<GetListingsBriefResponse> GetListingsBrief(
        GetListingsBriefRequest request,
        ServerCallContext context)
    {
        var ids = request.ListingIds
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        var response = new GetListingsBriefResponse();
        if (ids.Count == 0)
            return response;

        var listings = await _repository.GetByIdsAsync(ids, context.CancellationToken);
        foreach (var l in listings)
        {
            response.Listings.Add(new ListingBrief
            {
                ListingId = l.Id.ToString(),
                Title = l.Title,
                DonorId = l.DonorId.ToString(),
                Status = l.Status.ToString()
            });
        }
        return response;
    }

    public override async Task<GetListingByIdResponse> GetListingById(
        GetListingByIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid listing ID."));
        }

        try
        {
            var listing = await _mediator.Send(new GetListingByIdQuery(listingId), context.CancellationToken);
            return new GetListingByIdResponse
            {
                ListingId = listing.Id.ToString(),
                Title = listing.Title,
                DonorId = listing.Donor.Id.ToString(),
                Status = listing.Status,
                TransferType = listing.TransferType,
                CategoryName = listing.Category.Name
            };
        }
        catch (Common.Exceptions.NotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Listing {request.ListingId} not found."));
        }
    }

    public override async Task<ReserveListingResponse> ReserveListingForTransaction(
        ReserveListingRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId) ||
            !Guid.TryParse(request.RecipientId, out var recipientId))
        {
            return new ReserveListingResponse { Success = false, ErrorMessage = "Invalid IDs." };
        }

        try
        {
            await _mediator.Send(
                new ChangeListingStatusCommand(listingId, recipientId, ListingStatus.Reserved),
                context.CancellationToken);

            return new ReserveListingResponse { Success = true };
        }
        catch (Exception ex)
        {
            return new ReserveListingResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<CompleteListingResponse> CompleteListingTransaction(
        CompleteListingRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId))
        {
            return new CompleteListingResponse { Success = false };
        }

        try
        {
            await _mediator.Send(
                new ChangeListingStatusCommand(listingId, Guid.Empty, ListingStatus.Completed),
                context.CancellationToken);

            return new CompleteListingResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete listing {ListingId}", listingId);

            return new CompleteListingResponse { Success = false };
        }
    }
}
