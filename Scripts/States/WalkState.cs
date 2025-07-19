using Godot;
using System;
using GodotTools;

public partial class WalkState : PlayerState
{
    [Export] float WalkSpeed;
    LocomotionBehavior locomotion;
    FreeLookBehavior freeLook;
    public override void HandleReady()
    {
    }

    public override void HandleEnter()
    {
        GodotLogger.Debug("Entered Walk State");
        if (animationPlayer == null)
            return;
        animationPlayer.Play("TPose|Walking");
        locomotion = GetLocomotionBehavior();
        freeLook = locomotion.GetFreeLookBehavior();
        locomotion.SetSpeed(WalkSpeed);
    }

    public override void HandleExit()
    {
    }

    public override void HandleProcess(double delta)
    {
        // Let the state machine handle the hierarchical processing
        // We don't need to manually delegate here
    }

    public override void HandlePhysicsProcess(double delta)
    {
        if (locomotion == null)
            return;
        if (freeLook == null)
            return;

        // Start with the shared velocity
        velocity = freeLook.velocity;

        // Apply gravity
        if (!player.IsOnFloor())
        {
            velocity.Y -= player.GetGravity() * (float)delta;
        }

        // Apply movement using the world direction from input provider
        Vector3 worldDirection = freeLook.GetWorldDirection();
        if (worldDirection.Length() > 0.1f)
        {
            velocity.X = worldDirection.X * locomotion.GetSpeed();
            velocity.Z = worldDirection.Z * locomotion.GetSpeed();
        }

        // Apply to player
        player.Velocity = velocity;
        player.MoveAndSlide();

        // Update shared velocity
        velocity = player.Velocity;
        freeLook.velocity = velocity;
    }

    LocomotionBehavior GetLocomotionBehavior()
    {
        return GetParent<LocomotionBehavior>();
    }
}
