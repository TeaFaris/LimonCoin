using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LimonCoin.Data.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    Coins = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CoinsThisDay = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CoinsThisWeek = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LastTimeClicked = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CoinsPerClick = table.Column<long>(type: "bigint", nullable: false),
                    Energy = table.Column<long>(type: "bigint", nullable: false),
                    EnergyPerSecond = table.Column<long>(type: "bigint", nullable: false),
                    MaxEnergy = table.Column<long>(type: "bigint", nullable: false),
                    ReferrerId = table.Column<int>(type: "integer", nullable: true),
                    CompletedTasks = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    AwardedTasks = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
