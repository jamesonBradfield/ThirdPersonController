using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GodotTools;

/// <summary>
/// Hierarchical Finite State Machine (HFSM) that uses Godot's scene tree for organization.
/// 
/// KEY CONCEPTS:
/// 
/// 1. STATE PATH: Instead of tracking a single "current state", we track a PATH of states
///    from root to leaf. For example: [FreeLookBehavior, LocomotionBehavior, WalkState]
/// 
/// 2. RELINQUISHING CONTROL: The StateMachine doesn't micromanage states. Instead:
///    - It builds the active state path
///    - It calls the ROOT state's Process/PhysicsProcess methods
///    - Each state in the path handles its own logic, then delegates to its substate
///    - This creates a natural chain of delegation down to the leaf state
/// 
/// 3. SMART TRANSITIONS: When changing states, it calculates the minimal set of
///    exits and enters needed. States that remain in the path don't get exited/re-entered.
/// 
/// SCENE TREE EXAMPLE:
/// StateMachine
/// └── FreeLookBehavior (provides camera control)
///     ├── IdleState (can look around while idle)
///     └── LocomotionBehavior (provides movement)
///         └── WalkState (specific movement type)
/// 
/// ACTIVE PATH: [FreeLookBehavior, LocomotionBehavior, WalkState]
/// DELEGATION: StateMachine calls FreeLookBehavior.Process() 
///            -> FreeLookBehavior calls LocomotionBehavior.Process()
///            -> LocomotionBehavior calls WalkState.Process()
/// </summary>
[GlobalClass]
public partial class StateMachine : Node
{
    [Signal] public delegate void StateChangedEventHandler();
    [Export] State rootState;

    /// <summary>
    /// The current active state path from root to leaf.
    /// Example: [FreeLookBehavior, LocomotionBehavior, WalkState]
    /// This represents the complete hierarchy of active states.
    /// </summary>
    List<State> currentStatePath = new List<State>();

    /// <summary>
    /// Cache of all states in the tree for fast lookup by name.
    /// Populated once during _Ready().
    /// </summary>
    List<State> allStates = new List<State>();

    public override void _Ready()
    {
        // Build our cache of all available states
        CollectAllStates();

        if (rootState == null)
            return;

        // Start the machine by entering the root state
        EnterState(rootState);
    }

    /// <summary>
    /// DELEGATION TO HIERARCHY: The StateMachine doesn't process states directly.
    /// Instead, it calls the ROOT state's Process() method, which then delegates
    /// down the hierarchy automatically via the State base class.
    /// 
    /// Flow: StateMachine._Process() -> RootState.Process() -> SubState.Process() -> LeafState.Process()
    /// </summary>
    public override void _Process(double delta)
    {
        if (currentStatePath.Count == 0)
            return;

        // Call the root state's Process method - it handles the rest of the hierarchy
        currentStatePath[0].Process(delta);
    }

    /// <summary>
    /// Same delegation pattern as _Process, but for physics.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (currentStatePath.Count == 0)
            return;

