namespace GerenciadorDeFinancas.Domain.ValueObjects;

public readonly record struct Money(long Cents)
{
    public decimal Amount => Cents / 100m;

    public static Money Zero => new(0);

    public static Money FromCents(long cents) => new(cents);

    public static Money FromDecimal(decimal amount) =>
        new(checked((long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)));

    public Money Add(Money other) => new(Cents + other.Cents);

    public Money Subtract(Money other) => new(Cents - other.Cents);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static bool operator <(Money left, Money right) => left.Cents < right.Cents;

    public static bool operator >(Money left, Money right) => left.Cents > right.Cents;

    public static IReadOnlyList<Money> SplitEvenlyIntoParts(Money total, int parts)
    {
        if (parts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parts), "Número de partes deve ser maior que zero.");
        }

        var baseAmount = total.Cents / parts;
        var remainder = total.Cents % parts;
        var result = new Money[parts];
        for (var i = 0; i < parts; i++)
        {
            result[i] = new Money(baseAmount + (i < remainder ? 1 : 0));
        }

        return result;
    }

    public override string ToString() => Amount.ToString("C2");
}
