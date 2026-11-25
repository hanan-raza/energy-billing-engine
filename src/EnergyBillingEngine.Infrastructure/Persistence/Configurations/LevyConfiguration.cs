using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal sealed class LevyConfiguration : IEntityTypeConfiguration<Levy>
{
    public void Configure(EntityTypeBuilder<Levy> builder)
    {
        builder.ToTable("Levies");
        builder.HasKey(l => l.Id);

        // Stored as text so the table stays readable and renumbering the enum can't change history.
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(32).IsUnicode(false);
        builder.MapMoney(l => l.RatePerKwh, "RatePerKwh", Precision.PriceDecimals);

        // A billing run asks "which rate of this levy applies on day X".
        builder.HasIndex(l => new { l.Type, l.ValidFrom });
    }
}
