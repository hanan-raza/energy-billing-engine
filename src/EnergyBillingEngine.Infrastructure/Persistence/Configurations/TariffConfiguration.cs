using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("Tariffs");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(Tariff.NameMaxLength);
        builder.MapMoney(t => t.BasePricePerYear, "BasePricePerYear", Precision.PriceDecimals);
        builder.MapMoney(t => t.EnergyPricePerKwh, "EnergyPricePerKwh", Precision.PriceDecimals);
        builder.Ignore(t => t.Currency);

        // Bands are part of the tariff: they are never shared and go when the tariff goes.
        builder.HasMany(t => t.TimeOfUseBands)
            .WithOne()
            .HasForeignKey("TariffId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
