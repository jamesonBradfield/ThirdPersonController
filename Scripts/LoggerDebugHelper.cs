using Godot;

namespace GodotTools
{
    public partial class LoggerDebugHelper : Node
    {
        public override void _Ready()
        {
            CallDeferred(nameof(RunDiagnostics));
        }

        private void RunDiagnostics()
        {
            GD.Print("=== LOGGER DIAGNOSTICS ===");

            // Check if we're in DEBUG mode
#if DEBUG
            GD.Print("✓ Running in DEBUG mode");
#else
            GD.Print("✗ Running in RELEASE mode - Logger methods won't execute!");
#endif

            // Check logger instance
            try
            {
                var instance = GodotLogger.Instance;
                if (instance != null)
                {
                    GD.Print("✓ Logger instance exists");

                    // Test logging
                    GodotLogger.Info("Diagnostic test message");
                    var logs = GodotLogger.GetAllLogs();
                    GD.Print($"✓ Total logs in memory: {logs.Count}");
                    var scripts = GodotLogger.GetScriptNames();
                    GD.Print($"✓ Scripts that have logged: {scripts.Count}");
                }
                else
                {
                    GD.Print("✗ Logger instance is null");
                }
            }
            catch (System.Exception e)
            {
                GD.Print($"✗ Logger error: {e.Message}");
            }

            // Check for LogViewer in scene
            var logViewer = GetTree().GetNodesInGroup("log_viewer");
            if (logViewer.Count > 0)
                GD.Print("✓ LogViewer found in scene");
            else
                GD.Print("? No LogViewer nodes found (this may be normal)");
            GD.Print("=== END DIAGNOSTICS ===");
        }
    }
}
