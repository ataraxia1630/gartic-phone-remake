namespace GarticPhone.Network.StateMachine
{
    public abstract class GameStateBase : IGameState
    {
        public abstract GameStateType Type { get; }
        public virtual void OnEnter(GameStateMachine machine) { }
        public virtual void Tick(GameStateMachine machine) { }
        public virtual void OnExit(GameStateMachine machine) { }
    }
}
