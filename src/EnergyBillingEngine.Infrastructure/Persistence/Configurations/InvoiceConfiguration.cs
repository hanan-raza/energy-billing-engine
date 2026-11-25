using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(Invoice.InvoiceNumberMaxLength).IsUnicode(false);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false);

        builder.ComplexProperty(i => i.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("PeriodStart");
            period.Property(p => p.End).HasColumnName("PeriodEnd");
        });

        builder.Ignore(i => i.NetTotal);

        builder.HasOne(i => i.Contract)
            .WithMany()
            .HasForeignKey(i => i.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey("InvoiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
