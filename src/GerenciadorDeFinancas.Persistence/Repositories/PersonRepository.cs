using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence.Repositories;

public sealed class PersonRepository : IPersonRepository
{
    private readonly FinanceDbContext _context;

    public PersonRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Persons.FirstOrDefaultAsync(person => person.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Person>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.Persons
            .Where(person => person.IsActive)
            .OrderBy(person => person.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Persons
            .OrderBy(person => person.Name)
            .ToListAsync(cancellationToken);

    public void Add(Person person) => _context.Persons.Add(person);

    public void Update(Person person) => _context.Persons.Update(person);
}
