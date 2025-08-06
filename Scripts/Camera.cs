using Godot;

public partial class Camera : Node3D
{
    [Export] public Camera3D camera;
    [Export] public float mouseSensitivity;
    [Export] public float tilt_limit = Mathf.DegToRad(75);
    [Export] public Node3D target;
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            Vector3 tempRotation = this.Rotation;
            tempRotation.X += (@event as InputEventMouseMotion).Relative.Y * mouseSensitivity;
            tempRotation.X = Mathf.Clamp(tempRotation.X, -tilt_limit, tilt_limit);
            tempRotation.Y += -(@event as InputEventMouseMotion).Relative.X * mouseSensitivity;
            this.Rotation = tempRotation;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Position = target.Position;
    }
}
