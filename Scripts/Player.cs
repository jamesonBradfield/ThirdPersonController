using Godot;
using System;

public partial class Player : CharacterBody3D
{
    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;
    public Vector3 direction;
    [Export] StateMachine stateMachine;
    [Export] Node3D cameraPivot;
    Vector2 inputDir;
    [Export] float turnSpeed = 10;
    public override void _Process(double delta)
    {

        inputDir = Input.GetVector("move_right", "move_left", "move_backward", "move_forward");
        if (IsOnFloor())
        {
            if (Input.IsActionJustPressed("jump"))
                stateMachine.ChangeState("Jump");
            if (inputDir == Vector2.Zero)
                stateMachine.ChangeState("Idle");
            else
                stateMachine.ChangeState("Walk");
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        direction = (cameraPivot.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        Vector3 TempRotation = Rotation;
        TempRotation.Y = Mathf.LerpAngle(Rotation.Y, Mathf.Atan2(direction.X, direction.Z), turnSpeed * (float)delta);
        Rotation = TempRotation;
        MoveAndSlide();
    }
}
