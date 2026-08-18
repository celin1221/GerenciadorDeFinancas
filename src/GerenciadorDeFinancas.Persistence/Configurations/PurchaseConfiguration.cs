using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(purchase => purchase.Id);
        builder.Property(purchase => purchase.AmountCents).IsRequired();
        builder.Property(purchase => purchase.Description).HasMaxLength(500).IsRequired();
        builder.Property(purchase => purchase.DedupHash).HasMaxLength(64);
        builder.Property(purchase => purchase.RawNotificationText);
        builder.HasIndex(purchase => purchase.DedupHash).IsUnique();
        builder.HasIndex(purchase => purchase.Status);
        builder.HasIndex(purchase => purchase.StatementId);
        builder.HasIndex(purchase => purchase.CardId);
        builder.HasOne(purchase => purchase.Card)
            .WithMany()
            .HasForeignKey(purchase => purchase.CardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(purchase => purchase.Merchant)
            .WithMany()
            .HasForeignKey(purchase => purchase.MerchantId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(purchase => purchase.Category)
            .WithMany()
            .HasForeignKey(purchase => purchase.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(purchase => purchase.Statement)
            .WithMany(statement => statement.Purchases)
            .HasForeignKey(purchase => purchase.StatementId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
