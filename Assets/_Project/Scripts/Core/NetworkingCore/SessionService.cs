using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace InkEcho.Network.Core
{
    public class SessionService
    {
        private readonly NetworkRunner _runner;
        private readonly NetworkConfig _config;
        private List<SessionInfo> _availableSessions = new List<SessionInfo>();

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
                var ok = await TryStartAsync(code, isHost: true);  // Host can create new session
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

        /// <summary>
        /// Updates the list of available sessions from Fusion
        /// </summary>
        public void UpdateAvailableSessions(List<SessionInfo> sessions)
        {
            _availableSessions = sessions ?? new List<SessionInfo>();
            Debug.Log($"[SessionService] Updated available rooms: {_availableSessions.Count} rooms");
        }

        /// <summary>
        /// Validates that a room exists and is joinable
        /// </summary>
        private string ValidateRoom(string roomCode)
        {
            // If no sessions available yet, allow the join attempt (Fusion will handle it)
            if (_availableSessions.Count == 0)
            {
                Debug.Log($"[SessionService] No session list available yet, allowing join attempt");
                return null;
            }

            // Find the room with matching code
            var room = _availableSessions.FirstOrDefault(s => s.Name == roomCode);
            if (room == null)
            {
                return $"Room '{roomCode}' does not exist";
            }

            // Check if room is open for joining
            if (!room.IsOpen)
            {
                return $"Room '{roomCode}' is not accepting new players";
            }

            // Check if room is full
            if (room.PlayerCount >= room.MaxPlayers)
            {
                return $"Room '{roomCode}' is full ({room.PlayerCount}/{room.MaxPlayers})";
            }

            Debug.Log($"[SessionService] Room '{roomCode}' validated successfully ({room.PlayerCount}/{room.MaxPlayers} players)");
            return null;
        }

        private async Task<bool> TryStartAsync(string code, bool isHost = false)
        {
            var args = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = code,
                PlayerCount = _config.MaxPlayers,
                EnableClientSessionCreation = isHost,  // Only host can create new sessions
            };

            var result = await _runner.StartGame(args);
            if (result.Ok)
            {
                Code = code;
                OnConnected?.Invoke(code);
                return true;
            }
            
            // If join failed, check if it's because the room doesn't exist
            var validationError = ValidateRoom(code);
            if (!string.IsNullOrEmpty(validationError))
            {
                Fail(validationError);
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
