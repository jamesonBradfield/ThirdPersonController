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
        // could be a case that a leaf state won't set its data, leaf states should set data, and execute signal expressions IE, our active Child bool should check if we have children and an active state, if we have no active child but also have no children, we are a leaf and should still execute, else we should probably check our condition to see if we should still be in the state. IE (if we are in grounded and idle/walk/run aren't firing, we must be Airborne and there is some bug "this should be impossible given the nature of our system but just making sure")
        //
        // TLDR
        // just because we don't have an active child doesn't mean we shouldn't process signalExpressions (if we are the walk state "a leaf with no active child" currently our signalExpression isn't being executed)
        if (activeChild == null)
            return;

        if (!string.IsNullOrEmpty(activeChild.signalExpression))
            signalExpr.Execute(new Godot.Collections.Array { velocityHandler, genericData });
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
