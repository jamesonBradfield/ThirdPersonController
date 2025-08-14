using Godot;

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
public partial class PlayerInputWrapper : Node, IInputWrapper
{
    private Vector2 movementInput = Vector2.Zero;
    private Vector2 lookInput = Vector2.Zero;
    private Vector2 mouseLookInput = Vector2.Zero;
    private bool switchToMouse;
    public override void _Process(double delta)
    {
        movementInput = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        lookInput = Input.GetVector("look_left", "look_right", "look_up", "look_down");
    }
    public Vector2 GetMovementInput() => movementInput;
    public Vector2 GetLookInput() => lookInput;
    public bool JumpPressed() => Input.IsActionJustPressed("jump");
    public bool JumpHeld() => Input.IsActionPressed("jump");
    public bool AttackPressed() => Input.IsActionJustPressed("attack");
    public bool CrouchHeld() => Input.IsActionPressed("crouch");
    public bool RunHeld() => Input.IsActionPressed("run");
    public float GetMovementMagnitude() => movementInput.Length();
}
