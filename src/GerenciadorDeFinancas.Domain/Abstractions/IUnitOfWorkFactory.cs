namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
