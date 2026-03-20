using ResX.Common.Domain;

namespace ResX.Users.Domain.ValueObjects;

public sealed class EcoStats : ValueObject
{
    private EcoStats(int itemsGifted, int itemsReceived, decimal co2SavedKg, decimal wasteSavedKg)
    {
        ItemsGifted = itemsGifted;
        ItemsReceived = itemsReceived;
        Co2SavedKg = co2SavedKg;
        WasteSavedKg = wasteSavedKg;
    }

    public int ItemsGifted { get; }

    public int ItemsReceived { get; }

    public decimal Co2SavedKg { get; }

    public decimal WasteSavedKg { get; }

    public static EcoStats Create(
        int itemsGifted = 0,
        int itemsReceived = 0,
        decimal co2SavedKg = 0,
        decimal wasteSavedKg = 0)
    {
        return new EcoStats(
            Math.Max(0, itemsGifted),
            Math.Max(0, itemsReceived),
            Math.Max(0, co2SavedKg),
            Math.Max(0, wasteSavedKg));
    }

    public EcoStats AddGifted(int count, decimal co2, decimal waste)
    {
        return new EcoStats(ItemsGifted + count, ItemsReceived, Co2SavedKg + co2, WasteSavedKg + waste);
    }

    public EcoStats AddReceived(int count)
    {
        return new EcoStats(ItemsGifted, ItemsReceived + count, Co2SavedKg, WasteSavedKg);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ItemsGifted;
        yield return ItemsReceived;
        yield return Co2SavedKg;
        yield return WasteSavedKg;
    }
}