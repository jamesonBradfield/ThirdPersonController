using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class StateMachine : Node
{
    [Signal] public delegate void StateChangedEventHandler();
    [Export] State startState;
    public State currentState;
    State lastState;
    List<State> states = new List<State>();
    //TODO: we should add a system for auto naming states, (this is feature creep for now, but in a enterprise system it should be standard to avoid losing minutes every dev hour).
    public override void _Ready()
    {
        foreach (State child in GetChildren())
            states.Add(child);
        currentState = startState;
        currentState._Enter();
        currentState._Ready();
    }

    public override void _Process(double delta)
    {
        currentState._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        currentState._PhysicsProcess(delta);
    }

    public void ChangeState(string newStateName)
    {
        lastState = currentState;
        currentState._Exit();
        currentState = states.Find(i => i.Name == newStateName);
        if (currentState != null)
        {
            currentState._Enter();
            EmitSignal("StateChanged");
        }
        else
        {
            Logger.Error("State does not exist! Did you spell its name right? (it should be " + newStateName + " in Godot Inspector) double check it's correct!");
        }
    }
}
