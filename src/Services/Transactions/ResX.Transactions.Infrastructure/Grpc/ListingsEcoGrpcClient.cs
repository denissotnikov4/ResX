using Grpc.Core;
using ResX.Listings.API.Grpc;
using ResX.Transactions.Application.Services;

namespace ResX.Transactions.Infrastructure.Grpc;

public class ListingsEcoGrpcClient : IListingsEcoClient
{
    private readonly ListingsService.ListingsServiceClient _client;

    public ListingsEcoGrpcClient(ListingsService.ListingsServiceClient client)
    {
        _client = client;
    }

    public async Task<ListingEcoInfo?> GetEcoAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetListingByIdAsync(
                new GetListingByIdRequest { ListingId = listingId.ToString() },
                cancellationToken: cancellationToken);

            return new ListingEcoInfo(response.WeightGrams, response.Co2SavedG, response.WasteSavedG);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
