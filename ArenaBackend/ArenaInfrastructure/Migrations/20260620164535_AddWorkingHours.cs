using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkingHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingHours", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "WorkingHours",
                columns: new[] { "Id", "CloseTime", "CreatedAt", "CreatedBy", "DayOfWeek", "DeletedAt", "IsDeleted", "OpenTime", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Monday", null, false, new TimeSpan(0, 8, 0, 0, 0), null, null },
                    { 2, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Tuesday", null, false, new TimeSpan(0, 8, 0, 0, 0), null, null },
                    { 3, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Wednesday", null, false, new TimeSpan(0, 8, 0, 0, 0), null, null },
                    { 4, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Thursday", null, false, new TimeSpan(0, 8, 0, 0, 0), null, null },
                    { 5, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Friday", null, false, new TimeSpan(0, 15, 0, 0, 0), null, null },
                    { 6, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Saturday", null, false, new TimeSpan(0, 8, 0, 0, 0), null, null },
                    { 7, new TimeSpan(0, 3, 0, 0, 0), new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sunday", null, false, new TimeSpan(0, 8, 0, 0, 0), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkingHours");
        }
    }
}
