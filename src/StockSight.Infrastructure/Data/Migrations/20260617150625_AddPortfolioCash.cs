using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSight.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashBalance",
                table: "Portfolios",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialCash",
                table: "Portfolios",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashBalance",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "InitialCash",
                table: "Portfolios");
        }
    }
}
