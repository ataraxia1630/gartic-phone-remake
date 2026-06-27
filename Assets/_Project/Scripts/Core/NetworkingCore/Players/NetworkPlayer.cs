using Fusion;
using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.StateMachine;
using UnityEngine;

namespace InkEcho.Network.Players
{
    public class NetworkPlayer : NetworkBehaviour
    {
        public static NetworkPlayer Local { get; private set; }

        [Networked] public NetworkString<_16> DisplayName { get; set; }
        [Networked] public NetworkBool IsReady { get; set; }

        private bool _registered;
        private bool _readyApplied;
        private bool _localReady;
        private string _localDisplayName;
        private bool _gameStateInit;
        private GameStateType _lastGameState;

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                Local = this;
                ServiceLocator.Register<NetworkPlayer>(this);

                var nickname = PlayerPrefs.GetString("gartic_nickname", $"Player{Object.InputAuthority.PlayerId}");
                if (string.IsNullOrWhiteSpace(nickname)) nickname = $"Player{Object.InputAuthority.PlayerId}";
                _localDisplayName = nickname.Length > 16 ? nickname.Substring(0, 16) : nickname;
            }

            if (HasStateAuthority && HasInputAuthority)
            {
                DisplayName = _localDisplayName;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Local == this)
            {
                Local = null;
                ServiceLocator.Unregister<NetworkPlayer>(this);
            }
        }

        public override void Render()
        {
            if (!HasInputAuthority) return;

            HandleReturnToLobby();

            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry == null) return;

            // Khi host migrate, PlayerRegistry cũ bị despawn và spawn lại rỗng.
            // Nếu slot của mình biến mất thì reset flag để re-register.
            if (_registered && !registry.Slots.TryGet(Object.InputAuthority, out _))
            {
                _registered = false;
                _readyApplied = false;
            }

            if (!_registered)
            {
                if (string.IsNullOrWhiteSpace(_localDisplayName))
                    _localDisplayName = $"Player{Object.InputAuthority.PlayerId}";
                registry.Rpc_RegisterPlayer(new NetworkString<_16>(_localDisplayName));
                _registered = true;
                return; // chờ frame sau mới sync ready
            }

            if (_readyApplied != _localReady)
            {
                registry.Rpc_SetReady(_localReady);
                _readyApplied = _localReady;
                if (HasStateAuthority) IsReady = _localReady;
            }
        }

        // Khi ván chơi kết thúc và quay lại sảnh, mỗi client phải tự reset:
        //  - ý định ready cục bộ (nếu không sẽ không bấm ready lại được vì _readyApplied vẫn == _localReady)
        //  - các store tranh static (DrawingTextureStore/StrokeStore) vì chúng không bao giờ
        //    được clear trên client → phase Observe của ván mới sẽ hiện tranh cũ.
        private void HandleReturnToLobby()
        {
            var sm = ServiceLocator.Get<GameStateMachine>();
            if (sm == null) return; // host migration: bỏ qua, không reset nhầm

            var current = sm.CurrentState;
            if (!_gameStateInit)
            {
                _gameStateInit = true;
                _lastGameState = current;
                return;
            }

            if (current == _lastGameState) return;
            bool returnedToLobby = current == GameStateType.Lobby && _lastGameState != GameStateType.Lobby;
            _lastGameState = current;
            if (!returnedToLobby) return;

            _localReady = false;
            _readyApplied = false;
            DrawingTextureStore.Clear();
            DrawingStrokeStore.ClearStrokes();
        }

        public void SetLocalReady(bool ready)
        {
            if (!HasInputAuthority) return;
            _localReady = ready;
        }

        public void ToggleLocalReady() => SetLocalReady(!_localReady);
    }
}
