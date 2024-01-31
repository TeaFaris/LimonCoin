using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimonCoin.Data.Migrations
{
    /// <inheritdoc />
    public partial class boosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClickerLevel",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnergyCapacityLevel",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnergyRecoveryLevel",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferrerId",
                table: "Users",
                column: "ReferrerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ReferrerId",
                table: "Users",
                column: "ReferrerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ReferrerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ReferrerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClickerLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EnergyCapacityLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EnergyRecoveryLevel",
                table: "Users");
        }
    }
}
