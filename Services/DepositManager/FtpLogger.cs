using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SmkcApi.Services.DepositManager
{
    /// <summary>
    /// Dedicated logger for FTP operations that writes to a log file in the published folder.
    /// Thread-safe implementation with automatic log rotation.
    /// </summary>
    public static class FtpLogger
    {
        private static readonly object _lockObject = new object();
        private static string _logDirectory;
        private static string _logFilePath;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the logger with the log directory path.
        /// Call this once during application startup.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Get the application's base directory (published folder)
                _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
                
                // Create Logs subdirectory if it doesn't exist
                var logsPath = Path.Combine(_logDirectory, "Logs");
                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }

                // Set log file path with today's date
                _logFilePath = Path.Combine(logsPath, string.Format("FtpLog_{0:yyyyMMdd}.txt", DateTime.Now));
                
                _isInitialized = true;

                // Write initialization message
                LogInfo("=== FTP Logger Initialized ===");
                LogInfo("Log Directory: " + logsPath);
                LogInfo("Log File: " + Path.GetFileName(_logFilePath));
            }
            catch (Exception ex)
            {
                // Fallback to System.Diagnostics.Trace if file logging fails
                System.Diagnostics.Trace.TraceError("Failed to initialize FTP logger: " + ex.Message);
            }
        }

        /// <summary>
        /// Log an informational message (success, status updates)
        /// </summary>
        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Log a warning message (non-critical issues)
        /// </summary>
        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// Log an error message (failures, exceptions)
        /// </summary>
        public static void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Log an error with exception details
        /// </summary>
        public static void LogError(string message, Exception ex)
        {
            var fullMessage = string.Format("{0}\nException: {1}\nStack Trace: {2}", 
                message, ex.Message, ex.StackTrace);
            WriteLog("ERROR", fullMessage);
        }

        /// <summary>
        /// Log a step in the FTP upload process
        /// </summary>
        public static void LogStep(int stepNumber, string description, bool success, string details = null)
        {
            var stepMessage = new StringBuilder();
            stepMessage.AppendFormat("STEP {0}: {1}", stepNumber, description);
            
            if (success)
            {
                stepMessage.Append(" - ? SUCCESS");
            }
            else
            {
                stepMessage.Append(" - ? FAILED");
            }

            if (!string.IsNullOrEmpty(details))
            {
                stepMessage.AppendLine();
                stepMessage.Append("  Details: " + details);
            }

            WriteLog(success ? "INFO" : "ERROR", stepMessage.ToString());
        }

        /// <summary>
        /// Core method to write log entries to file
        /// </summary>
        private static void WriteLog(string level, string message)
        {
            // Ensure logger is initialized
            if (!_isInitialized)
            {
                Initialize();
            }

            try
            {
                lock (_lockObject)
                {
                    // Check if we need to rotate log (new day)
                    var currentLogFile = Path.Combine(
                        Path.GetDirectoryName(_logFilePath), 
                        string.Format("FtpLog_{0:yyyyMMdd}.txt", DateTime.Now));
                    
                    if (currentLogFile != _logFilePath)
                    {
                        _logFilePath = currentLogFile;
                        LogInfo("=== Log rotated to new day ===");
                    }

                    // Format log entry
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = string.Format("[{0}] [{1}] {2}\n", timestamp, level.PadRight(5), message);

                    // Write to file
                    File.AppendAllText(_logFilePath, logEntry, Encoding.UTF8);

                    // Also write to System.Diagnostics.Trace for debugging
                    if (level == "ERROR")
                    {
                        System.Diagnostics.Trace.TraceError(message);
                    }
                    else if (level == "WARN")
                    {
                        System.Diagnostics.Trace.TraceWarning(message);
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceInformation(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // If file logging fails, at least log to System.Diagnostics.Trace
                System.Diagnostics.Trace.TraceError(
                    string.Format("FTP Logger failed to write to file: {0}. Original message: {1}", 
                        ex.Message, message));
            }
        }

        /// <summary>
        /// Get the current log file path (for diagnostics)
        /// </summary>
        public static string GetLogFilePath()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            return _logFilePath;
        }

        /// <summary>
        /// Check if the logger is initialized
        /// </summary>
        public static bool IsInitialized
        {
            get { return _isInitialized; }
        }
    }
}
