using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Services;

public interface IUsersClient
{
    Task<IReadOnlyDictionary<Guid, DonorDto>> GetDonorsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}