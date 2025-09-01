using Godot;
public partial class InputHandler : Node
{
    [Signal] public delegate void MovementEventHandler(Vector3 direction);
    [Signal] public delegate void JumpEventHandler();
    [Signal] public delegate void SprintEventHandler(bool held);
    [Signal] public delegate void NoMovementEventHandler();

    Vector3 lastMovement;

    public override void _Process(double delta)
    {
        Vector3 moveInput = new(Input.GetVector("move_left", "move_right", "move_forward", "move_backward").X, 0, Input.GetVector("move_left", "move_right", "move_forward", "move_backward").Y);


        if (moveInput.Length() > 0.1f)
            EmitSignal(SignalName.Movement, moveInput);
        else if (lastMovement.Length() > 0.1f)
            EmitSignal(SignalName.NoMovement);

        lastMovement = moveInput;

        if (Input.IsActionJustPressed("jump"))
            EmitSignal(SignalName.Jump);

        if (Input.IsActionPressed("run"))
            EmitSignal(SignalName.Sprint, true);
        else if (Input.IsActionJustReleased("run"))
            EmitSignal(SignalName.Sprint, false);
    }
}
