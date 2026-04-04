using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxiCostToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxiCost",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "GlobalSettings",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 27, 12, 19, 57, 850, DateTimeKind.Utc).AddTicks(4371));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxiCost",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "GlobalSettings",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 26, 19, 21, 48, 803, DateTimeKind.Utc).AddTicks(6043));
        }
    }
}
