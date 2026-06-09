using Fusion;
using InkEcho.Network.Core;
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

            if (!_registered)
            {
                var registry = ServiceLocator.Get<PlayerRegistry>();
                if (registry != null)
                {
                    if (string.IsNullOrWhiteSpace(_localDisplayName))
                        _localDisplayName = $"Player{Object.InputAuthority.PlayerId}";
                    registry.Rpc_RegisterPlayer(new NetworkString<_16>(_localDisplayName));
                    _registered = true;
                }
            }

            if (_registered && _readyApplied != _localReady)
            {
                var registry = ServiceLocator.Get<PlayerRegistry>();
                if (registry != null)
                {
                    registry.Rpc_SetReady(_localReady);
                    _readyApplied = _localReady;
                    if (HasStateAuthority) IsReady = _localReady;
                }
            }
        }

        public void SetLocalReady(bool ready)
        {
            if (!HasInputAuthority) return;
            _localReady = ready;
        }

        public void ToggleLocalReady() => SetLocalReady(!_localReady);
    }
}
