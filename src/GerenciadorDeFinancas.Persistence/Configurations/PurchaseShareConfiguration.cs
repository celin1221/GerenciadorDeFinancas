using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class PurchaseShareConfiguration : IEntityTypeConfiguration<PurchaseShare>
{
    public void Configure(EntityTypeBuilder<PurchaseShare> builder)
    {
        builder.ToTable("PurchaseShares");
        builder.HasKey(share => share.Id);
        builder.Property(share => share.AmountCents).IsRequired();
        builder.HasIndex(share => new { share.PurchaseId, share.PersonId });
        builder.HasOne(share => share.Purchase)
            .WithMany(purchase => purchase.Shares)
            .HasForeignKey(share => share.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(share => share.Person)
            .WithMany()
            .HasForeignKey(share => share.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
