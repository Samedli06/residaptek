using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyNameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PharmacyName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PharmacyName",
                table: "Users");
        }
    }
}
