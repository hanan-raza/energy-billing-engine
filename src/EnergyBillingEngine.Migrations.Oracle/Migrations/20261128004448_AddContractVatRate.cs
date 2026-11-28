using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyBillingEngine.Migrations.Oracle.Migrations
{
    /// <inheritdoc />
    public partial class AddContractVatRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRatePercent",
                table: "Contracts",
                type: "DECIMAL(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                // Backfills existing contracts with the standard rate; EF proposed 0, which would bill them without VAT.
                // The model has no default, so new contracts always store their own rate (0 % included).
                defaultValue: 19m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatRatePercent",
                table: "Contracts");
        }
    }
}
