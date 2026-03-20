using System.Text.RegularExpressions;
using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Identity.Domain.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{9,14}$",
        RegexOptions.Compiled);

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PhoneNumber Create(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("Phone number cannot be empty.");
        }

        phone = phone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        return !PhoneRegex.IsMatch(phone)
            ? throw new DomainException($"'{phone}' is not a valid phone number.")
            : new PhoneNumber(phone);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(PhoneNumber phone)
    {
        return phone.Value;
    }
}