using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence;

public sealed class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    public DbSet<Person> Persons => Set<Person>();

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<Statement> Statements => Set<Statement>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseShare> PurchaseShares => Set<PurchaseShare>();

    public DbSet<Merchant> Merchants => Set<Merchant>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<NotificationButton> NotificationButtons => Set<NotificationButton>();

    public DbSet<NotificationButtonPerson> NotificationButtonPersons => Set<NotificationButtonPerson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
