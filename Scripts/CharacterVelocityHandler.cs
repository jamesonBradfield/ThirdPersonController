using Godot;
[GlobalClass]
public partial class CharacterVelocityHandler : Node
{
    // PUBLIC FIELDS for Expression access
    public Vector3 Direction;
    public Vector3 Velocity;
    public float Acceleration;
    public float MaxSpeed;

    [Export] CharacterBody3D Target;
    [Export] Node3D Transform;

    public override void _Process(double delta)
    {
        if (Transform != null)
        {
            Basis yOnlyBasis = Basis.FromEuler(new Vector3(0, Transform.Rotation.Y, 0));
            Vector3 worldDirection = (yOnlyBasis * Direction).Normalized();
            Vector3 targetVelocity = worldDirection * MaxSpeed;

            if (Acceleration > 0)
            {
                float accelThisFrame = Acceleration * (float)delta;
                Velocity.X = Mathf.MoveToward(Velocity.X, targetVelocity.X, accelThisFrame);
                Velocity.Z = Mathf.MoveToward(Velocity.Z, targetVelocity.Z, accelThisFrame);
            }
            else if (Acceleration < 0)
            {
                float decelThisFrame = Mathf.Abs(Acceleration) * (float)delta;
                Velocity.X = Mathf.MoveToward(Velocity.X, 0, decelThisFrame);
                Velocity.Z = Mathf.MoveToward(Velocity.Z, 0, decelThisFrame);
            }
        }

        Target.Velocity = Velocity;
    }
}
