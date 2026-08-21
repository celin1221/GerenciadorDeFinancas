namespace GerenciadorDeFinancas.Application.Banks;

public sealed record BankOption(string BankId, string DisplayName);

public static class KnownBanks
{
    public const string Nubank = "nubank";
    public const string MercadoPago = "mercadopago";
    public const string Inter = "inter";
    public const string BancoDoBrasil = "bb";
    public const string Generic = "generic";

    public const string NubankPackage = "com.nubank.nubank";
    public const string NubankPackageProduction = "com.nu.production";
    public const string MercadoPagoPackage = "com.mercadopago.wallet";
    public const string InterPackage = "br.com.inter";
    public const string BancoDoBrasilPackage = "br.com.bb.android";

    public static IReadOnlyList<string> KnownBankPackages { get; } = new[]
    {
        NubankPackage,
        NubankPackageProduction,
        MercadoPagoPackage,
        InterPackage,
        BancoDoBrasilPackage,
    };

    public static IReadOnlyList<BankOption> All { get; } = new[]
    {
        new BankOption(Nubank, "Nubank"),
        new BankOption(MercadoPago, "Mercado Pago"),
        new BankOption(Inter, "Inter"),
        new BankOption(BancoDoBrasil, "Banco do Brasil"),
        new BankOption(Generic, "Genérico / Outro banco"),
    };

    public static string? DisplayName(string bankId) =>
        All.FirstOrDefault(bank => bank.BankId == bankId)?.DisplayName;
}
