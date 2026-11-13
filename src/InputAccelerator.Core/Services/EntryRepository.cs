using System.Text.Json;
using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public sealed class EntryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
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
        var snapshot = entries
            .Where(static entry => entry is not null && !string.IsNullOrWhiteSpace(entry.Content))
            .Select(static entry => new TextEntry
            {
                Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
                Content = entry.Content,
            })
            .ToList();

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
            if (entry is null || string.IsNullOrWhiteSpace(entry.Content))
            {
                continue;
            }

            var id = entry.Id == Guid.Empty || !usedIds.Add(entry.Id) ? Guid.NewGuid() : entry.Id;
            usedIds.Add(id);
            result.Add(new TextEntry
            {
                Id = id,
                Content = entry.Content,
            });
        }

        return result;
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
