using Godot;

public partial class FreeLook : InitializedState
{
    [Export] private Node3D pivot;
    // [Export] public CharacterVelocityHandler velocityHandler;
    // [Export] public Node3D SpineIK;
    [Export] private Node3D target;
    [Export] private float lookSensitivity;
    [Export] private float tilt_limit = Mathf.DegToRad(75);

    public Node3D Pivot { get => pivot; set => pivot = value; }
    public Node3D Target { get => target; set => target = value; }
    public float LookSensitivity { get => lookSensitivity; set => lookSensitivity = value; }
    public float Tilt_limit { get => tilt_limit; set => tilt_limit = value; }
    public override void HandleReady()
    {
        base.HandleReady();
    }

    public override void HandleEnter() { }

    public override void HandleExit() { }

    public override void HandleProcess(double delta)
    {
        RotateCamera();
    }

    public override void HandlePhysicsProcess(double delta)
    {
        pivot.Position = target.Position;
    }

    private void RotateCamera()
    {
        Vector2 rotationAmount = inputProvider.GetLookInput();
        Vector3 tmpPivotRot = pivot.Rotation;
        tmpPivotRot.X += rotationAmount.Y * lookSensitivity;
        tmpPivotRot.X = Mathf.Clamp(tmpPivotRot.X, -tilt_limit, tilt_limit);
        tmpPivotRot.Y += -rotationAmount.X * lookSensitivity;
        pivot.Rotation = tmpPivotRot;
    }
}
