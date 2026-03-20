using MediatR;

namespace ResX.Transactions.Application.Commands.ConfirmReceipt;

public record ConfirmReceiptCommand(
    Guid TransactionId,
    Guid RecipientId) : IRequest<Unit>;