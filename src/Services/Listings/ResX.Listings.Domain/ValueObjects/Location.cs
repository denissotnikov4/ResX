using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Listings.Domain.ValueObjects;

public sealed class Location : ValueObject
{
    private Location(string city, string? district, double? latitude, double? longitude)
    {
        City = city;
        District = district;
        Latitude = latitude;
        Longitude = longitude;
    }

    public string City { get; }
    
    public string? District { get; }
    
    public double? Latitude { get; }
    
    public double? Longitude { get; }

    public static Location Create(
        string city,
        string? district = null,
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("City cannot be empty.");
        }

        if (latitude is < -90 or > 90)
        {
            throw new DomainException("Latitude must be between -90 and 90.");
        }

        return longitude is < -180 or > 180 
            ? throw new DomainException("Longitude must be between -180 and 180.") 
            : new Location(city, district, latitude, longitude);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return District;
        yield return Latitude;
        yield return Longitude;
    }
}