using Godot;
[GlobalClass]
public partial class Player : CharacterBody3D
{
    // PUBLIC FIELDS for Expression access (not properties!)
    public bool Grounded;
    public bool Moving;
    public bool JumpRequested;
    public bool SprintHeld;
    public float Speed;

    State rootState;

    public override void _Ready()
    {
        rootState = GetNode<State>("RootState");
    }

    public override void _Process(double delta)
    {
        // UPDATE VALUES FIRST
        Grounded = IsOnFloor();
        Moving = (Input.GetVector("move_left", "move_right", "move_forward", "move_backward") != Vector2.Zero);


        Speed = Velocity.Length();

        // THEN process states
        rootState?.Process(delta);

        // Reset per-frame flags AFTER processing
        JumpRequested = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        // // Gravity
        // if (!IsOnFloor())
        //     Velocity = new Vector3(Velocity.X, Velocity.Y - GetGravity().Y * (float)delta, Velocity.Z);
        //
        MoveAndSlide();
    }
}
