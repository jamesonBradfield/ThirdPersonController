using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GodotTools
{
    /// <summary>
    /// A global logger for Godot-Mono that automatically tracks caller information.
    /// Only works in DEBUG mode.
    /// </summary>
    public partial class GodotLogger : Node
    {
        // Singleton instance
        private static GodotLogger _instance;

        // Log levels
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        // Log entry structure
        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public string ScriptName { get; set; }
            public string MethodName { get; set; }
            public int LineNumber { get; set; }
            public string FormattedMessage { get; set; }
            public string PlainMessage { get; set; }
        }

        // Configuration
        private LogLevel _minimumLogLevel = LogLevel.Debug;
        private bool _logToFile = false;
        private string _logFilePath = "user://godot_log.txt";
        private bool _includeTimestamp = true;

        // Log storage
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private HashSet<string> _scriptNames = new HashSet<string>();
        private int _maxLogEntries = 1000;

        // Events for UI updates
        public static event Action<LogEntry> LogAdded;
        public static event Action<string> ScriptAdded;

        // Get the singleton instance
        public static GodotLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    GD.PushError("GodotLogger is not initialized. Call Initialize() first.");
                }
                return _instance;
            }
        }
        /// <summary>
        /// Initialize the logger - must be called once from the main scene or autoload
        /// </summary>
        public static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new GodotLogger();
                var tree = Engine.GetMainLoop() as SceneTree;
                if (tree != null)
                {
                    tree.Root.CallDeferred("add_child", _instance);
                }
                Debug("GodotLogger initialized");
            }
        }
        /// <summary>
        /// Called when the node enters the scene tree, handles self-initialization
        /// </summary>
        public override void _Ready()
        {
            if (_instance == null)
            {
                _instance = this;
                Debug("GodotLogger self-initialized");
            }
        }
        /// <summary>
        /// Log a debug message with automatically captured caller information
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Instance._minimumLogLevel <= LogLevel.Debug)
            {
                string callerInfo = GetCallerInfo(memberName, sourceFilePath, sourceLineNumber);
                Instance.LogMessage(LogLevel.Debug, message, callerInfo);
            }
        }
        /// <summary>
        /// Log an info message with automatically captured caller information
        /// </summary>
        [Conditional("DEBUG")]
        public static void Info(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Instance._minimumLogLevel <= LogLevel.Info)
            {
                string callerInfo = GetCallerInfo(memberName, sourceFilePath, sourceLineNumber);
                Instance.LogMessage(LogLevel.Info, message, callerInfo);
            }
        }
        /// <summary>
        /// Log a warning message with automatically captured caller information
        /// </summary>
        [Conditional("DEBUG")]
        public static void Warning(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Instance._minimumLogLevel <= LogLevel.Warning)
            {
                string callerInfo = GetCallerInfo(memberName, sourceFilePath, sourceLineNumber);
                Instance.LogMessage(LogLevel.Warning, message, callerInfo);
            }
        }
        /// <summary>
        /// Log an error message with automatically captured caller information
        /// </summary>
        [Conditional("DEBUG")]
        public static void Error(string message, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Instance._minimumLogLevel <= LogLevel.Error)
            {
                string callerInfo = GetCallerInfo(memberName, sourceFilePath, sourceLineNumber);
                Instance.LogMessage(LogLevel.Error, message, callerInfo);
            }
        }

        // Helper method to format caller information with BBCode coloring
        private static string GetCallerInfo(string memberName, string sourceFilePath, int sourceLineNumber)
        {
            string scriptName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            return $"[color=cyan]{scriptName}[/color].[color=yellow]{memberName}[/color]:[color=gray]{sourceLineNumber}[/color]";
        }

        // Internal method to actually log the message
        private void LogMessage(LogLevel level, string message, string caller)
        {
            string levelText = GetColoredLogLevel(level);
            string formattedMessage;

            // Parse caller info
            var callerParts = caller.Split('.');
            string scriptName = callerParts.Length > 0 ? callerParts[0] : "Unknown";
            string methodAndLine = callerParts.Length > 1 ? callerParts[1] : "Unknown:0";
            var methodParts = methodAndLine.Split(':');
            string methodName = methodParts.Length > 0 ? methodParts[0] : "Unknown";
            int lineNumber = methodParts.Length > 1 && int.TryParse(methodParts[1], out int line) ? line : 0;
            if (_includeTimestamp)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                formattedMessage = $"[color=gray][{timestamp}][/color] {levelText} [{caller}] {message}";
            }
            else
            {
                formattedMessage = $"{levelText} [{caller}] {message}";
            }

            // Create log entry
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                ScriptName = scriptName,
                MethodName = methodName,
                LineNumber = lineNumber,
                FormattedMessage = formattedMessage,
                PlainMessage = StripBBCode(formattedMessage)
            };

            // Store log entry
            StoreLogEntry(logEntry);

            // Log to Godot console
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    GD.PrintRich(formattedMessage);
                    break;
                case LogLevel.Warning:
                    GD.PushWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    GD.PushError(formattedMessage);
                    break;
            }

            // Log to file if enabled
            if (_logToFile)
            {
                try
                {
                    string filePath = ProjectSettings.GlobalizePath(_logFilePath);
                    using (StreamWriter writer = File.AppendText(filePath))
                    {
                        writer.WriteLine(logEntry.PlainMessage);
                    }
                }
                catch (Exception e)
                {
                    GD.PushError($"Failed to write to log file: {e.Message}");
                    _logToFile = false;
                }
            }
        }

        // Store log entry and manage collection size
        private void StoreLogEntry(LogEntry entry)
        {
            _logEntries.Add(entry);

            // Add script name to set if new
            if (_scriptNames.Add(entry.ScriptName))
            {
                ScriptAdded?.Invoke(entry.ScriptName);
            }

            // Trigger log added event
            LogAdded?.Invoke(entry);

            // Manage log collection size
            if (_logEntries.Count > _maxLogEntries)
            {
                int removeCount = _maxLogEntries / 4; // Remove 25% when limit reached
                _logEntries.RemoveRange(0, removeCount);
            }
        }

        // Get colored log level text
        private string GetColoredLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "[color=blue][DEBUG][/color]",
                LogLevel.Info => "[color=green][INFO][/color]",
                LogLevel.Warning => "[color=orange][WARNING][/color]",
                LogLevel.Error => "[color=red][ERROR][/color]",
                _ => $"[{level.ToString().ToUpper()}]"
            };
        }

        // Strip BBCode tags for file output
        private string StripBBCode(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text, @"\[/?[^\]]*\]", "");
        }
        /// <summary>
        /// Set the minimum log level
        /// </summary>
        [Conditional("DEBUG")]
        public static void SetMinimumLogLevel(LogLevel level)
        {
            Instance._minimumLogLevel = level;
            Debug($"Log level set to {level}");
        }
        /// <summary>
        /// Enable or disable logging to file
        /// </summary>
        [Conditional("DEBUG")]
        public static void SetLogToFile(bool enable, string filePath = null)
        {
            Instance._logToFile = enable;
            if (filePath != null)
            {
                Instance._logFilePath = filePath;
            }
            if (enable)
            {
                try
                {
                    string fullPath = ProjectSettings.GlobalizePath(Instance._logFilePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    File.WriteAllText(fullPath, $"=== Log started at {DateTime.Now} ===\n");
                    Debug($"Logging to file: {fullPath}");
                }
                catch (Exception e)
                {
                    GD.PushError($"Failed to create log file: {e.Message}");
                    Instance._logToFile = false;
                }
            }
            else
            {
                Debug("File logging disabled");
            }
        }
        /// <summary>
        /// Enable or disable timestamps in log messages
        /// </summary>
        [Conditional("DEBUG")]
        public static void SetIncludeTimestamp(bool include)
        {
            Instance._includeTimestamp = include;
        }
        /// <summary>
        /// Set the maximum number of log entries to keep in memory
        /// </summary>
        [Conditional("DEBUG")]
        public static void SetMaxLogEntries(int maxEntries)
        {
            Instance._maxLogEntries = maxEntries;
        }
        /// <summary>
        /// Get all log entries
        /// </summary>
        public static List<LogEntry> GetAllLogs()
        {
            return new List<LogEntry>(Instance._logEntries);
        }
        /// <summary>
        /// Get log entries filtered by script name
        /// </summary>
        public static List<LogEntry> GetLogsByScript(string scriptName)
        {
            return Instance._logEntries.FindAll(entry => entry.ScriptName == scriptName);
        }
        /// <summary>
        /// Get all script names that have logged messages
        /// </summary>
        public static HashSet<string> GetScriptNames()
        {
            return new HashSet<string>(Instance._scriptNames);
        }
        /// <summary>
        /// Clear all stored log entries
        /// </summary>
        [Conditional("DEBUG")]
        public static void ClearLogs()
        {
            Instance._logEntries.Clear();
            Instance._scriptNames.Clear();
        }
    }
}
