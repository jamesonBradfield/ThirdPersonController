using Godot;
using System;
using GodotTools;

/// <summary>
/// Player CharacterBody3D that serves as the physical representation and utility provider.
/// 
/// DESIGN PHILOSOPHY:
/// In our HFSM architecture, the Player is a "dumb" physical object that provides
/// utility methods to states but doesn't contain game logic itself.
/// 
/// RESPONSIBILITIES:
/// - Be the CharacterBody3D for physics
/// - Provide utility methods (GetGravity, Jump, etc.)
/// - Hold references that states need (StateMachine, CameraPivot)
/// - Validate setup during _Ready()
/// 
/// WHAT IT DOESN'T DO:
/// - Handle input (FreeLookBehavior does this)
/// - Manage state transitions (StateMachine and states do this) 
/// - Process movement/rotation (states do this)
/// - Call MoveAndSlide() (states do this when they're ready)
/// 
/// This separation means states have full control over when and how
/// movement is applied, while Player just provides the "what" capabilities.
/// </summary>
public partial class Player : CharacterBody3D
{
    [Export] public StateMachine stateMachine;
    [Export] public Node3D cameraPivot;
    [Export] public float jumpVelocity = 4.5f;

    public override void _Ready()
    {
        ValidateSetup();
    }

    public override void _Process(double delta)
    {
        // Player no longer handles input or state transitions
        // This is now handled by FreeLookBehavior and states
    }

    public override void _PhysicsProcess(double delta)
    {
        // Player no longer handles physics directly
        // States handle their own physics processing and call MoveAndSlide() when ready
    }

    /// <summary>
    /// Validates that required components are properly assigned.
    /// Logs warnings for missing components that could cause issues.
    /// </summary>
    void ValidateSetup()
    {
        if (stateMachine == null)
        {
            GodotLogger.Warning("StateMachine not assigned to Player");
            return;
        }

        if (cameraPivot == null)
        {
            GodotLogger.Warning("CameraPivot not assigned to Player - states won't be able to calculate camera direction");
        }
    }

    /// <summary>
    /// Gets the current gravity value from project settings.
    /// Used by states to apply consistent gravity.
    /// </summary>
    /// <returns>Gravity force as a positive value</returns>
    public float GetGravity()
    {
        return ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    }

    /// <summary>
    /// Applies a jump by setting the Y velocity, but only if on the floor.
    /// Called by states (like JumpState) when jump input is detected.
    /// 
    /// NOTE: This directly modifies player.Velocity rather than working through
    /// the state velocity system, since jump is an immediate velocity change.
    /// </summary>
    public void Jump()
    {
        if (!IsOnFloor())
            return;

        Vector3 currentVelocity = Velocity;
        currentVelocity.Y = jumpVelocity;
        Velocity = currentVelocity;

        GodotLogger.Debug("Player jumped");
    }

    /// <summary>
    /// Gets the camera pivot for states that need to calculate camera-relative movement.
    /// Returns null if not assigned (states should handle this gracefully).
    /// </summary>
    /// <returns>Camera pivot Node3D or null</returns>
    public Node3D GetCameraPivot()
    {
        return cameraPivot;
    }

    /// <summary>
    /// Gets the state machine for any external systems that need to trigger state changes.
    /// </summary>
    /// <returns>The player's state machine</returns>
    public StateMachine GetStateMachine()
    {
        return stateMachine;
    }
}
