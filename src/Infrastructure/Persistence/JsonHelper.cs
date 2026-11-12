using System;
using System.Text.Json;

namespace Glance
{
    /// <summary>
    /// Helper serialisasi dan deserialisasi JSON untuk penyimpanan konfigurasi dan data lokal.
    /// </summary>
    public class JsonHelper
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }

        public T Deserialize<T>(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return default(T);
            try
            {
                return JsonSerializer.Deserialize<T>(text, Options);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
