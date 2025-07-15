using Godot;
using System;

/// <summary>
/// Base abstract class for all states in the hierarchical finite state machine.
/// 
/// ARCHITECTURE OVERVIEW:
/// This class provides two layers of functionality:
/// 
/// 1. ORCHESTRATION LAYER (Enter, Exit, Process, PhysicsProcess):
///    - These methods handle hierarchical state management
///    - They automatically delegate to substates in the scene tree
///    - They maintain the parent/child relationships
///    - YOU SHOULD NOT OVERRIDE THESE unless you need custom hierarchy behavior
/// 
/// 2. IMPLEMENTATION LAYER (HandleXxx methods):
///    - These are the methods you override in your concrete states
///    - They contain your actual state logic (movement, animations, etc.)
///    - They are called BY the orchestration layer
///    - This is where your state behavior goes
/// </summary>
public abstract partial class State : Node
{
    [Export] protected State defaultSubstate;
    protected StateMachine stateMachine;
    protected State parentState;

    // IMPLEMENTATION LAYER - Override these in your concrete states
    public abstract void HandleReady();
    public abstract void HandleProcess(double delta);
    public abstract void HandlePhysicsProcess(double delta);
    public abstract void HandleEnter();
    public abstract void HandleExit();

    public override void _Ready()
    {
        // Find our state machine by walking up the tree
        stateMachine = GetStateMachine();

        // Set our parent state (may be null if we're directly under StateMachine)
        Node parent = GetParent();
        if (parent is State state)
            parentState = state;
    }

    /// <summary>
    /// ORCHESTRATION LAYER: Manages hierarchical state entry.
    /// 
    /// When a state is entered:
    /// 1. Calls HandleEnter() for the state's custom logic
    /// 2. If this state has a defaultSubstate, automatically enters it
    /// 
    /// This creates a chain: ParentState.Enter() -> HandleEnter() -> ChildState.Enter()
    /// </summary>
    public virtual void Enter()
    {
        // First, let this state do its entry logic
        HandleEnter();

        // Then, if we have a default substate, enter it automatically
        if (defaultSubstate == null)
            return;
        defaultSubstate.Enter();
    }

    /// <summary>
    /// ORCHESTRATION LAYER: Manages hierarchical state exit.
    /// 
    /// When a state is exited:
    /// 1. First exits all substates (children go first)
    /// 2. Then calls HandleExit() for this state's cleanup logic
    /// 
    /// This ensures children are cleaned up before parents
    /// </summary>
    public virtual void Exit()
    {
        // First, exit any active substates
        ExitAllSubstates();

        // Then do our own exit logic
        HandleExit();
    }

    /// <summary>
    /// ORCHESTRATION LAYER: Manages hierarchical processing.
    /// 
    /// Each frame:
    /// 1. Calls HandleProcess() for this state's logic
    /// 2. Finds the currently active substate at the next level
    /// 3. Calls Process() on that substate (which repeats this pattern)
    /// 
    /// This creates a chain from root to leaf: Root.Process() -> Parent.Process() -> Leaf.Process()
    /// But each state's HandleProcess() only runs once per frame
    /// </summary>
    public virtual void Process(double delta)
    {
        // First, do this state's processing
        HandleProcess(delta);

        // Then delegate to the active substate
        State activeSubstate = GetActiveSubstate();
        if (activeSubstate == null)
            return;
        activeSubstate.Process(delta);
    }

    /// <summary>
    /// ORCHESTRATION LAYER: Manages hierarchical physics processing.
    /// Same pattern as Process(), but for physics updates.
    /// </summary>
    public virtual void PhysicsProcess(double delta)
    {
        // First, do this state's physics processing
        HandlePhysicsProcess(delta);

        // Then delegate to the active substate
        State activeSubstate = GetActiveSubstate();
        if (activeSubstate == null)
            return;
        activeSubstate.PhysicsProcess(delta);
    }

    /// <summary>
    /// Gets the currently active state at the next level down in the hierarchy.
    /// Used by the orchestration layer to know which child to delegate to.
    /// </summary>
    public State GetActiveSubstate()
    {
        if (stateMachine == null)
            return null;
        return stateMachine.GetActiveStateAtLevel(GetStateLevel() + 1);
    }

    /// <summary>
    /// Exits all substates of this state.
    /// Called during Exit() to ensure proper cleanup order.
    /// </summary>
    public void ExitAllSubstates()
    {
        State activeSubstate = GetActiveSubstate();
        if (activeSubstate == null)
            return;
        activeSubstate.Exit();
    }

    /// <summary>
    /// Calculates this state's level in the hierarchy.
    /// Level 0 = directly under StateMachine
    /// Level 1 = child of a level 0 state, etc.
    /// </summary>
    public int GetStateLevel()
    {
        int level = 0;
        Node current = this;

        // Walk up the tree until we hit the StateMachine
        while (current.GetParent() != stateMachine)
        {
            current = current.GetParent();
            level++;
        }
        return level;
    }

    /// <summary>
    /// Finds the StateMachine that owns this state by walking up the scene tree.
    /// </summary>
    public StateMachine GetStateMachine()
    {
        Node current = this;
        while (current != null)
        {
            if (current is StateMachine machine)
                return machine;
            current = current.GetParent();
        }
        return null;
    }
}
