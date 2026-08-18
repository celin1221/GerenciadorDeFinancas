using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("Merchants");
        builder.HasKey(merchant => merchant.Id);
        builder.HasIndex(merchant => merchant.NormalizedName).IsUnique();
        builder.Property(merchant => merchant.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(merchant => merchant.NormalizedName).HasMaxLength(200).IsRequired();
        builder.HasOne(merchant => merchant.Category)
            .WithMany()
            .HasForeignKey(merchant => merchant.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
