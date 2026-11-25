using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal sealed class TimeOfUseBandConfiguration : IEntityTypeConfiguration<TimeOfUseBand>
{
    public void Configure(EntityTypeBuilder<TimeOfUseBand> builder)
    {
        builder.ToTable("TariffTimeOfUseBands");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(TimeOfUseBand.NameMaxLength);

        // FROM and TO are reserved words in SQL; plain names save quoting in hand-written queries.
        builder.Property(b => b.From).HasColumnName("StartTime");
        builder.Property(b => b.To).HasColumnName("EndTime");

        builder.MapMoney(b => b.EnergyPricePerKwh, "EnergyPricePerKwh", Precision.PriceDecimals);
    }
}
