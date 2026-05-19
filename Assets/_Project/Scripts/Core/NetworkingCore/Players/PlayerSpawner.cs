using System.Collections;
using Fusion;
using InkEcho.Hoai.Drawing;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.StateMachine;
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
        [SerializeField] private NetworkObject drawingChannelPrefab;

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
            _bootstrap.OnPlayerLeftEvent += HandlePlayerLeft;
            _bootstrap.OnShutdownEvent += HandleShutdown;
        }

        private void OnDestroy()
        {
            if (_bootstrap == null) return;
            _bootstrap.OnPlayerJoinedEvent -= HandlePlayerJoined;
            _bootstrap.OnPlayerLeftEvent -= HandlePlayerLeft;
            _bootstrap.OnShutdownEvent -= HandleShutdown;
        }

        private void HandleShutdown(ShutdownReason _)
        {
            _localPlayerSpawned = false;
            _singletonsSpawned = false;
        }

        private void HandlePlayerLeft(PlayerRef _)
        {
            StartCoroutine(TryRespawnSingletonsAfterDelay());
        }

        private IEnumerator TryRespawnSingletonsAfterDelay()
        {
            yield return null;
            yield return null;
            var runner = _bootstrap?.Runner;
            if (runner == null || !runner.IsRunning || !runner.IsSharedModeMasterClient) yield break;

            _singletonsSpawned = true;
            if (ServiceLocator.Get<PlayerRegistry>() == null)
                EnsureSingleton(runner, playerRegistryPrefab, "PlayerRegistry");
            if (ServiceLocator.Get<GameStateMachine>() == null)
                EnsureSingleton(runner, gameStateMachinePrefab, "GameStateMachine");
            if (ServiceLocator.Get<PhaseManager>() == null)
                EnsureSingleton(runner, phaseManagerPrefab, "PhaseManager");
            if (ServiceLocator.Get<AlbumStore>() == null)
                EnsureSingleton(runner, albumStorePrefab, "AlbumStore");
            if (ServiceLocator.Get<DrawingChannel>() == null)
                EnsureSingleton(runner, drawingChannelPrefab, "DrawingChannel");
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
                    var localPlayer = runner.Spawn(networkPlayerPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
                    if (localPlayer != null)
                    {
                        runner.MakeDontDestroyOnLoad(localPlayer.gameObject);
                    }
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
                EnsureSingleton(runner, drawingChannelPrefab, "DrawingChannel");
            }
        }

        private void EnsureSingleton(NetworkRunner runner, NetworkObject prefab, string label)
        {
            if (prefab == null)
            {
                Debug.LogError($"[PlayerSpawner] {label} prefab not assigned");
                return;
            }
            var instance = runner.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            if (instance != null)
            {
                runner.MakeDontDestroyOnLoad(instance.gameObject);
            }
            Debug.Log($"[PlayerSpawner] Spawned {label} (master)");
        }
    }
}
