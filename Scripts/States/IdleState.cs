using Godot;
using System;
using GodotTools;

public partial class IdleState : PlayerState
{
    [Export] float decelerationMultiplier = 2.0f;

    public override void HandleReady()
    {
    }

    public override void HandleEnter()
    {
        GodotLogger.Debug("Entered Idle State");
        if (animationPlayer == null)
            return;
        animationPlayer.Play("TPose|Idle");
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
        FreeLookBehavior freeLook = GetFreeLookBehavior();
        if (freeLook == null)
            return;

        // Start with the shared velocity
        velocity = freeLook.velocity;

        // Apply gravity
        if (!player.IsOnFloor())
        {
            velocity.Y -= player.GetGravity() * (float)delta;
        }

        // Apply deceleration
        float decelerationSpeed = 5.0f * decelerationMultiplier;
        velocity.X = Mathf.MoveToward(velocity.X, 0, decelerationSpeed);
        velocity.Z = Mathf.MoveToward(velocity.Z, 0, decelerationSpeed);

        // Apply to player
        player.Velocity = velocity;
        player.MoveAndSlide();

        // Update shared velocity
        velocity = player.Velocity;
        freeLook.velocity = velocity;
    }

    FreeLookBehavior GetFreeLookBehavior()
    {
        return GetParent().GetParent<FreeLookBehavior>();
    }
}
