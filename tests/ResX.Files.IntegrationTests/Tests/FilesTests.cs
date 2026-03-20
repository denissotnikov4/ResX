using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using ResX.Files.Application.DTOs;
using ResX.Files.IntegrationTests.Collections;
using ResX.Files.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Files.IntegrationTests.Tests;

[Collection(FilesCollection.Name)]
public sealed class FilesTests : IAsyncLifetime
{
    private readonly FilesWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();

    public FilesTests(FilesWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // POST /api/files/upload
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Upload_ValidImageFile_Returns200WithFileRecord()
    {
        var content = CreateMultipartContent("photo.jpg", "image/jpeg", "fake-image-bytes"u8.ToArray());

        var response = await _client.PostAsync("/api/files/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var record = await response.ReadAsAsync<FileRecordDto>();
        record.Id.Should().NotBeEmpty();
        record.OriginalName.Should().Be("photo.jpg");
        record.UploadedBy.Should().Be(_userId);
        record.Url.Should().Contain("cdn.example.com");
    }

    [Fact]
    public async Task Upload_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var content = CreateMultipartContent("photo.jpg", "image/jpeg", "bytes"u8.ToArray());

        var response = await _client.PostAsync("/api/files/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_PdfFile_Returns200()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var content = CreateMultipartContent("document.pdf", "application/pdf", "pdf-bytes"u8.ToArray());

        var response = await _client.PostAsync("/api/files/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var record = await response.ReadAsAsync<FileRecordDto>();
        record.ContentType.Should().Be("application/pdf");
    }

    // -------------------------------------------------------------------------
    // DELETE /api/files/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_OwnFile_Returns204()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var fileId = await UploadFileAsync();

        var response = await _client.DeleteAsync($"/api/files/{fileId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AnotherUsersFile_Returns404OrForbidden()
    {
        // Upload as userId
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var fileId = await UploadFileAsync();

        // Try to delete as a different user
        var otherId = Guid.NewGuid();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(otherId, "other@test.com"));
        var response = await _client.DeleteAsync($"/api/files/{fileId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentFile_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/files/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.DeleteAsync($"/api/files/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GET /api/files/{id}/url
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUrl_ExistingFile_Returns200WithUrl()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var fileId = await UploadFileAsync();

        var response = await _client.GetAsync($"/api/files/{fileId}/url");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<UrlResponse>();
        body.Url.Should().Contain("cdn.example.com");
    }

    [Fact]
    public async Task GetUrl_NonExistentFile_Returns404()
    {
        var response = await _client.GetAsync($"/api/files/{Guid.NewGuid()}/url");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUrl_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync($"/api/files/{Guid.NewGuid()}/url");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> UploadFileAsync()
    {
        var content = CreateMultipartContent("test.jpg", "image/jpeg", "bytes"u8.ToArray());
        var response = await _client.PostAsync("/api/files/upload", content);
        var record = await response.ReadAsAsync<FileRecordDto>();
        return record.Id;
    }

    private static MultipartFormDataContent CreateMultipartContent(
        string fileName, string contentType, byte[] bytes)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    private sealed record UrlResponse(string Url);
}
