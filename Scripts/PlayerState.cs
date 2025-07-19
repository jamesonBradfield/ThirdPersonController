using Godot;
using System;
using GodotTools;

/// <summary>
/// Base class for all player-related states.
/// 
/// PURPOSE:
/// This class serves as a specialized version of State that includes
/// common properties and references needed by player states.
/// 
/// DESIGN PHILOSOPHY:
/// - Keep it minimal - only include what ALL player states need
/// - Don't add behavior here - this is just shared data
/// - Let concrete states handle their own specific logic
/// - Use composition (scene tree) rather than inheritance for behaviors
/// 
/// WHAT GOES HERE:
/// - Common exports that most player states need (AnimationPlayer, Player reference)
/// - Shared data structures (velocity)
/// - NOT behavior or logic - that goes in concrete states or behavior providers
/// 
/// INHERITANCE CHAIN:
/// PlayerState : State : Node
/// 
/// All concrete player states should inherit from this, not from State directly.
/// This gives them access to player-specific properties while maintaining
/// the hierarchical state machine functionality.
/// </summary>
public abstract partial class PlayerState : State
{
    /// <summary>
    /// Reference to the player's animation controller.
    /// Most player states will need to trigger animations, so this is commonly used.
    /// Export allows it to be set in the Godot editor.
    /// </summary>
    protected AnimationPlayer animationPlayer;

    /// <summary>
    /// Reference to the Player (CharacterBody3D) that owns this state machine.
    /// States need this to access player properties like IsOnFloor(), GetGravity(), etc.
    /// and to apply movement via MoveAndSlide().
    /// </summary>
    protected Player player;

    /// <summary>
    /// Shared velocity vector that states can modify.
    /// 
    /// IMPORTANT: This is the main way states communicate movement intent.
    /// - States modify this vector based on their logic (movement, deceleration, etc.)
    /// - The final velocity gets applied to player.Velocity before MoveAndSlide()
    /// - Parent states can share their velocity with child states for coordination
    /// 
    /// For example:
    /// - IdleState modifies velocity to apply deceleration
    /// - WalkState modifies velocity to apply movement in camera direction
    /// - Gravity is applied by adding to velocity.Y
    /// </summary>
    public Vector3 velocity;
}
