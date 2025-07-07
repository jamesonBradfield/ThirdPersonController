using Godot;
using System;

public partial class IdleState : PlayerState
{
    [Export] float Speed;
    public Vector3 velocity;
    public override void _Enter()
    {
        Logger.Debug("Entered Idle State");
        animationPlayer.Play("TPose|Idle");
    }

    public override void _Exit()
    {
        // throw new NotImplementedException();
    }

    public override void _PhysicsProcess(double delta)
    {
        // throw new NotImplementedException();
        velocity.X = Mathf.MoveToward(player.Velocity.X, 0, Speed);
        velocity.Z = Mathf.MoveToward(player.Velocity.Z, 0, Speed);
    }

    public override void _Process(double delta)
    {
        // throw new NotImplementedException();
    }

    public override void _Ready()
    {
        // throw new NotImplementedException();
    }
}
