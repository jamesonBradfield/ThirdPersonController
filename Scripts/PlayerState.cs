using Godot;
using System;

public abstract partial class PlayerState : State
{
    [Export] protected AnimationPlayer animationPlayer;
    [Export] protected Player player;
    public Vector3 velocity;
}
