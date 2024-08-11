using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRateManager.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForexRates",
                columns: table => new
                {
                    FromCurrencyCode = table.Column<string>(type: "text", nullable: false),
                    ToCurrencyCode = table.Column<string>(type: "text", nullable: false),
                    FromCurrencyName = table.Column<string>(type: "text", nullable: false),
                    ToCurrencyName = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LastRefreshed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BidPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    AskPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForexRates", x => new { x.FromCurrencyCode, x.ToCurrencyCode });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForexRates");
        }
    }
}
