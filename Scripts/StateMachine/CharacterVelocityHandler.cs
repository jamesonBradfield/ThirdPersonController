using Godot;
[GlobalClass]
public partial class CharacterVelocityHandler : Node
{
    [ExportGroup("Physics")]
    [Export] private Vector3 velocity;
    [Export] private Vector3 direction;
    [Export] private float acceleration;
    [Export] private float maxSpeed;
    [ExportGroup("Nodes")]
    [Export] private Node3D transform;
    [Export] private CharacterBody3D target;
    [ExportGroup("Debug")]
    [Export] private bool Debug = true;

    public Vector3 Direction { get => direction; set => direction = value; }
    public float Acceleration { get => acceleration; set => acceleration = value; }
    public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }
    public Node3D Transform { get => transform; set => transform = value; }
    public CharacterBody3D Target { get => target; set => target = value; }
    public Vector3 Velocity { get => velocity; set => velocity = value; }

    public override void _PhysicsProcess(double delta) { }

    public override void _Process(double delta)
    {
        if (Transform != null)
        {
            Basis yOnlyBasis = Basis.FromEuler(new Vector3(0, Transform.Rotation.Y, 0));
            Vector3 localDirection = (yOnlyBasis * Direction).Normalized();
            velocity += localDirection * Acceleration * (float)delta;
        }
        else
        {
            velocity += Direction * Acceleration * (float)delta;
        }
        velocity = velocity.LimitLength(MaxSpeed);
        Target.Velocity = velocity;
        if (Debug)
        {
            DebugDraw3D.DrawGizmo(Target.Transform);
        }
    }


}
