using Godot;
using System;
using GodotTools;

public partial class LocomotionBehavior : PlayerState
{
    [Export] protected float speed = 5.0f;

    public override void HandleReady()
    {

    }

    public override void HandleEnter()
    {
        GodotLogger.Debug("Entered Locomotion Behavior");
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
        // Let leaf states handle physics processing
        // This prevents double-processing
    }

    public FreeLookBehavior GetFreeLookBehavior()
    {
        return GetParent<FreeLookBehavior>();
    }

    public float GetSpeed() => speed;
    public void SetSpeed(float speed) => this.speed = speed;
}
