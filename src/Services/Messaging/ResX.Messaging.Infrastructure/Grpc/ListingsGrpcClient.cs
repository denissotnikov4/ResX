using ResX.Listings.API.Grpc;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.Services;

namespace ResX.Messaging.Infrastructure.Grpc;

public class ListingsGrpcClient : IListingsClient
{
    private readonly ListingsService.ListingsServiceClient _client;

    public ListingsGrpcClient(ListingsService.ListingsServiceClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyDictionary<Guid, ListingSummaryDto>> GetListingSummariesAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
            return new Dictionary<Guid, ListingSummaryDto>();

        var request = new GetListingsBriefRequest();
        foreach (var id in listingIds.Distinct())
            request.ListingIds.Add(id.ToString());

        var response = await _client.GetListingsBriefAsync(request, cancellationToken: cancellationToken);

        var result = new Dictionary<Guid, ListingSummaryDto>(response.Listings.Count);
        foreach (var l in response.Listings)
        {
            if (!Guid.TryParse(l.ListingId, out var id))
                continue;

            result[id] = new ListingSummaryDto(id, l.Title);
        }

        return result;
    }
}
