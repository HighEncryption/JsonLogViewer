namespace JsonLog
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using JetBrains.Annotations;

    public static class Logger
    {
        private static readonly List<ILogWriter> LogWriters = new List<ILogWriter>();

        private static bool isShutdownStarted;

        [StringFormatMethod("message")]
        public static void Error(string message, params object[] args)
        {
            LogInternal(CreateLogEntry("ERROR", message, args));
        }

        [StringFormatMethod("message")]
        public static void Info(string message, params object[] args)
        {
            LogInternal(CreateLogEntry("INFO", message, args));
        }

        [StringFormatMethod("message")]
        public static void Warning(string message, params object[] args)
        {
            LogInternal(CreateLogEntry("WARN", message, args));
        }

        [StringFormatMethod("message")]
        public static void Verbose(string message, params object[] args)
        {
            LogInternal(CreateLogEntry("VERB", message, args));
        }

        [StringFormatMethod("message")]
        public static void Debug(string message, params object[] args)
        {
            LogInternal(CreateLogEntry("DEBUG", message, args));
        }

        public static void LogException(Exception exception)
        {
            LogInternal(CreateLogEntry("ERROR", exception.ToString()));
        }

        public static void AddLogWriter(ILogWriter logWriter)
        {
            if (logWriter == null)
            {
                throw new ArgumentNullException(nameof(logWriter));
            }

            if (!logWriter.IsInitialized)
            {
                throw new Exception(
                    string.Format("Log writer {0} is not initialized.", logWriter.GetType().Name));
            }

            if (LogWriters.Contains(logWriter))
            {
                return;
            }

            LogWriters.Add(logWriter);
        }

        public static void RemoveLogWriter(ILogWriter logWriter)
        {
            if (logWriter == null)
            {
                throw new ArgumentNullException(nameof(logWriter));
            }

            LogWriters.Remove(logWriter);
        }

        public static void Shutdown()
        {
            isShutdownStarted = true;

            foreach (ILogWriter logWriter in LogWriters)
            {
                logWriter.Shutdown();
            }
        }

        private static void LogInternal(LogEntry logEntry)
        {
            if (isShutdownStarted)
            {
                return;
            }

            foreach (ILogWriter logWriter in LogWriters.Where(writer => !writer.IsFaulted))
            {
                try
                {
                    logWriter.LogInternal(logEntry);
                }
                catch (Exception exception)
                {
                    // If an exception is caught while logging the entry to the specific logwriter,
                    // then that logwriter is considered faulted and will be skipped. An error will 
                    // be logged so that any remaining loggers can log the error.
                    logWriter.IsFaulted = true;

                    Error("LogWriter {0} is faulted", logWriter.GetType().FullName);
                    LogException(exception);
                }
            }
        }

        [StringFormatMethod("message")]
        private static LogEntry CreateLogEntry(string level, string message, params object[] args)
        {
            return new LogEntry
            {
                Level = level,
                Message = string.Format(CultureInfo.CurrentCulture, message, args),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                Timestamp = DateTime.Now
            };
        }
    }
}