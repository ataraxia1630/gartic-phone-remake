using Fusion;
using InkEcho.Network.Core;
using UnityEngine;

namespace InkEcho.Network.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Per-Player")]
        [SerializeField] private NetworkObject networkPlayerPrefab;

        [Header("Master-Spawned Singletons")]
        [SerializeField] private NetworkObject playerRegistryPrefab;
        [SerializeField] private NetworkObject gameStateMachinePrefab;
        [SerializeField] private NetworkObject phaseManagerPrefab;
        [SerializeField] private NetworkObject albumStorePrefab;

        private NetworkBootstrap _bootstrap;
        private bool _localPlayerSpawned;
        private bool _singletonsSpawned;

        private void Awake()
        {
            _bootstrap = GetComponent<NetworkBootstrap>();
            if (_bootstrap == null)
            {
                Debug.LogError("[PlayerSpawner] Must be on the same GameObject as NetworkBootstrap");
                return;
            }
            _bootstrap.OnPlayerJoinedEvent += HandlePlayerJoined;
            _bootstrap.OnShutdownEvent += HandleShutdown;
        }

        private void OnDestroy()
        {
            if (_bootstrap == null) return;
            _bootstrap.OnPlayerJoinedEvent -= HandlePlayerJoined;
            _bootstrap.OnShutdownEvent -= HandleShutdown;
        }

        private void HandleShutdown(ShutdownReason _)
        {
            _localPlayerSpawned = false;
            _singletonsSpawned = false;
        }

        private void HandlePlayerJoined(PlayerRef player)
        {
            var runner = _bootstrap != null ? _bootstrap.Runner : null;
            if (runner == null) return;
            if (player != runner.LocalPlayer) return;

            if (!_localPlayerSpawned)
            {
                if (networkPlayerPrefab != null)
                {
                    runner.Spawn(networkPlayerPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
                    Debug.Log("[PlayerSpawner] Spawned local NetworkPlayer");
                    _localPlayerSpawned = true;
                }
                else
                {
                    Debug.LogError("[PlayerSpawner] networkPlayerPrefab not assigned");
                }
            }

            if (runner.IsSharedModeMasterClient && !_singletonsSpawned)
            {
                _singletonsSpawned = true;
                EnsureSingleton(runner, playerRegistryPrefab, "PlayerRegistry");
                EnsureSingleton(runner, gameStateMachinePrefab, "GameStateMachine");
                EnsureSingleton(runner, phaseManagerPrefab, "PhaseManager");
                EnsureSingleton(runner, albumStorePrefab, "AlbumStore");
            }
        }

        private void EnsureSingleton(NetworkRunner runner, NetworkObject prefab, string label)
        {
            if (prefab == null)
            {
                Debug.LogError($"[PlayerSpawner] {label} prefab not assigned");
                return;
            }
            runner.Spawn(prefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
            Debug.Log($"[PlayerSpawner] Spawned {label} (master)");
        }
    }
}
