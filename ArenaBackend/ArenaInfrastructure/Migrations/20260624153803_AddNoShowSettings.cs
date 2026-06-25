using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoShowSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoShowPenalized",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GymSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoShowThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GymSettings",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "IsDeleted", "NoShowThreshold", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, 2, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymSettings");

            migrationBuilder.DropColumn(
                name: "NoShowPenalized",
                table: "Bookings");
        }
    }
}
