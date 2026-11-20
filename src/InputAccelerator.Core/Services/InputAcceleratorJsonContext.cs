using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(List<TextEntry>))]
[JsonSerializable(typeof(IReadOnlyList<TextEntry>))]
internal sealed partial class InputAcceleratorJsonContext : JsonSerializerContext
{
    internal static InputAcceleratorJsonContext Settings { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter<InputMode>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<AppTheme>(JsonNamingPolicy.CamelCase),
        },
    });

    internal static InputAcceleratorJsonContext Entries { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter<EntryKind>(JsonNamingPolicy.CamelCase),
        },
    });
}
