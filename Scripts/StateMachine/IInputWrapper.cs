using Godot;

/// <summary>
/// Abstraction layer for input sources.
/// 
/// PURPOSE:
/// Decouples states from specific input sources (player, AI, network, etc.)
/// States receive commands through this interface without knowing the source.
/// 
/// BENEFITS:
/// - States can be reused for AI characters
/// - Easy to swap input methods (keyboard, gamepad, network)
/// - States can be tested in isolation
/// - Clean separation of concerns
/// 
/// IMPLEMENTATIONS:
/// - PlayerInputProvider: Reads from Godot Input
/// - AIInputProvider: Uses AI logic to generate input
/// - NetworkInputProvider: Receives input from network
/// - TestInputProvider: For unit testing states
/// </summary>
public interface IInputWrapper
{
    /// <summary>
    /// Gets the current movement direction as a normalized vector.
    /// X = left/right, Y = forward/backward
    /// </summary>
    Vector2 GetMovementInput();
    /// <summary>
    /// Gets the current look direction as a normalized vector.
    /// For players, this comes from camera/mouse input.
    /// For AI, this comes from AI decision making.
    /// </summary>
    Vector2 GetLookInput();
    /// <summary>
    /// Checks if jump input was triggered this frame.
    /// </summary>
    bool JumpPressed();
    /// <summary>
    /// Checks if jump input is currently held.
    /// </summary>
    bool JumpHeld();
    /// <summary>
    /// Checks if attack input was triggered this frame.
    /// </summary>
    bool AttackPressed();
    /// <summary>
    /// Checks if crouch input is currently held.
    /// </summary>
    bool CrouchHeld();
    /// <summary>
    /// Checks if run input is currently held.
    /// </summary>
    bool RunHeld();
    /// <summary>
    /// Gets the magnitude of movement input (0.0 to 1.0).
    /// Useful for analog stick support and AI intensity levels.
    /// </summary>
    float GetMovementMagnitude();
}
