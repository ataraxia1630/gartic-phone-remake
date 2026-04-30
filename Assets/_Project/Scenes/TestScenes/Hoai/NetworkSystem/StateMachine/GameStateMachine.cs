using System;
using System.Collections.Generic;
using Fusion;
using GarticPhone.Network.Core;
using GarticPhone.Network.GameModes;
using GarticPhone.Network.Players;
using GarticPhone.Network.StateMachine.States;

namespace GarticPhone.Network.StateMachine
{
    public class GameStateMachine : NetworkBehaviour
    {
        [Networked] public GameStateType CurrentState { get; set; }
        [Networked] public GameModeType SelectedMode { get; set; }

        public event Action<GameStateType> OnStateChanged;

        private readonly Dictionary<GameStateType, IGameState> _states = new Dictionary<GameStateType, IGameState>();
        private GameStateType _lastObservedState;
        private bool _hasObserved;

        public override void Spawned()
        {
            _states[GameStateType.Lobby] = new LobbyState();
            _states[GameStateType.Playing] = new PlayingState();
            _states[GameStateType.Results] = new ResultsState();

            ServiceLocator.Register<GameStateMachine>(this);

            if (HasStateAuthority)
            {
                CurrentState = GameStateType.Lobby;
                SelectedMode = GameModeType.Sandwich;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Unregister<GameStateMachine>(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (_states.TryGetValue(CurrentState, out var state)) state.Tick(this);
        }

        public override void Render()
        {
            if (!_hasObserved)
            {
                _hasObserved = true;
                _lastObservedState = CurrentState;
                if (_states.TryGetValue(CurrentState, out var initial)) initial.OnEnter(this);
                OnStateChanged?.Invoke(CurrentState);
                return;
            }

            if (CurrentState != _lastObservedState)
            {
                if (_states.TryGetValue(_lastObservedState, out var prev)) prev.OnExit(this);
                _lastObservedState = CurrentState;
                if (_states.TryGetValue(CurrentState, out var next)) next.OnEnter(this);
                OnStateChanged?.Invoke(CurrentState);
            }
        }

        public void Transition(GameStateType next)
        {
            if (!HasStateAuthority) return;
            CurrentState = next;
        }

        public void SetSelectedMode(GameModeType mode)
        {
            if (!HasStateAuthority) return;
            SelectedMode = mode;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_RequestStart(RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            if (CurrentState != GameStateType.Lobby) return;

            var registry = ServiceLocator.Get<PlayerRegistry>();
            var bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            var config = bootstrap != null ? bootstrap.Config : null;
            if (registry == null || config == null) return;
            if (registry.ConnectedCount() < config.MinPlayersToStart) return;
            if (!registry.AreAllConnectedReady()) return;

            Transition(GameStateType.Playing);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_RequestSetMode(GameModeType mode, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            if (CurrentState != GameStateType.Lobby) return;
            SelectedMode = mode;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_RequestReturnToLobby(RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            if (CurrentState != GameStateType.Results) return;
            Transition(GameStateType.Lobby);
        }
    }
}
