using System.Globalization;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.Converters;

public sealed class MoneyToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long cents)
        {
            return Money.FromCents(cents).ToString();
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
