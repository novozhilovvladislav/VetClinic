using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VetClinic.Persistence;

/// <summary>Налаштування серіалізації, спільні для всіх файлів даних.</summary>
internal static class JsonStorageOptions
{
    public static readonly JsonSerializerOptions Default = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            // Читабельний файл, який можна відкрити й переглянути в текстовому редакторі.
            WriteIndented = true,

            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            // Порожні (null) значення у файл не пишемо.
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
