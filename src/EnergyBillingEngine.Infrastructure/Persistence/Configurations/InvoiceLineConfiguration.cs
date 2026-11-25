using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(l => l.Id);

        builder.HasIndex("InvoiceId", nameof(InvoiceLine.Position)).IsUnique();

        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        builder.Property(l => l.Description).HasMaxLength(InvoiceLine.DescriptionMaxLength);
        builder.Property(l => l.Quantity).HasPrecision(Precision.Digits, Precision.QuantityDecimals);
        builder.Property(l => l.Unit).HasMaxLength(InvoiceLine.UnitMaxLength);

        builder.MapMoney(l => l.UnitPrice, "UnitPrice", Precision.PriceDecimals);
        builder.MapMoney(l => l.Amount, "Amount", Precision.AmountDecimals);
    }
}
