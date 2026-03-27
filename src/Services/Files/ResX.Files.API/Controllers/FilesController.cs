using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Files.Application.Commands.DeleteFile;
using ResX.Files.Application.Commands.UploadFile;
using ResX.Files.Application.DTOs;
using ResX.Files.Application.Queries.GetFileUrl;

namespace ResX.Files.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Загружает файл в хранилище.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    [ProducesResponseType<FileRecordDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await using var stream = file.OpenReadStream();

        var result = await _mediator.Send(
            new UploadFileCommand(stream, file.FileName, file.ContentType, file.Length, userId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Удаляет файл из хранилища.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new DeleteFileCommand(id, userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Возвращает временную URL-ссылку для скачивания файла.</summary>
    [HttpGet("{id:guid}/url")]
    [ProducesResponseType<FileUrlDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrl(Guid id, CancellationToken cancellationToken)
    {
        var url = await _mediator.Send(new GetFileUrlQuery(id), cancellationToken);

        return Ok(new FileUrlDto(url));
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
