using ResX.Common.Domain;

namespace ResX.Charity.Domain.Entities;

public class RequestedItem : Entity<Guid>
{
    public Guid CategoryId { get; private set; }
    public string CategoryName { get; private set; } = string.Empty;
    public int QuantityNeeded { get; private set; }
    public int QuantityReceived { get; private set; }
    public string Condition { get; private set; } = string.Empty;
    public Guid CharityRequestId { get; private set; }

    private RequestedItem() { }

    public static RequestedItem Create(
        Guid charityRequestId,
        Guid categoryId,
        string categoryName,
        int quantityNeeded,
        string condition)
    {
        return new RequestedItem
        {
            Id = Guid.NewGuid(),
            CharityRequestId = charityRequestId,
            CategoryId = categoryId,
            CategoryName = categoryName,
            QuantityNeeded = quantityNeeded,
            QuantityReceived = 0,
            Condition = condition
        };
    }

    public void IncrementReceived(int count)
    {
        QuantityReceived = Math.Min(QuantityReceived + count, QuantityNeeded);
    }
}