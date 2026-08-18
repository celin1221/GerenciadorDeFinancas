using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.HasIndex(category => category.Name).IsUnique();
        builder.Property(category => category.Name).HasMaxLength(60).IsRequired();
        builder.Property(category => category.Icon).HasMaxLength(60);
        builder.Property(category => category.Color).HasMaxLength(20);
        builder.HasOne(category => category.Parent)
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
