# HFSM Signal-Based Refactor Plan

## Vision Statement

Transform our current Hierarchical Finite State Machine from a controller-driven, single-path system into a **signal-driven, compositional, self-organizing state system** that enables parallel state concerns and eliminates centralized transition logic.

## Current Pain Points

1. **Monolithic Controller Logic**: StateMachineController contains all transition logic in giant switch statements
2. **Single Path Limitation**: Cannot run Movement (Idle/Walk/Run) alongside Physics (Grounded/Airborne) simultaneously  
3. **Tight Coupling**: StateMachine crawls around on controller's orders instead of self-organizing
4. **Code Explosion**: Adding new states requires modifying controller logic, violating single responsibility
5. **Lack of Separation**: Cannot cleanly separate Grounded states (walk, run, idle, jump) from Airborne states (falling, landing)

## The Unified State Philosophy

### One Class, Infinite Configurations

There are no "parent states" or "child states" - just **State**. Each state automatically adapts its behavior based on scene tree structure:

**Leaf State** (Walk, Run, Idle):
- No children → just exposes `ChangeToMe()` and handles its logic
- Pure reactive behavior

**Container State** (Grounded, Airborne):  
- Has children → automatically wires up child signal connections
- Becomes a signal router through emergent behavior

**Mixed State** (FreeLook):
- Handles own logic (camera movement) AND manages children  
- Does both simultaneously without special code

**Root State** (StateMachine):
- Just another state that happens to be at the top
- Same rules apply, same behavior emerges

### Composition = Configuration

```
Grounded (State with children → automatic signal router)
├─ Walk (State without children → just reactive)  
├─ Run (State without children → just reactive)
└─ Idle (State without children → just reactive)
```

Move Walk under a different parent? Its routing changes automatically. No code modifications needed - behavior emerges from structure.

## The New Architecture Vision

### Core Principle: States Know When They Should Be Active

Instead of external controllers deciding transitions, each state becomes responsible for its own activation conditions:

```
StateMachine  
├─ Grounded (activates on: body3D.IsOnFloor())
│  ├─ Walk (activates on: MovementInput AND NOT RunHeld)
│  ├─ Run (activates on: MovementInput AND RunHeld)
│  └─ Idle (activates on: NOT MovementInput)
└─ Airborne (activates on: NOT body3D.IsOnFloor())
   ├─ Falling 
   └─ Gliding
```

### Signal-Driven Activation

States **expose** `ChangeToMe` functions that external signals call through logic gates:

```csharp
// Instead of controller polling:
if (hasMovementInput && runHeld) stateMachine.ChangeState("Run");

// Signal flow through logic gates:
inputProvider.MovementInput → AND Gate ← inputProvider.RunHeld
                            ↓
                    RunState.ChangeToMe()
```

### Compositional State System

Multiple states can be active simultaneously at different hierarchy levels:
- **Level 0**: FreeLook (camera control)
- **Level 1**: Grounded (physics state) 
- **Level 2**: Walk (movement state)

This allows orthogonal concerns (camera, physics, movement) to operate independently.

## Key Architectural Decisions

### 1. Single State Class Architecture
- One `State` class handles all scenarios
- States with children automatically become signal routers
- States without children just expose `ChangeToMe()` 
- Behavior emerges from scene tree composition

### 2. Blocking Windows (Dark Souls Rolling)
- States can define blocking periods where transitions are ignored
- Prevents state thrashing during critical animations
- Configurable per-state via exported duration

### 3. Compositional Conflict Resolution  
- Multiple states firing simultaneously is **additive**, not conflicting
- Grounded->Walk doesn't interfere with Airborne->AirMove
- Each hierarchy level resolves independently

### 4. Generic State Types
- `LocomotionState`: Movement with acceleration
- `ImpulseState`: One-frame burst of acceleration  
- States become data-driven with exported parameters

### 5. Resource-Based Configuration
- State hierarchy and transitions stored as `.tres` resources
- "Baking" system converts scene tree to portable data
- Enables visual node graph editing in the future

### 6. Global Logic Management
- Autoload handles signal logic evaluation (AND, OR, XOR, NOT)
- States register their activation conditions centrally
- Eliminates per-state polling logic

## Implementation Roadmap (5 x 30min Sprints)

