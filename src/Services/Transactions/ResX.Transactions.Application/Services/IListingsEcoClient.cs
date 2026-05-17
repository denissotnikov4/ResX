namespace ResX.Transactions.Application.Services;

public interface IListingsEcoClient
{
    /// <summary>
    /// Returns the cached eco-impact values stored on a listing.
    /// Used by Transactions when publishing TransactionCompleted so subscribers
    /// can credit the donor's lifetime EcoStats without their own gRPC roundtrip.
    /// </summary>
    Task<ListingEcoInfo?> GetEcoAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public record ListingEcoInfo(int WeightGrams, int Co2SavedG, int WasteSavedG);
