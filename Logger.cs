using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace GrapheneSensore.Logging
{
    /// <summary>
    /// Thread-safe file-based logger with structured logging support
    /// </summary>
    public sealed class Logger : IDisposable
    {
        private static Logger? _instance;
        private static readonly object _lock = new object();
        private readonly string _logDirectory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        private Logger()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public void LogInfo(string message, string? source = null, Dictionary<string, object>? properties = null)
        {
            _ = LogAsync(LogLevel.Info, message, source, null, properties);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void LogWarning(string message, string? source = null, Dictionary<string, object>? properties = null)
        {
            _ = LogAsync(LogLevel.Warning, message, source, null, properties);
        }

        /// <summary>
        /// Logs an error message with optional exception details
        /// </summary>
        public void LogError(string message, Exception? exception = null, string? source = null, Dictionary<string, object>? properties = null)
        {
            _ = LogAsync(LogLevel.Error, message, source, exception, properties);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public void LogDebug(string message, string? source = null, Dictionary<string, object>? properties = null)
        {
            _ = LogAsync(LogLevel.Debug, message, source, null, properties);
        }

        /// <summary>
        /// Logs a critical error message
        /// </summary>
        public void LogCritical(string message, Exception? exception = null, string? source = null, Dictionary<string, object>? properties = null)
        {
            _ = LogAsync(LogLevel.Critical, message, source, exception, properties);
        }

        /// <summary>
        /// Asynchronously writes a log entry with structured data
        /// </summary>
        private async Task LogAsync(LogLevel level, string message, string? source, Exception? exception, Dictionary<string, object>? properties)
        {
            if (_disposed)
                return;

            try
            {
                await _semaphore.WaitAsync();

                var logFileName = $"graphene-sensore-{DateTime.UtcNow:yyyyMMdd}.log";
                var logFilePath = Path.Combine(_logDirectory, logFileName);

                var logEntry = FormatLogEntry(level, message, source, exception, properties);

                await File.AppendAllTextAsync(logFilePath, logEntry);

                // Also write to debug output in development
                #if DEBUG
                System.Diagnostics.Debug.WriteLine(logEntry.TrimEnd());
                #endif
            }
            catch (Exception ex)
            {
                // Fallback to debug output if file logging fails
                System.Diagnostics.Debug.WriteLine($"[LOGGER ERROR] Failed to write log: {ex.Message}");
            }
            finally
            {
                if (!_disposed)
                    _semaphore.Release();
            }
        }

        /// <summary>
        /// Formats a log entry with all relevant information
        /// </summary>
        private string FormatLogEntry(LogLevel level, string message, string? source, Exception? exception, Dictionary<string, object>? properties)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(8);
            var sourceStr = source != null ? $"[{source}] " : "";
            
            var entry = $"[{timestamp}] [{levelStr}] {sourceStr}{message}";

            // Add properties if any
            if (properties != null && properties.Count > 0)
            {
                var propsStr = string.Join(", ", properties.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                entry += $" | Properties: {propsStr}";
            }

            // Add exception details if present
            if (exception != null)
            {
                entry += $"{Environment.NewLine}  Exception: {exception.GetType().FullName}";
                entry += $"{Environment.NewLine}  Message: {exception.Message}";
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    entry += $"{Environment.NewLine}  StackTrace: {exception.StackTrace}";
                }
                if (exception.InnerException != null)
                {
                    entry += $"{Environment.NewLine}  InnerException: {exception.InnerException.Message}";
                }
            }

            return entry + Environment.NewLine;
        }

        /// <summary>
        /// Cleans up old log files based on retention policy
        /// </summary>
        public void CleanOldLogs(int retainDays = 30)
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return;

                var files = Directory.GetFiles(_logDirectory, "*.log");
                var cutoffDate = DateTime.UtcNow.AddDays(-retainDays);
                var deletedCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTimeUtc < cutoffDate)
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete log file {file}: {ex.Message}");
                    }
                }

                if (deletedCount > 0)
                {
                    LogInfo($"Cleaned up {deletedCount} old log file(s)", "Logger");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clean old logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes of the logger resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _semaphore?.Dispose();
        }
    }

    /// <summary>
    /// Log severity levels
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}
