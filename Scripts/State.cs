using Godot;
using System;

public abstract partial class State : Node
{
    public abstract void _Ready();

    public abstract void _Process(double delta);

    public abstract void _PhysicsProcess(double delta);

    public abstract void _Enter();

    public abstract void _Exit();
}
