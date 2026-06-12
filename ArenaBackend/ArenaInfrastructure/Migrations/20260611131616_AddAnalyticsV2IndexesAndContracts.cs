using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsV2IndexesAndContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_MemberProfileId",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_CreatedAt",
                table: "UserSubscriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Status_EndDate",
                table: "UserSubscriptions",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentDate",
                table: "Payments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_PaymentDate",
                table: "Payments",
                columns: new[] { "Status", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingDate",
                table: "Bookings",
                column: "BookingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingDate_Status",
                table: "Bookings",
                columns: new[] { "BookingDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckInTime",
                table: "Attendances",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MemberProfileId_CheckInTime",
                table: "Attendances",
                columns: new[] { "MemberProfileId", "CheckInTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_CreatedAt",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_Status_EndDate",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingDate",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingDate_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckInTime",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_MemberProfileId_CheckInTime",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MemberProfileId",
                table: "Attendances",
                column: "MemberProfileId");
        }
    }
}
