using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenciadorDeFinancas.Persistence.Configurations;

public sealed class NotificationButtonPersonConfiguration : IEntityTypeConfiguration<NotificationButtonPerson>
{
    public void Configure(EntityTypeBuilder<NotificationButtonPerson> builder)
    {
        builder.ToTable("NotificationButtonPersons");
        builder.HasKey(bp => new { bp.ButtonId, bp.PersonId });
        builder.HasOne(bp => bp.Button)
            .WithMany(button => button.Persons)
            .HasForeignKey(bp => bp.ButtonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bp => bp.Person)
            .WithMany()
            .HasForeignKey(bp => bp.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
