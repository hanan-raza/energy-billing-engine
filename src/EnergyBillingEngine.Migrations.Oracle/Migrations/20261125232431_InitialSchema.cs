using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyBillingEngine.Migrations.Oracle.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CustomerNumber = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Levies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Type = table.Column<string>(type: "VARCHAR2(32)", unicode: false, maxLength: 32, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "DATE", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "DATE", nullable: true),
                    RatePerKwh = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    RatePerKwhCurrency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tariffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    BasePricePerYear = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    BasePricePerYearCurrency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    EnergyPricePerKwh = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    EnergyPricePerKwhCurrency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tariffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ContractNumber = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    MarketLocationId = table.Column<string>(type: "CHAR(11)", unicode: false, fixedLength: true, maxLength: 11, nullable: false),
                    TariffId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "DATE", nullable: false),
                    EndDate = table.Column<DateTime>(type: "DATE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Tariffs_TariffId",
                        column: x => x.TariffId,
                        principalTable: "Tariffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TariffTimeOfUseBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "INTERVAL DAY(0) TO SECOND(0)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "INTERVAL DAY(0) TO SECOND(0)", nullable: false),
                    TariffId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    EnergyPricePerKwh = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    EnergyPricePerKwhCurrency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TariffTimeOfUseBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TariffTimeOfUseBands_Tariffs_TariffId",
                        column: x => x.TariffId,
                        principalTable: "Tariffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    ContractId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Currency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "DATE", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Position = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Type = table.Column<string>(type: "VARCHAR2(16)", unicode: false, maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "DECIMAL(18,5)", precision: 18, scale: 5, nullable: false),
                    Unit = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountCurrency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "DECIMAL(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "CHAR(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractNumber",
                table: "Contracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CustomerId",
                table: "Contracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_MarketLocationId_StartDate",
                table: "Contracts",
                columns: new[] { "MarketLocationId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TariffId",
                table: "Contracts",
                column: "TariffId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerNumber",
                table: "Customers",
                column: "CustomerNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId_Position",
                table: "InvoiceLines",
                columns: new[] { "InvoiceId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ContractId",
                table: "Invoices",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Levies_Type_ValidFrom",
                table: "Levies",
                columns: new[] { "Type", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_TariffTimeOfUseBands_TariffId",
                table: "TariffTimeOfUseBands",
                column: "TariffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "Levies");

            migrationBuilder.DropTable(
                name: "TariffTimeOfUseBands");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Tariffs");
        }
    }
}
