using System;
using System.IO;
using System.Threading;

namespace SpaceTrans
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public enum LogOutput
    {
        File,
        Console,
        Both
    }

    public class Logger
    {
        private static readonly object lockObject = new();
        private static Logger instance;
        private readonly string logFilePath;
        private readonly LogLevel minLogLevel;
        private readonly LogOutput logOutput;

        private Logger(string logFilePath, LogLevel minLogLevel = LogLevel.Info, LogOutput logOutput = LogOutput.File)
        {
            this.logFilePath = logFilePath;
            this.minLogLevel = minLogLevel;
            this.logOutput = logOutput;
            
            // Ensure log directory exists if using file output
            if (logOutput != LogOutput.Console && !string.IsNullOrEmpty(logFilePath))
            {
                var logDirectory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
        }

        public static Logger Instance => instance ?? throw new InvalidOperationException("Logger not initialized. Call Initialize() first.");

        public static void Initialize(string logFilePath, LogLevel minLogLevel = LogLevel.Info, LogOutput logOutput = LogOutput.File)
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new Logger(logFilePath, minLogLevel, logOutput);
                    }
                }
            }
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < minLogLevel) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var logEntry = $"[{timestamp}] [{level}] [T{threadId}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception}";
            }

            lock (lockObject)
            {
                try
                {
                    // 输出到控制台
                    if (logOutput == LogOutput.Console || logOutput == LogOutput.Both)
                    {
                        WriteToConsole(level, logEntry);
                    }

                    // 输出到文件
                    if ((logOutput == LogOutput.File || logOutput == LogOutput.Both) && !string.IsNullOrEmpty(logFilePath))
                    {
                        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                    }
                }
                catch
                {
                    // Swallow logging errors to prevent application crashes
                }
            }
        }

        private void WriteToConsole(LogLevel level, string logEntry)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                // 根据日志级别设置颜色
                Console.ForegroundColor = level switch
                {
                    LogLevel.Debug => ConsoleColor.Gray,
                    LogLevel.Info => ConsoleColor.White,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine(logEntry);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);

        public void RotateLogIfNeeded(long maxSizeBytes = 10 * 1024 * 1024) // 10MB default
        {
            try
            {
                if ((logOutput == LogOutput.File || logOutput == LogOutput.Both) && 
                    !string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    if (fileInfo.Length > maxSizeBytes)
                    {
                        var backupPath = logFilePath + ".old";
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Move(logFilePath, backupPath);
                        Info("Log file rotated due to size limit");
                    }
                }
            }
            catch
            {
                // Swallow rotation errors
            }
        }
    }

    public static class LoggerExtensions
    {
        public static void LogTranslationStart(this Logger logger, string sourceText)
        {
            logger.Info($"Translation started: '{sourceText?.Substring(0, Math.Min(sourceText?.Length ?? 0, 50))}...'");
        }

        public static void LogTranslationSuccess(this Logger logger, string sourceText, string translatedText, string engine)
        {
            logger.Info($"Translation success [{engine}]: '{sourceText?.Substring(0, Math.Min(sourceText?.Length ?? 0, 30))}...' -> '{translatedText?.Substring(0, Math.Min(translatedText?.Length ?? 0, 30))}...'");
        }

        public static void LogTranslationError(this Logger logger, string sourceText, string engine, Exception exception)
        {
            logger.Error($"Translation failed [{engine}]: '{sourceText?.Substring(0, Math.Min(sourceText?.Length ?? 0, 30))}...'", exception);
        }

        public static void LogHotkeyEvent(this Logger logger, string action)
        {
            logger.Debug($"Hotkey event: {action}");
        }

        public static void LogEngineEvent(this Logger logger, string engine, string action)
        {
            logger.Info($"Engine [{engine}]: {action}");
        }
    }
}