### Sprint 1: Unified State Class
**Goal**: Create single State class that handles all scenarios

```csharp
public abstract partial class State : Node
{
    [Export] public float blockingWindowDuration = 0.0f;
    
    protected float blockingTimer = 0.0f;
    protected bool transitionsBlocked => blockingTimer > 0.0f;
    
    public void ChangeToMe()
    {
        if (transitionsBlocked) return;
        GetStateMachine()?.HandleStateChangeRequest(this);
    }
    
    public override void HandleEnter()
    {
        // If I have children, wire them up automatically
        if (GetChildren<State>().Count > 0)
            ConnectChildStates();
    }
    
    public override void HandleExit()
    {
        // If I have children, disconnect them automatically  
        if (GetChildren<State>().Count > 0)
            DisconnectChildStates();
    }
    
    void ConnectChildStates()
    {
        foreach (State child in GetChildren<State>())
            ConnectSignalsForChild(child);
    }
    
    void DisconnectChildStates()
    {
        foreach (State child in GetChildren<State>())
            DisconnectSignalsForChild(child);
    }
}
```

**Test Case**: Grounded state with Walk/Run/Idle children automatically manages their connections

### Sprint 2: Compositional StateMachine
**Goal**: Support multiple active states by hierarchy level

```csharp
public partial class StateMachine : Node
{
    Dictionary<int, State> activeStatesByLevel = new();
    
    public void HandleStateChangeRequest(State requestingState)
    {
        int stateLevel = requestingState.GetStateLevel();
        // Activate at appropriate level without affecting other levels
    }
}
```

**Test Case**: Grounded->Walk and FreeLook running simultaneously

### Sprint 3: State Transition Resource
**Goal**: Externalize transition data from code

```csharp
[GlobalClass]
public partial class StateTransitionResource : Resource
{
    [Export] public string stateName;
    [Export] public Godot.Collections.Array<string> signalPaths;
    [Export] public string logicExpression; // "A AND NOT B"
    [Export] public Godot.Collections.Array<string> canTransitionTo;
}
```

**Test Case**: Create `.tres` file for Walk state conditions

### Sprint 4: Resource Baking Script  
**Goal**: Convert scene hierarchy to portable resource

```csharp
public partial class StateMachineBaker : Node
{
    public static void BakeHierarchy(Node rootNode, string resourcePath)
    {
        // Extract all state transition data from scene tree
        // Save as unified resource file
    }
}
```

**Test Case**: Bake current hierarchy to `.tres` file

### Sprint 5: Global Logic Autoload
**Goal**: Centralized signal evaluation system

```csharp
public partial class SignalLogicManager : Node // Autoload
{
    Dictionary<string, bool> signalStates = new();
    
    public void RegisterLogic(string stateId, string expression, string[] signalPaths)
    {
        // Register state activation conditions
    }
    
    bool EvaluateExpression(string expression, string[] signalPaths)
    {
        // Parse and evaluate "A AND NOT B" style expressions
    }
}
```

**Test Case**: Register Walk state logic, verify activation on condition

## Long-Term Vision

### Visual Node Graph Editor
- Drag-and-drop state creation
- Visual signal connections  
- Real-time logic testing
- Export to resource files

### Complete Code Elimination
- New states added purely through composition
- No StateMachineController needed
- No hardcoded transition logic
- Pure data-driven state management

### Advanced Features
- State priority systems
- Conditional logic nodes (switches, gates)
- Animation integration
- Network state synchronization

## Migration Strategy

1. **Branch**: Create feature branch for experimentation
2. **Parallel Development**: Keep existing system while building new
3. **Incremental Testing**: Validate each sprint before proceeding  
4. **Gradual Migration**: Move states one by one to new system
5. **Legacy Removal**: Delete old controller when migration complete

## Success Criteria

✅ **Elimination of StateMachineController**  
✅ **Parallel state execution** (multiple states active simultaneously)  
✅ **Zero-code state addition** (pure scene tree composition)  
✅ **Emergent signal routing** (states automatically manage children)  
✅ **One class handles everything** (no parent/child distinctions)  
✅ **Maintainable transition logic** (distributed, not centralized)  

---

*This document represents the collaborative vision developed through rubber duck debugging sessions. The goal is not just to solve the immediate problem, but to evolve toward a more elegant, maintainable, and extensible state management system.*
