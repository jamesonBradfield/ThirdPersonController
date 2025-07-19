using Godot;
using System;
using GodotTools;

public partial class FreeLookBehavior : PlayerState
{
    [Export] protected float turnSpeed = 10.0f;
    protected IInputProvider inputProvider;
    protected Vector2 inputDir;
    protected Vector3 worldDirection;

    public override void HandleReady()
    {
        // Automatically find input provider in the scene tree
        inputProvider = GetInputProviderFromTree();
        if (inputProvider == null)
        {
            GodotLogger.Warning("No InputProvider found in scene tree - FreeLookBehavior will not respond to input");
        }
        else
        {
            GodotLogger.Debug($"Found InputProvider: {inputProvider.GetType().Name}");
        }

        // Debug: Check our setup
        GodotLogger.Debug($"FreeLookBehavior ready. StateMachine: {stateMachine?.Name}, DefaultSubstate: {defaultSubstate?.Name}");
    }

    public override void HandleEnter()
    {
        GodotLogger.Debug("Entered FreeLook Behavior");
    }

    public override void HandleExit()
    {
    }

    public override void HandleProcess(double delta)
    {
        GodotLogger.Debug("FreeLookBehavior.HandleProcess called");
        UpdateInput();
        HandleRotation(delta);
        CheckGlobalTransitions();
    }

    public override void HandlePhysicsProcess(double delta)
    {
        // Let leaf states handle physics processing
        // This prevents double-processing
    }

    /// <summary>
    /// Tries to find an input provider in the scene tree.
    /// Looks for PlayerInputProvider or AIInputProvider nodes.
    /// </summary>
    IInputProvider GetInputProviderFromTree()
    {
        // First check player node (common location)
        Node playerNode = player;
        if (playerNode != null)
        {
            // Check direct children
            foreach (Node child in playerNode.GetChildren())
            {
                if (child is IInputProvider childProvider)
                {
                    GodotLogger.Debug($"Found InputProvider as child of Player: {child.Name}");
                    return childProvider;
                }
            }
        }

        // Check our own parent and siblings
        Node parent = GetParent();
        if (parent != null)
        {
            foreach (Node sibling in parent.GetChildren())
            {
                if (sibling is IInputProvider siblingProvider)
                {
                    GodotLogger.Debug($"Found InputProvider as sibling: {sibling.Name}");
                    return siblingProvider;
                }
            }
        }

        // Check state machine's parent (often the character)
        if (stateMachine?.GetParent() != null)
        {
            foreach (Node child in stateMachine.GetParent().GetChildren())
            {
                if (child is IInputProvider characterProvider)
                {
                    GodotLogger.Debug($"Found InputProvider as child of character: {child.Name}");
                    return characterProvider;
                }
            }
        }
        return null;
    }

    public virtual void UpdateInput()
    {
        if (inputProvider == null)
            return;
        inputDir = inputProvider.GetMovementInput();
        worldDirection = inputProvider.GetWorldMovementDirection();

        // Debug log for input
        if (inputDir.Length() > 0.1f)
        {
            GodotLogger.Debug($"Input: {inputDir}, World Direction: {worldDirection}");
        }
    }

    public virtual void HandleRotation(double delta)
    {
        if (inputDir.Length() <= 0.1f)
            return;
        if (worldDirection.Length() <= 0.1f)
            return;

        float targetRotation = Mathf.Atan2(worldDirection.X, worldDirection.Z);
        Vector3 tempRotation = player.Rotation;
        tempRotation.Y = Mathf.LerpAngle(player.Rotation.Y, targetRotation, turnSpeed * (float)delta);
        player.Rotation = tempRotation;
    }

    public virtual void CheckGlobalTransitions()
    {
        GodotLogger.Debug($"CheckGlobalTransitions called, inputProvider: {inputProvider?.GetType().Name ?? "NULL"}"); if (inputProvider == null)
            return;
        if (inputProvider.JumpPressed() && player.IsOnFloor())
        {
            GodotLogger.Debug("Jump input detected");
            stateMachine.ChangeState("JumpState");
            return;
        }
        CheckMovementTransitions();
    }

    public virtual void CheckMovementTransitions()
    {
        if (stateMachine == null)
        {
            GodotLogger.Warning("StateMachine is null in FreeLookBehavior");
            return;
        }
        if (inputProvider == null)
            return;
        bool hasMovementInput = inputProvider.GetMovementMagnitude() > 0.1f;
        bool runHeld = inputProvider.RunHeld();
        State currentLeafState = stateMachine.GetCurrentLeafState();
        if (currentLeafState == null)
        {
            GodotLogger.Warning("Current leaf state is null");
            return;
        }
        string currentStateName = currentLeafState.Name;

        // Debug current state
        GodotLogger.Debug($"Current leaf state: {currentStateName}, Has input: {hasMovementInput}");

        // Log the current state path for debugging
        for (int i = 0; i < 10; i++) // Check up to 10 levels
        {
            State stateAtLevel = stateMachine.GetActiveStateAtLevel(i);
            if (stateAtLevel == null) break;
            GodotLogger.Debug($"Level {i}: {stateAtLevel.Name}");
        }
        //TODO: add running transition logic.
        if (hasMovementInput && currentStateName == "Idle" && !runHeld)
        {
            GodotLogger.Debug("Transitioning from Idle to Walk");
            stateMachine.ChangeState("Walk");
            return;
        }
        if (hasMovementInput && currentStateName == "Run" && !runHeld)
        {
            GodotLogger.Debug("Transitioning from Run to Walk");
            stateMachine.ChangeState("Walk");
            return;
        }
        if (hasMovementInput && currentStateName == "Idle" && runHeld)
        {
            GodotLogger.Debug("Transitioning from Idle to Run");
            stateMachine.ChangeState("Run");
            return;
        }
        if (hasMovementInput && currentStateName == "Walk" && runHeld)
        {
            GodotLogger.Debug("Transitioning from Walk to Run");
            stateMachine.ChangeState("Run");
            return;
        }
        if ((!hasMovementInput && !runHeld) && (currentStateName == "Walk" || currentStateName == "Run"))
        {
            GodotLogger.Debug("Transitioning from Walk to Idle");
            stateMachine.ChangeState("Idle");
            return;
        }
    }

    public virtual void ApplyGravity(double delta)
    {
        if (player.IsOnFloor())
            return;

        // Apply gravity (negative because GetGravity() returns positive value)
        velocity.Y -= player.GetGravity() * (float)delta;
    }

    public Vector2 GetInputDirection() => inputDir;
    public Vector3 GetWorldDirection() => worldDirection;
    public Vector3 GetVelocity() => velocity;
    public void SetVelocity(Vector3 newVelocity) => velocity = newVelocity;
}
