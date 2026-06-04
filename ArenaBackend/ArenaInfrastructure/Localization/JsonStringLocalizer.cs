using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArenaInfrastructure.Localization
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly JsonSerializer _serializer = new();
        private static readonly string _resourcesPath;

        static JsonStringLocalizer()
        {
            var assemblyLocation = Path.GetDirectoryName(typeof(JsonStringLocalizer).Assembly.Location);
            _resourcesPath = Path.Combine(assemblyLocation, "Resources");
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = GetString(name);
                return new LocalizedString(name, value ?? name, value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var actualValue = this[name];
                return !actualValue.ResourceNotFound
                    ? new LocalizedString(name, string.Format(actualValue.Value, arguments))
                    : actualValue;
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var filePath = GetFilePath();
            if (!File.Exists(filePath))
                yield break;

            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader sr = new(stream);
            using JsonTextReader reader = new(sr);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var key = reader.Value as string;
                    reader.Read();
                    var value = _serializer.Deserialize<string>(reader);
                    if (key != null)
                        yield return new LocalizedString(key, value, false);
                }
            }
        }

        private string? GetString(string key)
        {
            var filePath = GetFilePath();
            if (File.Exists(filePath))
            {
                return GetValueFromJson(key, filePath);
            }
            return null;
        }

        private string? GetValueFromJson(string propertyName, string filePath)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(filePath))
                return null;

            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader sr = new(stream);
            using JsonTextReader reader = new(sr);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value as string == propertyName)
                {
                    reader.Read();
                    return _serializer.Deserialize<string>(reader);
                }
            }

            return null;
        }

        private static string GetFilePath()
        {
            return Path.Combine(_resourcesPath, $"{Thread.CurrentThread.CurrentCulture.Name}.json");
        }
    }
}
