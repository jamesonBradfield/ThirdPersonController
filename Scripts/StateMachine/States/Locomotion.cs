using Godot;

public partial class Locomotion : InitializedState
{
    [Export] private float blendMult;
    [Export] private float MaxSpeed;
    [Export] private float Acceleration;
    [Export] private bool inheritVelocity;
    [Export] private Vector3 Velocity;
    [Export] private CharacterVelocityHandler velocityHandler;
    private FreeLook freeLook;


    public override void HandleReady()
    {
        base.HandleReady();
        freeLook = stateMachine.FindState("FreeLook") as FreeLook;
    }

    public override void HandleEnter()
    {
        velocityHandler.MaxSpeed = MaxSpeed;
        velocityHandler.Acceleration = Acceleration;
        if (inheritVelocity) { return; }
        velocityHandler.Velocity += Velocity;
    }

    public override void HandleExit() { }

    public override void HandleProcess(double delta)
    {
        Vector2 moveInput = inputProvider.GetMovementInput();
        // GodotLogger.Info($"moveInput : {moveInput}");
        Vector3 horizonMove = new(-moveInput.X, 0, -moveInput.Y);
        velocityHandler.Direction = horizonMove;
    }

    public override void HandlePhysicsProcess(double delta) { }
}
