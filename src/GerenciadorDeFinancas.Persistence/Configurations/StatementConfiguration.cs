using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class StatementConfiguration : IEntityTypeConfiguration<Statement>
{
    public void Configure(EntityTypeBuilder<Statement> builder)
    {
        builder.ToTable("Statements");
        builder.HasKey(statement => statement.Id);
        builder.HasIndex(statement => new { statement.CardId, statement.YearMonth }).IsUnique();
        builder.Property(statement => statement.YearMonth).IsRequired();
        builder.HasOne(statement => statement.Card)
            .WithMany()
            .HasForeignKey(statement => statement.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
