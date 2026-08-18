using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class NotificationButtonConfiguration : IEntityTypeConfiguration<NotificationButton>
{
    public void Configure(EntityTypeBuilder<NotificationButton> builder)
    {
        builder.ToTable("NotificationButtons");
        builder.HasKey(button => button.Id);
        builder.Property(button => button.Label).HasMaxLength(20).IsRequired();
        builder.Property(button => button.Order).IsRequired();
    }
}
