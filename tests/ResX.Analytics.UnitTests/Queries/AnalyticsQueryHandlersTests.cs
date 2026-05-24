using FluentAssertions;
using NSubstitute;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Queries.GetCategoryStats;
using ResX.Analytics.Application.Queries.GetCityStats;
using ResX.Analytics.Application.Queries.GetEcoStats;
using ResX.Analytics.Application.Repositories;
using Xunit;

namespace ResX.Analytics.UnitTests.Queries;

public class AnalyticsQueryHandlersTests
{
    private readonly IAnalyticsRepository _repository = Substitute.For<IAnalyticsRepository>();

    [Fact]
    public async Task GetCategoryStats_ReturnsRepositoryResult()
    {
        var data = new List<CategoryStatsDto>
        {
            new(Guid.NewGuid(), "Books", 5, 3),
            new(Guid.NewGuid(), "Toys", 1, 0)
        };
        _repository.GetCategoryStatsAsync(Arg.Any<CancellationToken>()).Returns(data);
        var sut = new GetCategoryStatsQueryHandler(_repository);

        var result = await sut.Handle(new GetCategoryStatsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task GetCityStats_ReturnsRepositoryResult()
    {
        var data = new List<CityStatsDto>
        {
            new("Moscow", 10, 20, 30m)
        };
        _repository.GetCityStatsAsync(Arg.Any<CancellationToken>()).Returns(data);
        var sut = new GetCityStatsQueryHandler(_repository);

        var result = await sut.Handle(new GetCityStatsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task GetEcoStats_ReturnsRepositoryResult()
    {
        var data = new EcoPlatformStatsDto(100, 50m, 30m, 5, 100, DateTime.UtcNow);
        _repository.GetEcoStatsAsync(Arg.Any<CancellationToken>()).Returns(data);
        var sut = new GetEcoStatsQueryHandler(_repository);

        var result = await sut.Handle(new GetEcoStatsQuery(), CancellationToken.None);

        result.Should().Be(data);
    }
}
