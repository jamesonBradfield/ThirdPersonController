using Godot;

namespace GodotTools
{
    public partial class LogViewerSetup : Node
    {
        private LogViewer _logViewer;
        public override void _Ready()
        {
            InitializeLoggingSystem();
            ConfigureLogger();
            CreateLogViewer();
            TestLogging();
        }

        private void InitializeLoggingSystem()
        {
            GodotLogger.Initialize();

            // Wait one frame to ensure logger is fully initialized
            GetTree().ProcessFrame += OnLoggerInitialized;
        }

        private void OnLoggerInitialized()
        {
            GetTree().ProcessFrame -= OnLoggerInitialized;
            GodotLogger.Info("Logger initialization complete");
        }

        private void ConfigureLogger()
        {
            GodotLogger.SetMinimumLogLevel(GodotLogger.LogLevel.Debug);
            GodotLogger.SetIncludeTimestamp(true);
            GodotLogger.SetMaxLogEntries(500);
        }

        private void CreateLogViewer()
        {
            _logViewer = LogViewer.CreateLogViewer();

            // Show immediately for testing - remove this line for production
            GetTree().ProcessFrame += OnNextFrame;
        }

        private void OnNextFrame()
        {
            GetTree().ProcessFrame -= OnNextFrame;
            _logViewer?.ShowLogViewer();
            GodotLogger.Info("LogViewer displayed - Press F1 to toggle");
        }

        private void TestLogging()
        {
            GodotLogger.Debug("Debug message from setup");
            GodotLogger.Info("System started successfully");
            GodotLogger.Warning("Sample warning message");

            // Test from another method
            SimulateOtherScript();
        }

        private void SimulateOtherScript()
        {
            GodotLogger.Info("Message from SimulateOtherScript method");
            GodotLogger.Debug("Additional debug information");
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
                return;

            switch (keyEvent.Keycode)
            {
                case Key.Key1:
                    GodotLogger.Debug("Test debug message");
                    break;
                case Key.Key2:
                    GodotLogger.Info("Test info message");
                    break;
                case Key.Key3:
                    GodotLogger.Warning("Test warning message");
                    break;
                case Key.Key4:
                    GodotLogger.Error("Test error message");
                    break;
                case Key.F2:
                    _logViewer?.ShowLogViewer();
                    break;
                case Key.F3:
                    // Debug current state
                    var logs = GodotLogger.GetAllLogs();
                    GD.Print($"=== F3 Debug ===");
                    GD.Print($"Total logs: {logs.Count}");
                    GD.Print($"LogViewer exists: {_logViewer != null}");
                    if (logs.Count > 0)
                    {
                        GD.Print($"First log: {logs[0].PlainMessage}");
                        GD.Print($"Last log: {logs[logs.Count - 1].PlainMessage}");
                    }
                    break;
            }
        }
    }
}
