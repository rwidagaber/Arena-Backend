using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DescriptionAr", "DescriptionEn", "DurationMonths", "IsActive", "IsDeleted", "NameAr", "NameEn", "Price", "SessionLimit", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "مثالي للمبتدئين للبدء في اللياقة البدنية", "Perfect for beginners to get started with fitness", 1, true, false, "أساسي", "Basic", 9.99m, 4, null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "الوصول الكامل إلى جميع المرافق والفئات المتميزة", "Full access to all facilities and premium classes", 3, true, false, "بريميوم", "Premium", 24.99m, 12, null, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "وصول غير محدود مع جلسات المدرب الشخصي", "Unlimited access with personal trainer sessions", 12, true, false, "نخبة", "Elite", 79.99m, null, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
