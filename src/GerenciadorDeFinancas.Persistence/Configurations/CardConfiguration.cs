using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");
        builder.HasKey(card => card.Id);
        builder.Property(card => card.Name).HasMaxLength(60).IsRequired();
        builder.Property(card => card.BankId).HasMaxLength(40).IsRequired();
        builder.Property(card => card.Last4Digits).HasMaxLength(4);
        builder.HasIndex(card => new { card.BankId, card.Last4Digits });
        builder.HasOne(card => card.Owner)
            .WithMany()
            .HasForeignKey(card => card.OwnerPersonId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
