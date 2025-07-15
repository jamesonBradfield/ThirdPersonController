using Godot;
using System;
using GodotTools;

public partial class WalkState : PlayerState
{
    public override void HandleReady()
    {
    }

    public override void HandleEnter()
    {
        GodotLogger.Debug("Entered Walk State");
        if (animationPlayer == null)
            return;
        animationPlayer.Play("TPose|Walking");
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
        LocomotionBehavior locomotion = GetLocomotionBehavior();
        if (locomotion == null)
            return;
        FreeLookBehavior freeLook = locomotion.GetFreeLookBehavior();
        if (freeLook == null)
            return;

        // Start with the shared velocity
        velocity = freeLook.velocity;

        // Apply gravity
        if (!player.IsOnFloor())
        {
            velocity.Y += player.GetGravity() * (float)delta;
        }

        // Apply movement using the camera direction
        Vector3 cameraDirection = freeLook.GetWorldDirection();
        if (cameraDirection.Length() > 0.1f)
        {
            velocity.X = cameraDirection.X * locomotion.GetSpeed();
            velocity.Z = cameraDirection.Z * locomotion.GetSpeed();
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