        // Call the root state's PhysicsProcess method - it handles the rest of the hierarchy
        currentStatePath[0].PhysicsProcess(delta);
    }

    /// <summary>
    /// Changes to a state by name. Finds the state and delegates to ChangeState(State).
    /// </summary>
    public void ChangeState(string stateName)
    {
        State targetState = FindState(stateName);

        if (targetState == null)
        {
            GodotLogger.Warning($"State '{stateName}' not found in hierarchy");
            return;
        }

        TransitionToState(targetState);
    }

    /// <summary>
    /// Changes to a specific state instance.
    /// </summary>
    public void ChangeState(State targetState)
    {
        if (targetState == null)
        {
            GodotLogger.Warning("Target state is null");
            return;
        }

        TransitionToState(targetState);
    }

    /// <summary>
    /// Gets the deepest (leaf) state in the current active path.
    /// This is usually what you think of as the "current state".
    /// </summary>
    public State GetCurrentLeafState()
    {
        if (currentStatePath.Count == 0)
            return null;

        return currentStatePath.Last();
    }

    /// <summary>
    /// Gets the active state at a specific level in the hierarchy.
    /// Level 0 = root state, Level 1 = first substate, etc.
    /// Used by the State base class to find its active substates.
    /// </summary>
    public State GetActiveStateAtLevel(int level)
    {
        if (level >= currentStatePath.Count)
            return null;

        return currentStatePath[level];
    }

    /// <summary>
    /// Checks if a state (by name) is anywhere in the current active path.
    /// Useful for checking if you're "in" a behavior that might have substates.
    /// </summary>
    public bool StateActive(string stateName)
    {
        return currentStatePath.Any(state => state.Name == stateName);
    }

    /// <summary>
    /// Checks if a specific state instance is anywhere in the current active path.
    /// </summary>
    public bool StateActive(State state)
    {
        return currentStatePath.Contains(state);
    }

    /// <summary>
    /// Builds a cache of all states in the scene tree for fast lookup.
    /// Called once during _Ready().
    /// </summary>
    void CollectAllStates()
    {
        allStates.Clear();
        CollectStatesRecursive(this);
    }

    /// <summary>
    /// Recursively walks the scene tree to find all State nodes.
    /// </summary>
    void CollectStatesRecursive(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is State state)
                allStates.Add(state);

            CollectStatesRecursive(child);
        }
    }

    /// <summary>
    /// Finds a state by name from our cached list.
    /// </summary>
    State FindState(string stateName)
    {
        return allStates.FirstOrDefault(state => state.Name == stateName);
    }

    /// <summary>
    /// SMART TRANSITION LOGIC: This is the heart of the HFSM.
    /// 
    /// Instead of exiting all states and entering all new states, we:
    /// 1. Build the path to the target state
    /// 2. Find the common path between current and target
    /// 3. Only exit states that aren't in the target path
    /// 4. Only enter states that aren't already active
    /// 
    /// Example transition from [A, B, C] to [A, D, E]:
    /// - Common path: [A]
    /// - Exit: [C, B] (in reverse order)
    /// - Enter: [D, E] (in forward order)
    /// - Result: [A, D, E]
    /// 
    /// This preserves state in parent behaviors during transitions.
    /// </summary>
    void TransitionToState(State targetState)
    {
        // Build the full path to the target state
        List<State> targetPath = BuildStatePath(targetState);

        // Find which states are common between current and target paths
        List<State> commonPath = FindCommonStatePath(currentStatePath, targetPath);

        // Exit states that won't be in the new path
        ExitToCommonAncestor(commonPath);

        // Enter states that aren't already active
        EnterFromCommonAncestor(targetPath, commonPath);

        // Update our current path
        currentStatePath = targetPath;
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Enters a state directly without transition logic.
    /// Used during initial startup. For normal operation, use TransitionToState.
    /// </summary>
    void EnterState(State state)
    {
        currentStatePath = BuildStatePath(state);

        // Enter each state in the path from root to leaf
        foreach (State pathState in currentStatePath)
        {
            pathState.HandleEnter();
        }

        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Builds the complete path from root to a given leaf state.
    /// 
    /// Example: For WalkState under LocomotionBehavior under FreeLookBehavior,
    /// returns [FreeLookBehavior, LocomotionBehavior, WalkState]
    /// 
    /// This path represents the complete hierarchy that needs to be active
    /// for the target state to function properly.
    /// </summary>
    List<State> BuildStatePath(State leafState)
    {
        List<State> path = new List<State>();
        State current = leafState;

        // Walk up the tree until we hit the StateMachine
        while (current != null && current.GetParent() != this)
        {
            path.Insert(0, current);  // Insert at beginning to build root-to-leaf order
            current = current.GetParent<State>();
        }

        // Add the root state if we found one
        if (current != null)
            path.Insert(0, current);

        return path;
    }

    /// <summary>
    /// Finds the common prefix between two state paths.
    /// 
    /// Example: [A, B, C] and [A, D, E] returns [A]
    /// This represents the states that don't need to be exited/re-entered.
    /// </summary>
    List<State> FindCommonStatePath(List<State> pathA, List<State> pathB)
    {
        List<State> commonPath = new List<State>();
        int minLength = Math.Min(pathA.Count, pathB.Count);

        // Compare paths element by element from the beginning
        for (int i = 0; i < minLength; i++)
        {
            if (pathA[i] != pathB[i])
                break;

            commonPath.Add(pathA[i]);
        }

        return commonPath;
    }

    /// <summary>
    /// Exits states from the current path down to the common ancestor.
    /// States are exited in reverse order (children first, then parents).
    /// </summary>
    void ExitToCommonAncestor(List<State> commonPath)
    {
        // Exit from leaf to root, stopping at the common ancestor
        for (int i = currentStatePath.Count - 1; i >= commonPath.Count; i--)
        {
            currentStatePath[i].HandleExit();
        }
    }

    /// <summary>
    /// Enters states from the common ancestor to the target leaf.
    /// States are entered in forward order (parents first, then children).
    /// </summary>
    void EnterFromCommonAncestor(List<State> targetPath, List<State> commonPath)
    {
        // Enter from root to leaf, starting after the common ancestor
        for (int i = commonPath.Count; i < targetPath.Count; i++)
        {
            targetPath[i].HandleEnter();
        }
    }
}
