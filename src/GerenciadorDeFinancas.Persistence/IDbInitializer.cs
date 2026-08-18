namespace GerenciadorDeFinancas.Persistence;

public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
