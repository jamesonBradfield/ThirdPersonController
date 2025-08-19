using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotTools;
[GlobalClass]
public partial class State : Node
{
    [ExportGroup("Activation")]
    [Export] string condition = "true";
    [Export] int priority = 0;

    [ExportGroup("Signal Processing")]
    [Export] string signalExpression = "";
    Variant genericData;

    [ExportGroup("Velocity Configuration")]
    [Export] Vector3 velocityImpulse;
    [Export] float acceleration;
    [Export] float maxSpeed;
    [Export] bool inheritVelocity = true;

    CharacterVelocityHandler velocityHandler;
    Player player;
    List<State> children = new();
    State activeChild;
    Expression conditionExpr = new();
    Expression signalExpr = new();

    // Track if we're active in our parent's context
    bool isActive = false;

    public override void _Ready()
    {
        player = GetNode<Player>("/root/Main/Player");
        velocityHandler = player.GetNode<CharacterVelocityHandler>("CharacterVelocityHandler");
        children = GetChildren().OfType<State>()
            .OrderBy(s => s.priority)
            .ToList();

        conditionExpr.Parse(condition, new[] { "player", "velocity" });
        if (!string.IsNullOrEmpty(signalExpression))
            signalExpr.Parse(signalExpression, new[] { "velocity", "genericData" });
    }

    public void OnSignal(Variant data)
    {
        genericData = data;

        // If we have an active child, let it handle the signal
        if (activeChild != null)
        {
            activeChild.OnSignal(data);
            return;
        }

        // We're a leaf state - execute our own signal expression
        if (!string.IsNullOrEmpty(signalExpression))
        {
            signalExpr.Execute(new Godot.Collections.Array { velocityHandler, genericData });
            GodotLogger.Info("genericData : " + genericData + "\nVelocity : " + velocityHandler.Velocity);
        }
    }
    bool CanActivate()
    {
        if (condition == "true")
            return true;

        var result = conditionExpr.Execute(new Godot.Collections.Array { player, velocityHandler });
        return result.VariantType == Variant.Type.Bool && result.AsBool();
    }

    public void Process(double delta)
    {
        if (!CanActivate())
        {
            Deactivate();
            return;
        }

        State bestChild = children.FirstOrDefault(c => c.CanActivate())
                          ?? children.FirstOrDefault(c => c.Name == "Idle");

        if (bestChild != activeChild)
        {
            activeChild?.OnExit();
            activeChild = bestChild;
            activeChild?.OnEnter();
        }

        activeChild?.Process(delta);
    }

    public void OnEnter()
    {
        isActive = true;
        GodotLogger.Debug($"Entering state: {Name}");

        // Apply our velocity configuration
        velocityHandler.MaxSpeed = maxSpeed;
        velocityHandler.Acceleration = acceleration;

        if (!inheritVelocity)
            velocityHandler.Velocity = Vector3.Zero;

        if (velocityImpulse != Vector3.Zero)
        {
            velocityHandler.Velocity += velocityImpulse;
            velocityImpulse = Vector3.Zero;  // Only apply once
        }

        // Find and activate our best child
        State bestChild = children.FirstOrDefault(c => c.CanActivate())
                          ?? children.FirstOrDefault(c => c.Name == "Idle");

        if (bestChild != null)
        {
            activeChild = bestChild;
            activeChild.OnEnter();
        }
    }

    void OnExit()
    {
        isActive = false;
        GodotLogger.Debug($"Exiting state: {Name}");

        // Exit our active child first
        activeChild?.OnExit();
        activeChild = null;

        // Any cleanup for this state
        // Reset any state-specific flags if needed
    }

    void Deactivate()
    {
        if (!isActive)
            return;

        // Recursively deactivate children
        activeChild?.Deactivate();
        activeChild = null;

        // Mark ourselves as inactive
        isActive = false;
        GodotLogger.Debug($"Deactivating state: {Name}");
    }
}
