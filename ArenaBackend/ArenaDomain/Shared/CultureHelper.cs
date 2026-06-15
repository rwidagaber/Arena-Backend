using System.Globalization;

namespace ArenaDomain.Shared
{
    public static class CultureHelper
    {
        public static bool IsArabic =>
            CultureInfo.CurrentUICulture.Name.StartsWith("ar");

        public static string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2", CultureInfo.CurrentUICulture);
        }

        public static string FormatDecimal(decimal value)
        {
            return value.ToString("N2", CultureInfo.CurrentUICulture);
        }

        public static string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        public static string FormatDateLong(DateTime date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }
}
