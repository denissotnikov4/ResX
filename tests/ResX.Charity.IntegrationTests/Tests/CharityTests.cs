using System.Net;
using FluentAssertions;
using ResX.Charity.Application.DTOs;
using ResX.Charity.IntegrationTests.Collections;
using ResX.Charity.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Charity.IntegrationTests.Tests;

[Collection(CharityCollection.Name)]
public sealed class CharityTests : IAsyncLifetime
{
    private readonly CharityWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();

    public CharityTests(CharityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/charity/requests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCharityRequests_EmptyState_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/charity/requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<CharityRequestListResponse>();
        body.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // POST /api/charity/organizations
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateOrganization_WithValidData_Returns200WithOrgId()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "org@test.com"));

        var response = await _client.PostJsonAsync("/api/charity/organizations", new CreateOrganizationDto(
            Name: "Фонд помощи",
            Description: "Помогаем малоимущим семьям",
            LegalDocumentUrl: "https://docs.example.com/legal.pdf"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<OrgIdResponse>();
        body.OrgId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateOrganization_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/charity/organizations",
            new CreateOrganizationDto("Фонд", "Описание", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // PUT /api/charity/organizations/{id}/verify
    // -------------------------------------------------------------------------

    [Fact]
    public async Task VerifyOrganization_AsAdmin_Returns204()
    {
        var orgId = await CreateOrganizationAsync(_userId, "org-verify@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/verify", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task VerifyOrganization_AsNonAdmin_Returns403()
    {
        var orgId = await CreateOrganizationAsync(_userId, "org-verify-nonadmin@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "user@test.com", role: "Donor"));
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/verify", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyOrganization_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{Guid.NewGuid()}/verify", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyOrganization_NotFound_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{Guid.NewGuid()}/verify", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // PUT /api/charity/organizations/{id}/reject
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RejectOrganization_AsAdmin_Returns204()
    {
        var orgId = await CreateOrganizationAsync(_userId, "org-reject@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/reject", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RejectOrganization_AsNonAdmin_Returns403()
    {
        var orgId = await CreateOrganizationAsync(_userId, "org-reject-nonadmin@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "user@test.com", role: "Donor"));
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/reject", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectOrganization_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{Guid.NewGuid()}/reject", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RejectOrganization_NotFound_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PutJsonAsync($"/api/charity/organizations/{Guid.NewGuid()}/reject", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/charity/requests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCharityRequest_WithoutOrganization_Returns400()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "noorg@test.com"));

        var response = await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            Title: "Нужна одежда",
            Description: "Тёплая одежда для детей",
            DeadlineDate: DateTime.UtcNow.AddDays(30),
            Items: [new CreateRequestedItemDto(Guid.NewGuid(), "Одежда", 10, "Any")]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCharityRequest_WithPendingOrg_Returns403()
    {
        // Create org but do NOT verify — stays Pending
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "pending@test.com"));
        await _client.PostJsonAsync("/api/charity/organizations",
            new CreateOrganizationDto("Фонд (Pending)", "Описание", null));

        var response = await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            Title: "Нужны игрушки",
            Description: "Для детского дома",
            DeadlineDate: null,
            Items: [new CreateRequestedItemDto(Guid.NewGuid(), "Игрушки", 5, "Good")]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCharityRequest_WithRejectedOrg_Returns403()
    {
        var orgId = await CreateOrganizationAsync(_userId, "rejected@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/reject", new { });

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "rejected@test.com"));
        var response = await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            Title: "Нужны игрушки",
            Description: "Для детского дома",
            DeadlineDate: null,
            Items: [new CreateRequestedItemDto(Guid.NewGuid(), "Игрушки", 5, "Good")]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCharityRequest_WithVerifiedOrg_Returns201()
    {
        var orgId = await CreateOrganizationAsync(_userId, "verified@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/verify", new { });

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "verified@test.com"));
        var response = await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            Title: "Нужны игрушки",
            Description: "Для детского дома",
            DeadlineDate: DateTime.UtcNow.AddDays(14),
            Items: [new CreateRequestedItemDto(Guid.NewGuid(), "Игрушки", 5, "Good")]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCharityRequest_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            "Title", "Desc", null, []));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCharityRequests_AfterCreatingVerifiedRequest_ReturnsIt()
    {
        var orgId = await CreateOrganizationAsync(_userId, "verified2@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/verify", new { });

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "verified2@test.com"));
        await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            Title: "Запрос",
            Description: "Нужна помощь",
            DeadlineDate: null,
            Items: [new CreateRequestedItemDto(Guid.NewGuid(), "Вещи", 3, "Any")]));

        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/charity/requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<CharityRequestListResponse>();
        body.Items.Should().HaveCount(1);
    }

    // -------------------------------------------------------------------------
    // GET /api/charity/requests/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCharityRequest_ExistingRequest_Returns200WithDto()
    {
        var requestId = await CreateVerifiedRequest(_userId, "getreq@test.com");

        _client.WithoutAuth();
        var response = await _client.GetAsync($"/api/charity/requests/{requestId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<CharityRequestDto>();
        body.Id.Should().Be(requestId);
        body.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetCharityRequest_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/charity/requests/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GET /api/charity/organizations/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrganization_ExistingOrg_Returns200()
    {
        var orgId = await CreateOrganizationAsync(_userId, "getorg@test.com");

        _client.WithoutAuth();
        var response = await _client.GetAsync($"/api/charity/organizations/{orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<OrganizationResponse>();
        body.Id.Should().Be(orgId);
        body.VerificationStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task GetOrganization_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/charity/organizations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/charity/requests/{id}/cancel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CancelCharityRequest_AsAdmin_Returns204()
    {
        var requestId = await CreateVerifiedRequest(_userId, "cancel@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PostAsync($"/api/charity/requests/{requestId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CancelCharityRequest_AsNonAdmin_Returns403()
    {
        var requestId = await CreateVerifiedRequest(_userId, "cancel-user@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "user@test.com", role: "Donor"));
        var response = await _client.PostAsync($"/api/charity/requests/{requestId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelCharityRequest_NotFound_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PostAsync($"/api/charity/requests/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/charity/requests/{id}/complete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CompleteCharityRequest_AsAdmin_Returns204()
    {
        var requestId = await CreateVerifiedRequest(_userId, "complete@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PostAsync($"/api/charity/requests/{requestId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CompleteCharityRequest_AsNonAdmin_Returns403()
    {
        var requestId = await CreateVerifiedRequest(_userId, "complete-user@test.com");

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "user@test.com", role: "Donor"));
        var response = await _client.PostAsync($"/api/charity/requests/{requestId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteCharityRequest_NotFound_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        var response = await _client.PostAsync($"/api/charity/requests/{Guid.NewGuid()}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateOrganizationAsync(Guid userId, string email)
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(userId, email));
        var response = await _client.PostJsonAsync("/api/charity/organizations",
            new CreateOrganizationDto("Тест организация", "Описание", null));
        response.EnsureSuccessStatusCode();
        var body = await response.ReadAsAsync<OrgIdResponse>();
        return body.OrgId;
    }

    private async Task<Guid> CreateVerifiedRequest(Guid userId, string email)
    {
        var orgId = await CreateOrganizationAsync(userId, email);
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", role: "Admin"));
        await _client.PutJsonAsync($"/api/charity/organizations/{orgId}/verify", new { });
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(userId, email));
        var resp = await _client.PostJsonAsync("/api/charity/requests", new CreateCharityRequestDto(
            Title: "Тест запрос", Description: "Описание", DeadlineDate: null,
            Items: [new CreateRequestedItemDto(Guid.NewGuid(), "Вещи", 1, "Any")]));
        resp.EnsureSuccessStatusCode();
        var body = await resp.ReadAsAsync<RequestIdResponse>();
        return body.Id;
    }

    private sealed record OrgIdResponse(Guid OrgId);
    private sealed record RequestIdResponse(Guid Id);
    private sealed record OrganizationResponse(Guid Id, Guid UserId, string Name, string Description, string VerificationStatus, string? LegalDocumentUrl, DateTime CreatedAt);
    private sealed record CharityRequestListResponse(List<CharityRequestDto> Items, int TotalCount, int PageNumber, int PageSize);
}
