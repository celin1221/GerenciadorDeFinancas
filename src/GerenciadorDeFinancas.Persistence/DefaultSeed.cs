namespace GerenciadorDeFinancas.Persistence;

internal static class DefaultSeed
{
    public static IReadOnlyList<(string Name, string Icon, string Color)> Categories { get; } = new[]
    {
        ("Alimentação", "restaurant", "#E57373"),
        ("Supermercado", "cart", "#F06292"),
        ("Transporte", "directions_car", "#64B5F6"),
        ("Moradia", "home", "#FFB74D"),
        ("Lazer", "celebration", "#BA68C8"),
        ("Saúde", "favorite", "#4DB6AC"),
        ("Educação", "school", "#81C784"),
        ("Compras", "shopping_bag", "#FF8A65"),
        ("Assinaturas", "subscriptions", "#A1887F"),
        ("Outros", "category", "#90A4AE"),
    };
}
