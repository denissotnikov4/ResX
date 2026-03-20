namespace ResX.Transactions.Domain.Enums;

public enum TransactionStatus
{
    Pending = 1,
    DonorAgreed = 2,
    RecipientAgreed = 3,
    Completed = 4,
    Cancelled = 5,
    Disputed = 6
}
