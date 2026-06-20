using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ArenaInfrastructure.AI
{
    internal sealed class FlexibleIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
                return Math.Max(0, number);

            if (reader.TokenType == JsonTokenType.String)
                return Math.Max(0, ParseInt(reader.GetString()));

            return 0;
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);

        private static int ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var match = Regex.Match(value, @"\d+");
            if (!match.Success)
                return 0;

            return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0;
        }
    }

    internal sealed class FlexibleDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var number))
                return Math.Max(0, number);

            if (reader.TokenType == JsonTokenType.String)
                return Math.Max(0, ParseDecimal(reader.GetString()));

            return 0;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);

        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var match = Regex.Match(value, @"\d+(\.\d+)?");
            if (!match.Success)
                return 0;

            return decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0;
        }
    }
}
