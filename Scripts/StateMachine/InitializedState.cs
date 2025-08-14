using Godot;
using GodotTools;

public partial class InitializedState : State
{
    public override void HandleReady()
    {
        // Find our state machine by walking up the tree
        stateMachine = GetStateMachine();
        inputProvider = stateMachine.inputProvider;
        GodotLogger.Debug((inputProvider != null) ? "inputProvider Found" : "inputProvider NOT FOUND!");
        body3D = stateMachine.body3D;
        animationTree = body3D.FindChild("AnimationTree") as AnimationTree;
    }
    public override void HandleProcess(double delta)
    {

    }
    public override void HandlePhysicsProcess(double delta)
    {

    }
    public override void HandleEnter()
    {

    }
    public override void HandleExit()
    {

    }
}
