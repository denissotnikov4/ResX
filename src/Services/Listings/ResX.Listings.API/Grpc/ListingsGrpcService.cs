using Grpc.Core;
using MediatR;
using ResX.Listings.Application.Commands.ChangeListingStatus;
using ResX.Listings.Application.Queries.GetListingById;
using ResX.Listings.Domain.Enums;

namespace ResX.Listings.API.Grpc;

public class ListingsGrpcService : ListingsService.ListingsServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ListingsGrpcService> _logger;

    public ListingsGrpcService(IMediator mediator, ILogger<ListingsGrpcService> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
                DonorId = listing.DonorId.ToString(),
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
