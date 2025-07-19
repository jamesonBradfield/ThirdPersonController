using Godot;
using System;
using GodotTools;

/// <summary>
/// Utility script to validate that all exports are properly assigned.
/// Add this to any node temporarily to check your setup.
/// 
/// USAGE:
/// 1. Create an empty Node as child of Player
/// 2. Assign this script to the node
/// 3. Run the game and check console output
/// 4. Fix any issues reported
/// 5. Remove this node when everything is working
/// </summary>
public partial class ExportValidator : Node
{
    public override void _Ready()
    {
        GodotLogger.Debug("=== EXPORT VALIDATION START ===");
        ValidatePlayerSetup();
        ValidateStateMachineSetup();
        ValidateInputProviderSetup();
        ValidateStateSetup();
        GodotLogger.Debug("=== EXPORT VALIDATION COMPLETE ===");
    }

    void ValidatePlayerSetup()
    {
        GodotLogger.Debug("--- Validating Player ---");
        Player player = GetParent<Player>();
        if (player == null)
        {
            GodotLogger.Warning("ExportValidator must be child of Player node");
            return;
        }
        if (player.stateMachine == null)
            GodotLogger.Warning("❌ Player.stateMachine not assigned");
        else
            GodotLogger.Debug("✅ Player.stateMachine assigned");
        if (player.cameraPivot == null)
            GodotLogger.Warning("❌ Player.cameraPivot not assigned");
        else
            GodotLogger.Debug("✅ Player.cameraPivot assigned");
    }

    void ValidateStateMachineSetup()
    {
        GodotLogger.Debug("--- Validating StateMachine ---");
        StateMachine stateMachine = GetParent().GetNode<StateMachine>("StateMachine");
        if (stateMachine == null)
        {
            GodotLogger.Warning("❌ StateMachine node not found as child of Player");
            return;
        }

        // Use reflection to access rootState since it's not public
        var rootStateField = typeof(StateMachine).GetField("rootState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rootStateField != null)
        {
            State rootState = (State)rootStateField.GetValue(stateMachine);
            if (rootState == null)
                GodotLogger.Warning("❌ StateMachine.rootState not assigned - select StateMachine and assign FreeLookBehavior");
            else
                GodotLogger.Debug($"✅ StateMachine.rootState assigned to: {rootState.Name}");
        }
    }

    void ValidateInputProviderSetup()
    {
        GodotLogger.Debug("--- Validating InputProvider ---");
        PlayerInputProvider inputProvider = GetParent().GetNode<PlayerInputProvider>("PlayerInputProvider");
        if (inputProvider == null)
        {
            GodotLogger.Warning("❌ PlayerInputProvider node not found as child of Player");
            return;
        }

        // Check if cameraPivot is assigned
        var cameraPivotField = typeof(PlayerInputProvider).GetField("cameraPivot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (cameraPivotField != null)
        {
            Node3D cameraPivot = (Node3D)cameraPivotField.GetValue(inputProvider);
            if (cameraPivot == null)
                GodotLogger.Warning("❌ PlayerInputProvider.cameraPivot not assigned");
            else
                GodotLogger.Debug("✅ PlayerInputProvider.cameraPivot assigned");
        }
    }

    void ValidateStateSetup()
    {
        GodotLogger.Debug("--- Validating States ---");
        StateMachine stateMachine = GetParent().GetNode<StateMachine>("StateMachine");
        if (stateMachine == null) return;

        // Check FreeLookBehavior
        FreeLookBehavior freeLook = stateMachine.GetNode<FreeLookBehavior>("FreeLookBehavior");
        if (freeLook == null)
        {
            GodotLogger.Warning("❌ FreeLookBehavior node not found");
            return;
        }
        ValidatePlayerStateExports("FreeLookBehavior", freeLook);
        ValidateDefaultSubstate("FreeLookBehavior", freeLook);

        // Check IdleState
        ValidateStateNode("Idle", freeLook);

        // Check LocomotionBehavior
        LocomotionBehavior locomotion = freeLook.GetNode<LocomotionBehavior>("LocomotionBehavior");
        if (locomotion != null)
        {
            ValidatePlayerStateExports("LocomotionBehavior", locomotion);
            ValidateDefaultSubstate("LocomotionBehavior", locomotion);

            // Check WalkState
            ValidateStateNode("Walk", locomotion);
        }
        else
        {
            GodotLogger.Warning("❌ LocomotionBehavior node not found under FreeLookBehavior");
        }
    }

    void ValidateStateNode(string nodeName, Node parent)
    {
        PlayerState state = parent.GetNode<PlayerState>(nodeName);
        if (state == null)
        {
            GodotLogger.Warning($"❌ {nodeName} node not found under {parent.Name}");
            return;
        }
        ValidatePlayerStateExports(nodeName, state);
    }

    void ValidatePlayerStateExports(string stateName, PlayerState state)
    {
        // Check player assignment
        var playerField = typeof(PlayerState).GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerField != null)
        {
            Player player = (Player)playerField.GetValue(state);
            if (player == null)
                GodotLogger.Warning($"❌ {stateName}.player not assigned");
            else
                GodotLogger.Debug($"✅ {stateName}.player assigned");
        }
    }

    void ValidateDefaultSubstate(string stateName, State state)
    {
        var defaultSubstateField = typeof(State).GetField("defaultSubstate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (defaultSubstateField != null)
        {
            State defaultSubstate = (State)defaultSubstateField.GetValue(state);
            if (defaultSubstate == null)
                GodotLogger.Warning($"❌ {stateName}.defaultSubstate not assigned");
            else
                GodotLogger.Debug($"✅ {stateName}.defaultSubstate assigned to: {defaultSubstate.Name}");
        }
    }
}
