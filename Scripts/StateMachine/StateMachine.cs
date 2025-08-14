using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotTools;
// NOTE: there is a way of restructuring this so we don't need a controller, or complex state machine logic, and that is, every state has a priority and defines a function its parent state grabs and organizes by priority, every state does this on ready and checks
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
    [Export] InitializedState rootState;
    [Export] public CharacterBody3D body3D;
    public IInputWrapper inputProvider;
    /// <summary>
    /// The current active state path from root to leaf.
    /// Example: [FreeLookBehavior, LocomotionBehavior, WalkState]
    /// This represents the complete hierarchy of active states.
    /// </summary>
    List<InitializedState> currentStatePath = new List<InitializedState>();

    /// <summary>
    /// Cache of all states in the tree for fast lookup by name.
    /// Populated once during _Ready().
    /// </summary>
    List<InitializedState> allStates = new List<InitializedState>();


    public override void _Ready()
    {
        foreach (Node child in body3D.GetChildren())
        {
            if (child is IInputWrapper)
                inputProvider = child as IInputWrapper;
        }
        GodotLogger.Debug((inputProvider != null) ? "inputProvider Found" : "inputProvider NOT FOUND!");
        // Build our cache of all available states
        CollectAllStates();
        GodotLogger.Debug($"Found {allStates.Count} total states in scene tree");
        // Call HandleReady on all states
        foreach (InitializedState state in allStates)
        {
            state.HandleReady();
        }
        // List all found states for debugging
        foreach (InitializedState state in allStates)
        {
            GodotLogger.Debug($"  - {state.Name} ({state.GetType().Name})");
        }

        if (rootState == null)
        {
            GodotLogger.Warning("CRITICAL: StateMachine.rootState is not assigned! Select the StateMachine node and assign the root state in the inspector.");
            return;
        }

        GodotLogger.Debug($"Root state assigned: {rootState.Name}");

        // Start the machine by entering the root state
        EnterState(rootState);

        GodotLogger.Debug("StateMachine initialization complete");
        LogCurrentStatePath();
    }

    /// <summary>
    /// DELEGATION TO HIERARCHY: The StateMachine doesn't process states directly.
    /// Instead, it calls the ROOT state's Process() method, which then delegates
    /// down the hierarchy automatically via the State base class.
    ///
    /// Flow: StateMachine._Process() -> RootState.Process() -> SubState.Process() -> LeafState.Process()
    /// </summary>
    // In StateMachine._Process()
    public override void _Process(double delta)
    {
        foreach (InitializedState activeState in currentStatePath)
        {
            activeState.HandleProcess(delta);
        }
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
        GodotLogger.Debug($"Attempting to change to state: {stateName}");

        InitializedState targetState = FindState(stateName);

        if (targetState == null)
        {
            GodotLogger.Warning($"State '{stateName}' not found in hierarchy. Available states:");
            foreach (InitializedState state in allStates)
            {
                GodotLogger.Warning($"  - {state.Name}");
            }
            return;
        }

        GodotLogger.Debug($"Found target state: {targetState.Name} ({targetState.GetType().Name})");
        TransitionToState(targetState);
    }

    /// <summary>
    /// Changes to a specific state instance.
    /// </summary>
    public void ChangeState(InitializedState targetState)
    {
        if (targetState == null)
        {
            GodotLogger.Warning("Target state is null");
            return;
        }

        GodotLogger.Debug($"Changing to state instance: {targetState.Name}");
        TransitionToState(targetState);
    }

    /// <summary>
    /// Gets the deepest (leaf) state in the current active path.
    /// This is usually what you think of as the "current state".
    /// </summary>
    public InitializedState GetCurrentLeafState()
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
    public InitializedState GetActiveStateAtLevel(int level)
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
    public bool StateActive(InitializedState state)
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
            if (child is InitializedState state)
                allStates.Add(state);

            CollectStatesRecursive(child);
        }
    }

    /// <summary>
    /// Finds a state by name from our cached list.
    /// </summary>
    public InitializedState FindState(string stateName)
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
    void TransitionToState(InitializedState targetState)
    {
        GodotLogger.Debug($"Transition From: {GetCurrentStatePathString()}");

        // Build the full path to the target state
        List<InitializedState> targetPath = BuildStatePath(targetState);
        GodotLogger.Debug($"Transition To: {GetStatePathString(targetPath)}");

        // Find which states are common between current and target paths
        List<InitializedState> commonPath = FindCommonStatePath(currentStatePath, targetPath);
        GodotLogger.Debug($"Transition Common: {GetStatePathString(commonPath)}");

        // Exit states that won't be in the new path
        ExitToCommonAncestor(commonPath);

        // Enter states that aren't already active
        EnterFromCommonAncestor(targetPath, commonPath);

        // Update our current path to reflect the new active states
        currentStatePath = targetPath;
        EmitSignal(SignalName.StateChanged);

        GodotLogger.Debug($"Final State Path: {GetCurrentStatePathString()}");
    }

    /// <summary>
    /// Enters a state directly without transition logic.
    /// Used during initial startup. For normal operation, use TransitionToState.
    /// </summary>
    void EnterState(InitializedState state)
    {
        GodotLogger.Debug($"Entering root state: {state.Name}");

        // IMPORTANT: Call Enter() not HandleEnter() to trigger orchestration layer
        // This ensures default substates are automatically entered
        state.Enter();

        // After the orchestration layer has done its work, rebuild the current path
        // by walking down the active state chain
        currentStatePath = BuildActiveStatePath();

        GodotLogger.Debug($"Final state path after orchestration: {GetCurrentStatePathString()}");
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Builds the current active state path by walking down from the root state
    /// through each default substate until reaching the leaf.
    /// This follows the same path that the orchestration layer creates.
    /// </summary>
    List<InitializedState> BuildActiveStatePath()
    {
        List<InitializedState> path = new List<InitializedState>();

        if (rootState == null) return path;

        InitializedState current = rootState;
        path.Add(current);

        // Follow the defaultSubstate chain - this matches what Enter() actually does
        while (current != null)
        {
            InitializedState defaultSubstate = GetDefaultSubstate(current);
            if (defaultSubstate == null) break;

            GodotLogger.Debug($"Following defaultSubstate: {current.Name} -> {defaultSubstate.Name}");
            path.Add(defaultSubstate);
            current = defaultSubstate;
        }

        return path;
    }

    /// <summary>
    /// Gets the default substate of a state using reflection.
    /// This is needed because defaultSubstate is protected.
    /// </summary>
    InitializedState GetDefaultSubstate(InitializedState state)
    {
        var defaultSubstateField = typeof(InitializedState).GetField("defaultSubstate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (defaultSubstateField != null)
        {
            return (InitializedState)defaultSubstateField.GetValue(state);
        }

        return null;
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
    List<InitializedState> BuildStatePath(InitializedState leafState)
    {
        List<InitializedState> path = new List<InitializedState>();
        InitializedState current = leafState;

        // Walk up the tree until we hit the StateMachine
        while (current != null && current.GetParent() != this)
        {
            path.Insert(0, current);  // Insert at beginning to build root-to-leaf order
            current = current.GetParent<InitializedState>();
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
    List<InitializedState> FindCommonStatePath(List<InitializedState> pathA, List<InitializedState> pathB)
    {
        List<InitializedState> commonPath = new List<InitializedState>();
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
    void ExitToCommonAncestor(List<InitializedState> commonPath)
    {
        // Exit from leaf to root, stopping at the common ancestor
        for (int i = currentStatePath.Count - 1; i >= commonPath.Count; i--)
        {
            GodotLogger.Debug($"Calling HandleExit() on: {currentStatePath[i].Name}");
            currentStatePath[i].HandleExit();
        }
    }

    /// <summary>
    /// Enters states from the common ancestor to the target leaf.
    /// States are entered in forward order (parents first, then children).
    /// </summary>
    void EnterFromCommonAncestor(List<InitializedState> targetPath, List<InitializedState> commonPath)
    {
        // Enter from root to leaf, starting after the common ancestor
        for (int i = commonPath.Count; i < targetPath.Count; i++)
        {
            GodotLogger.Debug($"Calling HandleEnter() on: {targetPath[i].Name}");
            targetPath[i].HandleEnter();
        }
    }

    /// <summary>
    /// Logs the current state path for debugging.
    /// </summary>
    void LogCurrentStatePath()
    {
        GodotLogger.Debug($"Current state path: {GetCurrentStatePathString()}");
    }

    /// <summary>
    /// Gets the current state path as a readable string.
    /// </summary>
    string GetCurrentStatePathString()
    {
        return GetStatePathString(currentStatePath);
    }

    /// <summary>
    /// Gets the current state path as a list of InitializedStates.
    /// </summary>
    public List<InitializedState> GetCurrentStatePath()
    {
        return currentStatePath;
    }
    /// <summary>
    /// Converts a state path to a readable string.
    /// </summary>
    string GetStatePathString(List<InitializedState> path)
    {
        if (path.Count == 0) return "[empty]";
        return "[" + string.Join(" -> ", path.Select(s => s.Name)) + "]";
    }
}
