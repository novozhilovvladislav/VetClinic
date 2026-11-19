using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VetClinic.Persistence;

/// <summary>Налаштування серіалізації, спільні для всіх файлів даних </summary>
internal static class JsonStorageOptions
{
    public static readonly JsonSerializerOptions Default = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            // Читабельний файл
            WriteIndented = true,

            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            // Порожні значення у файл не пишуться
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
