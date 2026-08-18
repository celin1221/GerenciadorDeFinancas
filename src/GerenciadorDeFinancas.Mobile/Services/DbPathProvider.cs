using GerenciadorDeFinancas.Persistence;

namespace GerenciadorDeFinancas.Services;

public sealed class DbPathProvider : IDbPathProvider
{
    public string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, "gerenciador_financas.db3");
}
