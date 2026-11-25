using System.Linq.Expressions;
using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyBillingEngine.Infrastructure.Persistence.Configurations;

internal static class MoneyMapping
{
    /// <summary>
    /// Maps a <see cref="Money"/> property to two columns, <c>{column}</c> and <c>{column}Currency</c>.
    /// </summary>
    /// <remarks>
    /// A complex type, not an owned entity: Money has no identity and is always loaded with its owner,
    /// so it needs neither a key nor a join.
    /// </remarks>
    public static void MapMoney<TEntity>(
        this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, Money?>> property, string column, int decimals)
        where TEntity : class
    {
        builder.ComplexProperty(property, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName(column)
                .HasPrecision(Precision.Digits, decimals);
            money.Property(m => m.Currency)
                .HasColumnName(column + "Currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsUnicode(false);
        });
    }
}
