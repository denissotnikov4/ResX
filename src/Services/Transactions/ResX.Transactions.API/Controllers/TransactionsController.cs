using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Common.Models;
using ResX.Transactions.Application.Commands.AgreeTransaction;
using ResX.Transactions.Application.Commands.CancelTransaction;
using ResX.Transactions.Application.Commands.ConfirmReceipt;
using ResX.Transactions.Application.Commands.CreateTransaction;
using ResX.Transactions.Application.Commands.DisputeTransaction;
using ResX.Transactions.Application.DTOs;
using ResX.Transactions.Application.Queries.GetMyTransactions;
using ResX.Transactions.Application.Queries.GetTransactionById;

namespace ResX.Transactions.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Возвращает список транзакций текущего пользователя (как донора, так и получателя).</summary>
    [HttpGet]
    [ProducesResponseType<PagedList<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result = await _mediator.Send(
            new GetMyTransactionsQuery(userId, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Возвращает транзакцию по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTransactionByIdQuery(id), cancellationToken);

        return Ok(result);
    }

    /// <summary>Создаёт новую транзакцию — запрос от получателя на получение вещи донора.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var transactionId = await _mediator.Send(
            new CreateTransactionCommand(request.ListingId, request.DonorId, userId, request.Type, request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transactionId }, new { id = transactionId });
    }

    /// <summary>Донор подтверждает согласие на передачу вещи. Переводит транзакцию из Pending в DonorAgreed.</summary>
    [HttpPost("{id:guid}/agree")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Agree(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new AgreeTransactionCommand(id, userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Получатель подтверждает факт получения вещи. Переводит транзакцию в Completed.</summary>
    [HttpPost("{id:guid}/confirm-receipt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmReceipt(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new ConfirmReceiptCommand(id, userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Отменяет транзакцию. Доступно любому участнику, пока транзакция не завершена.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new CancelTransactionCommand(id, userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Открывает спор по транзакции. Доступно любому участнику, пока транзакция не завершена и не отменена.</summary>
    [HttpPost("{id:guid}/dispute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Dispute(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new DisputeTransactionCommand(id, userId), cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
