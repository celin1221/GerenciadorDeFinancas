using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");
        builder.HasKey(person => person.Id);
        builder.Property(person => person.Name).HasMaxLength(100).IsRequired();
        builder.Property(person => person.Color).HasMaxLength(20);
    }
}
