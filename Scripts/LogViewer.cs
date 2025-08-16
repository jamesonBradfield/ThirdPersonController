using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotTools
{
    public partial class LogViewer : Control
    {
        private OptionButton _scriptDropdown;
        private RichTextLabel _logDisplay;
        private Button _clearButton;
        private Button _toggleButton;
        private CheckBox _autoScrollCheck;
        private LineEdit _searchField;
        private Control _titleBar;
        private Control _resizeHandle;

        private string _selectedScript = "All";
        private bool _consoleVisible = false;
        private bool _autoScroll = true;
        private string _searchFilter = "";
        private bool _useBBCode = false;
        private bool _resizing = false;
        private bool _dragging = false;
        private Vector2 _dragStart;

        private const string ALL_SCRIPTS = "All";
        private const float MIN_WIDTH = 300f;
        private const float MIN_HEIGHT = 200f;

        public static LogViewer CreateLogViewer()
        {
            var logViewer = new LogViewer();
            var tree = Engine.GetMainLoop() as SceneTree;
            tree?.Root?.CallDeferred("add_child", logViewer);
            return logViewer;
        }

        public override void _Ready()
        {
            SetupConsoleLayout();
            CreateUI();
            ConnectSignals();
            Hide();
            CallDeferred(nameof(CompleteInitialization));
        }

        private void CompleteInitialization()
        {
            RefreshScriptDropdown();
            PositionResizeHandle();
        }

        private void SetupConsoleLayout()
        {
            var viewport = GetViewport();
            if (viewport == null)
                return;
            var screenSize = viewport.GetVisibleRect().Size;
            Position = new Vector2(10, 10);
            Size = new Vector2(
                Mathf.Max(screenSize.X / 3, MIN_WIDTH),
                Mathf.Max(screenSize.Y / 2, MIN_HEIGHT)
            );
        }

        private void CreateUI()
        {
            var vbox = new VBoxContainer();
            AddChild(vbox);
            vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var background = new ColorRect();
            background.Color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            vbox.AddChild(background);
            vbox.MoveChild(background, 0);
            CreateTitleBar(vbox);
            CreateLogDisplay(vbox);
            CreateResizeHandle();
        }

        private void CreateTitleBar(Container parent)
        {
            var hbox = new HBoxContainer();
            hbox.CustomMinimumSize = new Vector2(0, 30);
            parent.AddChild(hbox);

            var titleBg = new ColorRect();
            titleBg.Color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            titleBg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            hbox.AddChild(titleBg);
            hbox.MoveChild(titleBg, 0);

            var titleLabel = new Label { Text = "📋 Logs" };
            titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            hbox.AddChild(titleLabel);

            var scriptLabel = new Label { Text = "Script:" };
            hbox.AddChild(scriptLabel);
            _scriptDropdown = new OptionButton();
            _scriptDropdown.CustomMinimumSize = new Vector2(100, 0);
            hbox.AddChild(_scriptDropdown);

            var searchLabel = new Label { Text = "Filter:" };
            hbox.AddChild(searchLabel);
            _searchField = new LineEdit();
            _searchField.PlaceholderText = "search...";
            _searchField.CustomMinimumSize = new Vector2(70, 0);
            hbox.AddChild(_searchField);

            _autoScrollCheck = new CheckBox { Text = "Auto", ButtonPressed = true };
            hbox.AddChild(_autoScrollCheck);

            var formatButton = new Button { Text = "BBCode" };
            formatButton.Pressed += OnFormatToggled;
            hbox.AddChild(formatButton);

            var spacer = new Control();
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            hbox.AddChild(spacer);

            _clearButton = new Button { Text = "Clear" };
            hbox.AddChild(_clearButton);

            _toggleButton = new Button { Text = "Hide" };
            hbox.AddChild(_toggleButton);

            // Debug button
            var debugButton = new Button { Text = "Debug" };
            debugButton.Pressed += () =>
            {
                PositionResizeHandle();
                GodotLogger.Debug($"Manual handle update - Handle exists: {_resizeHandle != null}");
                if (_resizeHandle != null)
                    GodotLogger.Debug($"Handle pos: {_resizeHandle.Position}, size: {_resizeHandle.Size}, visible: {_resizeHandle.Visible}");
            };
            hbox.AddChild(debugButton);

            // Store reference and make draggable
            _titleBar = hbox;
        }

        private void CreateLogDisplay(Container parent)
        {
            _logDisplay = new RichTextLabel();
            _logDisplay.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _logDisplay.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _logDisplay.BbcodeEnabled = true;
            _logDisplay.ScrollFollowing = true;
            _logDisplay.SelectionEnabled = true;
            parent.AddChild(_logDisplay);
        }

        private void CreateResizeHandle()
        {
            _resizeHandle = new Control();
            _resizeHandle.Size = new Vector2(16, 16);
            _resizeHandle.MouseFilter = Control.MouseFilterEnum.Pass;
            AddChild(_resizeHandle);

            // Make it VERY visible for debugging
            var bg = new ColorRect();
            bg.Color = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Bright red again for debugging
            bg.Size = new Vector2(16, 16);
            _resizeHandle.AddChild(bg);

            // Add text to make it obvious
            var label = new Label();
            label.Text = "R";
            label.Size = new Vector2(16, 16);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            _resizeHandle.AddChild(label);

            // Force it to be on top
            _resizeHandle.ZIndex = 100;
            GodotLogger.Debug("Resize handle created");
        }

        private void PositionResizeHandle()
        {
            if (_resizeHandle == null)
                return;
            var newPos = Size - new Vector2(16, 16);
            _resizeHandle.Position = newPos;
            GodotLogger.Debug($"Handle positioned at: {newPos}, Window size: {Size}, Handle visible: {_resizeHandle.Visible}");
        }

        private void ConnectSignals()
        {
            if (_scriptDropdown != null)
                _scriptDropdown.ItemSelected += OnScriptSelected;
            if (_clearButton != null)
                _clearButton.Pressed += OnClearPressed;
            if (_toggleButton != null)
                _toggleButton.Pressed += OnTogglePressed;
            if (_autoScrollCheck != null)
                _autoScrollCheck.Toggled += OnAutoScrollToggled;
            if (_searchField != null)
                _searchField.TextChanged += OnSearchTextChanged;

            Resized += PositionResizeHandle;
            GodotLogger.LogAdded += OnLogAdded;
            GodotLogger.ScriptAdded += OnScriptAdded;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseButton)
            {
                GodotLogger.Debug($"Mouse button at: {mouseButton.Position}");
                if (mouseButton.ButtonIndex != MouseButton.Left)
                    return;

                var localPos = mouseButton.Position;
                var resizeArea = new Rect2(Size - new Vector2(16, 16), new Vector2(16, 16));
                var titleArea = new Rect2(Vector2.Zero, new Vector2(Size.X, 30));

                GodotLogger.Debug($"Resize area: {resizeArea}, Click pos: {localPos}, In resize area: {resizeArea.HasPoint(localPos)}");

                if (mouseButton.Pressed)
                {
                    if (resizeArea.HasPoint(localPos))
                    {
                        _resizing = true;
                        _dragStart = mouseButton.GlobalPosition;
                        GodotLogger.Debug("Started resizing!");
                        GetViewport().SetInputAsHandled();
                    }
                    else if (titleArea.HasPoint(localPos))
                    {
                        _dragging = true;
                        _dragStart = mouseButton.GlobalPosition - GlobalPosition;
                        GodotLogger.Debug("Started dragging!");
                        GetViewport().SetInputAsHandled();
                    }
                }
                else
                {
                    if (_resizing) GodotLogger.Debug("Stopped resizing");
                    if (_dragging) GodotLogger.Debug("Stopped dragging");
                    _resizing = false;
                    _dragging = false;
                }
            }
            else if (@event is InputEventMouseMotion mouseMotion)
            {
                var localPos = mouseMotion.Position;
                var resizeArea = new Rect2(Size - new Vector2(16, 16), new Vector2(16, 16));

                if (_resizing)
                {
                    var mousePos = mouseMotion.GlobalPosition;
                    var newSize = mousePos - GlobalPosition + new Vector2(8, 8);
                    newSize.X = Mathf.Max(newSize.X, MIN_WIDTH);
                    newSize.Y = Mathf.Max(newSize.Y, MIN_HEIGHT);
                    Size = newSize;
                    PositionResizeHandle();
                    GetViewport().SetInputAsHandled();
                }
                else if (_dragging)
                {
                    var newPosition = mouseMotion.GlobalPosition - _dragStart;
                    var viewport = GetViewport();
                    if (viewport != null)
                    {
                        var screenSize = viewport.GetVisibleRect().Size;
                        newPosition.X = Mathf.Clamp(newPosition.X, 0, screenSize.X - Size.X);
                        newPosition.Y = Mathf.Clamp(newPosition.Y, 0, screenSize.Y - Size.Y);
                    }
                    GlobalPosition = newPosition;
                    GetViewport().SetInputAsHandled();
                }
                else if (resizeArea.HasPoint(localPos))
                {
                    Input.SetDefaultCursorShape(Input.CursorShape.Fdiagsize);
                    GodotLogger.Debug("Mouse over resize area!");
                }
                else
                {
                    Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
                }
            }
        }

        private void RefreshScriptDropdown()
        {
            if (_scriptDropdown == null)
                return;
            _scriptDropdown.Clear();
            _scriptDropdown.AddItem(ALL_SCRIPTS);
            var scriptNames = GodotLogger.GetScriptNames().OrderBy(name => name);
            foreach (string scriptName in scriptNames)
            {
                _scriptDropdown.AddItem(scriptName);
            }

            for (int i = 0; i < _scriptDropdown.ItemCount; i++)
            {
                if (_scriptDropdown.GetItemText(i) != _selectedScript)
                    continue;
                _scriptDropdown.Selected = i;
                return;
            }
            _scriptDropdown.Selected = 0;
            _selectedScript = ALL_SCRIPTS;
        }

        private void UpdateLogDisplay()
        {
            if (_logDisplay == null)
                return;
            var logs = _selectedScript == ALL_SCRIPTS
                ? GodotLogger.GetAllLogs()
                : GodotLogger.GetLogsByScript(_selectedScript);

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                logs = logs.FindAll(log =>
                    log.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    log.ScriptName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    log.MethodName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));
            }

            _logDisplay.Clear();
            foreach (var log in logs)
            {
                string displayText = _useBBCode
                    ? log.FormattedMessage
                    : log.PlainMessage;
                _logDisplay.AppendText(displayText + "\n");
            }

            if (!_autoScroll)
                return;
            var lineCount = _logDisplay.GetLineCount();
            if (lineCount > 0)
                _logDisplay.ScrollToLine(lineCount - 1);
        }

        private void OnScriptSelected(long index)
        {
            if (_scriptDropdown == null)
                return;
            _selectedScript = _scriptDropdown.GetItemText((int)index);
            UpdateLogDisplay();
        }

        private void OnClearPressed()
        {
            GodotLogger.ClearLogs();
            RefreshScriptDropdown();
            UpdateLogDisplay();
        }

        private void OnTogglePressed()
        {
            if (_consoleVisible)
                HideLogViewer();
            else
                ShowLogViewer();
        }

        private void OnAutoScrollToggled(bool pressed)
        {
            _autoScroll = pressed;
        }

        private void OnSearchTextChanged(string newText)
        {
            _searchFilter = newText;
            UpdateLogDisplay();
        }

        private void OnFormatToggled()
        {
            _useBBCode = !_useBBCode;
            UpdateLogDisplay();
        }

        private void OnLogAdded(GodotLogger.LogEntry logEntry)
        {
            if (!_consoleVisible)
                return;

            bool shouldDisplay = _selectedScript == ALL_SCRIPTS || logEntry.ScriptName == _selectedScript;
            if (shouldDisplay && !string.IsNullOrEmpty(_searchFilter))
            {
                shouldDisplay = logEntry.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                               logEntry.ScriptName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                               logEntry.MethodName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
            }

            if (!shouldDisplay)
                return;

            string displayText = _useBBCode
                ? logEntry.FormattedMessage
                : logEntry.PlainMessage;
            _logDisplay?.AppendText(displayText + "\n");
            if (!_autoScroll || _logDisplay == null)
                return;
            var lineCount = _logDisplay.GetLineCount();
            if (lineCount > 0)
                _logDisplay.ScrollToLine(lineCount - 1);
        }

        private void OnScriptAdded(string scriptName)
        {
            RefreshScriptDropdown();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
                return;

            if (keyEvent.Keycode != Key.F1)
                return;
            OnTogglePressed();
            GetViewport().SetInputAsHandled();
        }

        public void ShowLogViewer()
        {
            if (_consoleVisible)
                return;
            Show();
            _toggleButton.Text = "Hide";
            _consoleVisible = true;
            RefreshScriptDropdown();
            UpdateLogDisplay();
            PositionResizeHandle();
        }

        public void HideLogViewer()
        {
            if (!_consoleVisible)
                return;
            Hide();
            _toggleButton.Text = "Show";
            _consoleVisible = false;
        }
    }
}
