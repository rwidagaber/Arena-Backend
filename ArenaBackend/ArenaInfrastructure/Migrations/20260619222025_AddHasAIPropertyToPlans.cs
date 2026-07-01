using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHasAIPropertyToPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAI",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Price" },
                values: new object[] { "دخول أساسي لصالة الألعاب الرياضية وحجز الحصص", "Essential access to gym facilities and class bookings", 400.00m });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "NameAr", "NameEn", "Price" },
                values: new object[] { "دخول قياسي ممتد مع توفير في التكلفة", "Extended standard access with savings", "برو", "Pro", 1000.00m });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "DurationMonths", "NameAr", "NameEn", "Price", "SessionLimit" },
                values: new object[] { "دخول متوسط المدى للرياضيين المستمرين", "Mid-term gym access for consistent athletes", 6, "بريميوم", "Premium", 1800.00m, 24 });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DescriptionAr", "DescriptionEn", "DurationMonths", "IsActive", "IsDeleted", "NameAr", "NameEn", "Price", "SessionLimit", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "دخول أساسي لصالة الألعاب الرياضية لمدة عام كامل", "Full year essential gym access", 12, true, false, "ماكس", "Max", 3000.00m, null, null, null });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DescriptionAr", "DescriptionEn", "DurationMonths", "HasAI", "IsActive", "IsDeleted", "NameAr", "NameEn", "Price", "SessionLimit", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "اشتراك لمدة شهر مع توجيه كامل من مدرب الذكاء الاصطناعي", "1 Month of full fitness access + AI Coach guidance", 1, true, true, false, "أساسي ذكي", "Basic AI", 700.00m, 4, null, null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "اشتراك لمدة 3 أشهر مع مدرب الذكاء الاصطناعي (الأكثر شعبية)", "3 Months of full access + AI Coach (Most Popular)", 3, true, true, false, "برو ذكي", "Pro AI", 1800.00m, 12, null, null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "اشتراك لمدة 6 أشهر مع توجيه كامل من مدرب الذكاء الاصطناعي", "6 Months of full access + AI Coach guidance", 6, true, true, false, "بريميوم ذكي", "Premium AI", 3200.00m, 24, null, null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc), null, null, "سنة كاملة من اللياقة المخصصة مع مدرب الذكاء الاصطناعي (أفضل قيمة)", "1 Year of complete personalized fitness (Best Value)", 12, true, true, false, "ماكس ذكي", "Max AI", 5500.00m, null, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DropColumn(
                name: "HasAI",
                table: "SubscriptionPlans");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Price" },
                values: new object[] { "مثالي للمبتدئين للبدء في اللياقة البدنية", "Perfect for beginners to get started with fitness", 9.99m });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "NameAr", "NameEn", "Price" },
                values: new object[] { "الوصول الكامل إلى جميع المرافق والفئات المتميزة", "Full access to all facilities and premium classes", "بريميوم", "Premium", 24.99m });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "DurationMonths", "NameAr", "NameEn", "Price", "SessionLimit" },
                values: new object[] { "وصول غير محدود مع جلسات المدرب الشخصي", "Unlimited access with personal trainer sessions", 12, "نخبة", "Elite", 79.99m, null });
        }
    }
}
