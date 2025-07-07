using Godot;
using System;

public partial class WalkState : PlayerState
{
    public override void _Enter()
    {
        Logger.Debug("Entered Walk State");
        animationPlayer.Play("TPose|Walking");
    }

    public override void _Exit()
    {
        // throw new NotImplementedException();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 direction = (cameraPivot.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
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
