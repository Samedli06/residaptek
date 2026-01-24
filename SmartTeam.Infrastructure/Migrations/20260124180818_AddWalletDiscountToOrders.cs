using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletDiscountToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WalletDiscount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WalletDiscount",
                table: "Orders");
        }
    }
}
