using Godot;
using System;

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

// func _unhandled_input(event: InputEvent) -> void:
// 	if event is InputEventMouseMotion:
// 		_camera_pivot.rotation.x -= event.relative.y * mouse_sensitivity
// # Prevent the camera from rotating too far up or down.
//         _camera_pivot.rotation.x = clampf(_camera_pivot.rotation.x, -tilt_limit, tilt_limit)
// 		_camera_pivot.rotation.y += -event.relative.x * mouse_sensitivity
//     }
