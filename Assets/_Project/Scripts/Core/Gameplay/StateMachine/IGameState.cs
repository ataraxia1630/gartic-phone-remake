namespace InkEcho.Network.StateMachine
{
    public interface IGameState
    {
        GameStateType Type { get; }
        void OnEnter(GameStateMachine machine);
        void Tick(GameStateMachine machine);
        void OnExit(GameStateMachine machine);
    }
}
