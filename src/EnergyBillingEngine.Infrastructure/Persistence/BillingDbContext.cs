using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnergyBillingEngine.Infrastructure.Persistence;

/// <summary>EF Core context of the billing store.</summary>
/// <param name="options">Built with <see cref="DependencyInjection.UseBillingStore{TBuilder}"/>.</param>
public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    /// <summary>End customers (Endkunden).</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Supply contracts (Lieferverträge), including ended ones.</summary>
    public DbSet<Contract> Contracts => Set<Contract>();

    /// <summary>Tariffs with their time-of-use bands.</summary>
    public DbSet<Tariff> Tariffs => Set<Tariff>();

    /// <summary>Levy, tax and network charge rates with their validity.</summary>
    public DbSet<Levy> Levies => Set<Levy>();

    /// <summary>Invoices (Rechnungen) with their lines.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Applies the <c>IEntityTypeConfiguration</c> classes of this assembly.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
}
