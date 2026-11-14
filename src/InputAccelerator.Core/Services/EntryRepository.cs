using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public sealed class EntryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public EntryRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<TextEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<TextEntry>();
        }

        try
        {
            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var entries = await JsonSerializer.DeserializeAsync<List<TextEntry>>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return Normalize(entries);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            TryBackupCorruptFile();
            return Array.Empty<TextEntry>();
        }
    }

    public async Task SaveAsync(IEnumerable<TextEntry> entries, CancellationToken cancellationToken = default)
    {
        var snapshot = Normalize(entries);

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = _filePath + ".tmp";
            try
            {
                await using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken)
                        .ConfigureAwait(false);
                }

                File.Move(temporaryPath, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static IReadOnlyList<TextEntry> Normalize(IEnumerable<TextEntry>? entries)
    {
        if (entries is null)
        {
            return Array.Empty<TextEntry>();
        }

        var usedIds = new HashSet<Guid>();
        var result = new List<TextEntry>();
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            var kind = Enum.IsDefined(entry.Kind) ? entry.Kind : EntryKind.Text;
            if (kind == EntryKind.Hotkey)
            {
                if (!HotkeyParser.TryParse(entry.Hotkey, out var chord))
                {
                    continue;
                }

                var label = NormalizeLabel(entry.Label);
                var id = entry.Id == Guid.Empty || !usedIds.Add(entry.Id) ? Guid.NewGuid() : entry.Id;
                usedIds.Add(id);
                result.Add(new TextEntry
                {
                    Id = id,
                    Kind = EntryKind.Hotkey,
                    Hotkey = HotkeyParser.Format(chord),
                    Label = label,
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Content))
            {
                continue;
            }

            var textId = entry.Id == Guid.Empty || !usedIds.Add(entry.Id) ? Guid.NewGuid() : entry.Id;
            usedIds.Add(textId);
            result.Add(new TextEntry
            {
                Id = textId,
                Kind = EntryKind.Text,
                Content = entry.Content,
            });
        }

        return result;
    }

    private static string? NormalizeLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        return label.Trim();
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            var backupPath = string.Concat(
                _filePath,
                ".bad-",
                DateTimeOffset.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture));
            File.Move(_filePath, backupPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
