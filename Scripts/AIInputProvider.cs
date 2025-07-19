using Godot;
using System;
using GodotTools;

/// <summary>
/// AI-driven input provider that generates input based on AI logic.
/// 
/// PURPOSE:
/// Demonstrates how the same states that work with player input
/// can be driven by AI without any changes to the state code.
/// 
/// EXAMPLE BEHAVIORS:
/// - Patrol between waypoints
/// - Chase player when in range
/// - Random movement for ambient creatures
/// - Scripted movement sequences
/// 
/// EXTENSIBILITY:
/// Inherit from this class to create specific AI behaviors:
/// - GuardAI: Patrols and investigates sounds
/// - ChaseAI: Pursues targets aggressively  
/// - FleeAI: Runs away from threats
/// - CompanionAI: Follows player at distance
/// </summary>
public partial class AIInputProvider : Node, IInputProvider
{
    [Export] Node3D[] waypoints = Array.Empty<Node3D>();
    [Export] float moveSpeed = 1.0f;
    [Export] float waypointThreshold = 1.0f;
    [Export] float pauseTime = 2.0f;
    CharacterBody3D character;
    int currentWaypointIndex = 0;
    float pauseTimer = 0.0f;
    bool paused = false;
    Vector2 currentMovementInput;
    Vector3 currentWorldDirection;

    public override void _Ready()
    {
        character = GetParent<CharacterBody3D>();
        if (character == null)
        {
            GodotLogger.Warning("AIInputProvider must be child of CharacterBody3D");
        }
        if (waypoints.Length == 0)
        {
            GodotLogger.Warning("No waypoints assigned to AIInputProvider - AI will not move");
        }
    }

    public override void _Process(double delta)
    {
        UpdateAILogic(delta);
    }

    /// <summary>
    /// Main AI logic update. Override this for different AI behaviors.
    /// </summary>
    protected virtual void UpdateAILogic(double delta)
    {
        if (character == null || waypoints.Length == 0)
        {
            SetMovementInput(Vector2.Zero);
            return;
        }
        HandlePauseTimer(delta);
        if (paused)
        {
            SetMovementInput(Vector2.Zero);
            return;
        }
        ProcessWaypointMovement();
    }

    /// <summary>
    /// Handles pause timer when AI reaches waypoints.
    /// </summary>
    void HandlePauseTimer(double delta)
    {
        if (!paused)
            return;
        pauseTimer -= (float)delta;
        if (pauseTimer <= 0.0f)
        {
            paused = false;
            NextWaypoint();
        }
    }

    /// <summary>
    /// Processes movement toward current waypoint.
    /// </summary>
    void ProcessWaypointMovement()
    {
        Node3D targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 characterPos = character.GlobalPosition;
        Vector3 targetPos = targetWaypoint.GlobalPosition;

        // Check if we've reached the waypoint
        float distanceToWaypoint = characterPos.DistanceTo(targetPos);
        if (distanceToWaypoint <= waypointThreshold)
        {
            StartPause();
            return;
        }

        // Calculate direction to waypoint
        Vector3 directionToWaypoint = (targetPos - characterPos).Normalized();

        // Convert world direction to movement input
        // This is a simplified conversion - you might want more sophisticated logic
        Vector2 movementDirection = new Vector2(directionToWaypoint.X, directionToWaypoint.Z);
        movementDirection = movementDirection.Normalized() * moveSpeed;
        SetMovementInput(movementDirection);
    }

    /// <summary>
    /// Starts a pause at the current waypoint.
    /// </summary>
    void StartPause()
    {
        paused = true;
        pauseTimer = pauseTime;
        SetMovementInput(Vector2.Zero);
    }

    /// <summary>
    /// Advances to the next waypoint in the sequence.
    /// </summary>
    void NextWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    /// <summary>
    /// Sets the current movement input and calculates world direction.
    /// </summary>
    protected void SetMovementInput(Vector2 input)
    {
        currentMovementInput = input;

        // For AI, movement input directly maps to world direction
        // (AI already thinks in world space)
        currentWorldDirection = new Vector3(input.X, 0, input.Y);
        if (currentWorldDirection.Length() > 1.0f)
            currentWorldDirection = currentWorldDirection.Normalized();
    }

    // IInputProvider implementation - AI never uses these actions by default
    public Vector2 GetMovementInput() => currentMovementInput;
    public Vector2 GetLookInput() => Vector2.Zero;
    public bool JumpPressed() => false;  // Override for jumping AI
    public bool JumpHeld() => false;
    public bool AttackPressed() => false;  // Override for combat AI
    public bool CrouchHeld() => false;
    public bool RunHeld() => false;  // Override for AI that can run
    public float GetMovementMagnitude() => currentMovementInput.Length();
    public Vector3 GetWorldMovementDirection() => currentWorldDirection;
}
