using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace GarticPhone.Network.Core
{
    public class SessionService
    {
        private readonly NetworkRunner _runner;
        private readonly NetworkConfig _config;

        public string Code { get; private set; }
        public bool IsConnecting { get; private set; }
        public NetworkRunner Runner => _runner;

        public event Action<string> OnConnected;
        public event Action<string> OnConnectFailed;

        public SessionService(NetworkRunner runner, NetworkConfig config)
        {
            _runner = runner;
            _config = config;
        }

        public async Task<bool> HostAsync()
        {
            if (_runner == null || _config == null)
            {
                Fail("Runner or config null");
                return false;
            }
            if (IsConnecting)
            {
                Debug.LogWarning("[SessionService] Already connecting");
                return false;
            }

            IsConnecting = true;
            for (int attempt = 0; attempt < _config.RoomCodeRetryAttempts; attempt++)
            {
                var code = RoomCode.Generate(_config.RoomCodeLength);
                var ok = await TryStartAsync(code);
                if (ok) { IsConnecting = false; return true; }

                Debug.Log($"[SessionService] Host attempt {attempt + 1} with code {code} failed, retrying...");
            }

            IsConnecting = false;
            Fail($"Host failed after {_config.RoomCodeRetryAttempts} attempts");
            return false;
        }

        public async Task<bool> JoinAsync(string code)
        {
            if (_runner == null || _config == null)
            {
                Fail("Runner or config null");
                return false;
            }

            var normalized = RoomCode.Normalize(code);
            if (!RoomCode.IsValidShape(normalized, _config.RoomCodeLength))
            {
                Fail($"Invalid room code shape: '{code}'");
                return false;
            }

            IsConnecting = true;
            var ok = await TryStartAsync(normalized);
            IsConnecting = false;
            if (!ok) Fail($"Join failed for '{normalized}'");
            return ok;
        }

        public void Leave()
        {
            if (_runner != null && _runner.IsRunning) _runner.Shutdown();
        }

        private async Task<bool> TryStartAsync(string code)
        {
            var args = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = code,
                PlayerCount = _config.MaxPlayers,
            };

            var result = await _runner.StartGame(args);
            if (result.Ok)
            {
                Code = code;
                OnConnected?.Invoke(code);
                return true;
            }
            return false;
        }

        private void Fail(string reason)
        {
            Debug.LogError($"[SessionService] {reason}");
            OnConnectFailed?.Invoke(reason);
        }
    }
}
