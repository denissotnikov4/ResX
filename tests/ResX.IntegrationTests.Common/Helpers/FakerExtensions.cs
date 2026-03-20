using Bogus;

namespace ResX.IntegrationTests.Common.Helpers;

/// <summary>
/// Centralized Bogus Faker configuration for consistent fake-data generation across test projects.
/// </summary>
public static class FakerExtensions
{
    public static readonly Faker Fake = new("ru");

    public static string RandomEmail() =>
        Fake.Internet.Email();

    public static string RandomPassword() =>
        Fake.Internet.Password(length: 12, memorable: false) + "A1!";

    public static string RandomFirstName() => Fake.Name.FirstName();
    public static string RandomLastName() => Fake.Name.LastName();

    public static string RandomTitle() =>
        Fake.Commerce.ProductName();

    public static string RandomDescription() =>
        Fake.Lorem.Sentences(3);

    public static string RandomCity() =>
        Fake.PickRandom("Москва", "Санкт-Петербург", "Казань", "Новосибирск", "Екатеринбург");
}
