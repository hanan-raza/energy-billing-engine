using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContractNumber).HasMaxLength(Contract.ContractNumberMaxLength).IsUnicode(false);
        builder.HasIndex(c => c.ContractNumber).IsUnique();

        builder.Property(c => c.MarketLocationId)
            .HasMaxLength(MarketLocationId.Length)
            .IsFixedLength()
            .IsUnicode(false);

        // "Which contract supplies this MaLo on day X" is the lookup of every billing run.
        builder.HasIndex(c => new { c.MarketLocationId, c.StartDate });

        builder.HasOne(c => c.Tariff)
            .WithMany()
            .HasForeignKey(c => c.TariffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
