using Godot;
using System;
using GodotTools;

/// <summary>
/// Player-specific input provider that reads from Godot's Input system.
/// 
/// RESPONSIBILITIES:
/// - Translates Godot input actions to abstract input commands
/// - Handles camera-relative movement calculations
/// - Provides frame-based input state (pressed vs held)
/// - Manages input mapping and sensitivity settings
/// 
/// USAGE:
/// Attach this to your Player node and assign it to states that need input.
/// States will receive clean, abstracted input without knowing it comes from keyboard/gamepad.
/// </summary>
public partial class PlayerInputProvider : Node, IInputProvider
{
    [Export] Node3D cameraPivot;
    [Export] float movementSensitivity = 1.0f;
    [Export] float lookSensitivity = 1.0f;
    Vector2 movementInput;
    Vector2 lookInput;
    Vector3 worldMovementDirection;

    public override void _Ready()
    {
        if (cameraPivot == null)
        {
            GodotLogger.Warning("CameraPivot not assigned to PlayerInputProvider");
        }
    }

    public override void _Process(double delta)
    {
        UpdateInputs();
        // Calculate world-space movement direction
        CalculateWorldMovementDirection();
    }

    /// <summary>
    /// Updates all input values each frame.
    /// Called automatically during _Process().
    /// </summary>
    void UpdateInputs()
    {
        // Get raw movement input
        movementInput = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        movementInput *= movementSensitivity;

        // Get look input (could be from mouse delta, right stick, etc.)
        lookInput = GetLookInputRaw();
        lookInput *= lookSensitivity;
    }

    /// <summary>
    /// Gets raw look input. Override this method for different look input sources.
    /// Default implementation returns zero (no look input).
    /// </summary>
    protected virtual Vector2 GetLookInputRaw()
    {
        // For now, return zero - this would be implemented for mouse look, right stick, etc.
        // In a full implementation, you might get mouse delta or right stick input here
        return Vector2.Zero;
    }

    /// <summary>
    /// Calculates the world-space movement direction based on camera orientation.
    /// </summary>
    void CalculateWorldMovementDirection()
    {
        if (cameraPivot == null)
        {
            // Fallback: treat input as world-space if no camera pivot
            worldMovementDirection = new Vector3(movementInput.X, 0, movementInput.Y);
            return;
        }
        if (movementInput.Length() <= 0.1f)
        {
            worldMovementDirection = Vector3.Zero;
            return;
        }

        // Transform movement input by camera basis to get world direction
        worldMovementDirection = (cameraPivot.Transform.Basis * new Vector3(-movementInput.X, 0, -movementInput.Y)).Normalized();

        // Scale by input magnitude for analog stick support
        worldMovementDirection *= movementInput.Length();
    }

    // IInputProvider implementation
    public Vector2 GetMovementInput() => movementInput;
    public Vector2 GetLookInput() => lookInput;
    public bool JumpPressed() => Input.IsActionJustPressed("jump");
    public bool JumpHeld() => Input.IsActionPressed("jump");
    public bool AttackPressed() => Input.IsActionJustPressed("attack");
    public bool CrouchHeld() => Input.IsActionPressed("crouch");
    public bool RunHeld() => Input.IsActionPressed("run");
    public float GetMovementMagnitude() => movementInput.Length();
    public Vector3 GetWorldMovementDirection() => worldMovementDirection;
}
