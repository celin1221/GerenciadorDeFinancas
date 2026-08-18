using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class CreatePersonUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ValidPerson_PersistsAndReturnsId()
    {
        var useCase = new CreatePersonUseCase(_factory);

        var id = await useCase.ExecuteAsync("Marcelo", "#512BD4");

        Assert.NotEqual(Guid.Empty, id);
        using var unitOfWork = _factory.Create();
        var person = await unitOfWork.Persons.GetByIdAsync(id);
        Assert.NotNull(person);
        Assert.Equal("Marcelo", person!.Name);
        Assert.Equal("#512BD4", person.Color);
        Assert.True(person.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_BlankName_Throws()
    {
        var useCase = new CreatePersonUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync("   ", null));
    }

    [Fact]
    public async Task ExecuteAsync_TrimsName()
    {
        var useCase = new CreatePersonUseCase(_factory);

        var id = await useCase.ExecuteAsync("  Marcelo  ", null);

        using var unitOfWork = _factory.Create();
        var person = await unitOfWork.Persons.GetByIdAsync(id);
        Assert.Equal("Marcelo", person!.Name);
    }
}
