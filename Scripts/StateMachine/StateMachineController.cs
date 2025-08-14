using System.Collections.Generic;
using Godot;
using GodotTools;

// might not even need this if every state handles its substates transitions.
public partial class StateMachineController : Node
{
    bool hasMovementInput;
    StateMachine stateMachine;
    IInputWrapper inputProvider;
    CharacterBody3D body3D;

    public override void _Ready()
    {
        body3D = GetParent<CharacterBody3D>();
        stateMachine = body3D.FindChild("StateMachine") as StateMachine;
        inputProvider = body3D.FindChild("PlayerInputWrapper") as IInputWrapper;
        if (stateMachine == null)
        {
            GodotLogger.Warning("StateMachine is null!");
            return;
        }
        if (inputProvider == null)
        {
            GodotLogger.Warning("inputProvider is null!");
            return;
        }
    }

    public override void _Process(double delta)
    {
        UpdateInput();
        CheckMovementTransitions();
    }

    public virtual void UpdateInput()
    {
        hasMovementInput = (inputProvider.GetMovementInput() != Vector2.Zero) ? (true) : (false);
    }
    public virtual void CheckMovementTransitions()
    {
        List<InitializedState> currentStates = stateMachine.GetCurrentStatePath();
        if (currentStates == null)
        {
            GodotLogger.Warning("Current states are null");
            return;
        }
        bool hasMovementInput = inputProvider.GetMovementMagnitude() > 0.1f;
        bool jumpPressed = inputProvider.JumpPressed();
        bool runHeld = inputProvider.RunHeld();
        foreach (InitializedState state in currentStates)
        {
            string currentStateName = state.Name;
            switch (currentStateName)
            {
                case "Grounded":
                    if (!body3D.IsOnFloor())
                    {
                        stateMachine.ChangeState("Airborne");
                        return;
                    }
                    if (jumpPressed)
                    {
                        stateMachine.ChangeState("Jump");
                        return;
                    }
                    break;
                case "Idle":
                    if (hasMovementInput && !runHeld)
                    {
                        stateMachine.ChangeState("Walk");
                        return;
                    }
                    else if (hasMovementInput && runHeld)
                    {
                        stateMachine.ChangeState("Run");
                        return;
                    }
                    break;

                case "Walk":
                    if (hasMovementInput && runHeld)
                    {
                        stateMachine.ChangeState("Run");
                        return;
                    }
                    else if (!hasMovementInput && !runHeld)
                    {
                        stateMachine.ChangeState("Idle");
                        return;
                    }
                    break;

                case "Run":
                    if (hasMovementInput && !runHeld)
                    {
                        stateMachine.ChangeState("Walk");
                        return;
                    }
                    else if (!hasMovementInput && !runHeld)
                    {
                        stateMachine.ChangeState("Idle");
                        return;
                    }
                    break;
                case "Airborne":
                    if (body3D.IsOnFloor())
                    {
                        stateMachine.ChangeState("Grounded");
                        return;
                    }
                    break;
                default:
                    // No transitions defined for other states
                    break;
            }
        }
    }

}
