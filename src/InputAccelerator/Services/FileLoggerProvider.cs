using System.Text;
using Microsoft.Extensions.Logging;

namespace InputAccelerator.Services;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private const long MaximumFileSize = 1_048_576;
    private readonly string _filePath;
    private readonly LogLevel _minimumLevel;
    private readonly object _sync = new();

    public FileLoggerProvider(string filePath, LogLevel minimumLevel = LogLevel.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _minimumLevel = minimumLevel;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(this, categoryName);
    }

    public void Dispose()
    {
    }

    private void Write(string message)
    {
        lock (_sync)
        {
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(_filePath) && new FileInfo(_filePath).Length > MaximumFileSize)
                {
                    var backupPath = _filePath + ".1";
                    File.Move(_filePath, backupPath, overwrite: true);
                }

                File.AppendAllText(_filePath, message + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        public FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _provider._minimumLevel && logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);
            var builder = new StringBuilder();
            builder.Append(DateTimeOffset.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(" [");
            builder.Append(logLevel);
            builder.Append("] ");
            builder.Append(_categoryName);
            builder.Append(": ");
            builder.Append(formatter(state, exception));
            if (exception is not null)
            {
                builder.AppendLine();
                builder.Append(exception);
            }

            _provider.Write(builder.ToString());
        }
    }
}
