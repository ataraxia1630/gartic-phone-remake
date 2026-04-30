using System.Text;
using GarticPhone.Network.Core;
using GarticPhone.Network.GameModes;
using GarticPhone.Network.Players;
using GarticPhone.Network.StateMachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GarticPhone.Network.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LobbyPanel : MonoBehaviour
    {
        private CanvasGroup _group;

        [Header("Display")]
        [SerializeField] private TMP_Text playerListText;
        [SerializeField] private TMP_Text modeText;

        [Header("Controls")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button modeSandwichButton;
        [SerializeField] private Button modeCoopButton;

        private bool _localReady;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (modeSandwichButton != null) modeSandwichButton.onClick.AddListener(() => RequestMode(GameModeType.Sandwich));
            if (modeCoopButton != null) modeCoopButton.onClick.AddListener(() => RequestMode(GameModeType.Coop));
        }

        private void Update()
        {
            var sm = ServiceLocator.Get<GameStateMachine>();
            bool inLobby = sm != null && sm.CurrentState == GameStateType.Lobby;
            SetVisible(inLobby);
            if (!inLobby) return;

            UpdatePlayerList();
            UpdateModeText();
            UpdateButtonsVisibility();
        }

        private void SetVisible(bool visible)
        {
            if (_group == null) return;
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        private void UpdatePlayerList()
        {
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry == null || playerListText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Players ({registry.ConnectedCount()}):");
            foreach (var pair in registry.Slots)
            {
                var slot = pair.Value;
                var status = slot.IsReady ? "[READY]" : "[...]";
                var conn = slot.IsConnected ? "" : " (offline)";
                sb.AppendLine($"  {slot.SlotIndex}. {slot.DisplayName} {status}{conn}");
            }
            playerListText.text = sb.ToString();
        }

        private void UpdateModeText()
        {
            var sm = ServiceLocator.Get<GameStateMachine>();
            if (sm == null || modeText == null) return;
            modeText.text = $"Mode: {sm.SelectedMode}";
        }

        private void UpdateButtonsVisibility()
        {
            var bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            var sm = ServiceLocator.Get<GameStateMachine>();
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (bootstrap == null || bootstrap.Runner == null) return;

            bool isMaster = bootstrap.Runner.IsSharedModeMasterClient;
            bool inLobby = sm != null && sm.CurrentState == GameStateType.Lobby;
            bool allReady = registry != null && registry.AreAllConnectedReady();
            bool enoughPlayers = registry != null && registry.ConnectedCount() >= bootstrap.Config.MinPlayersToStart;

            if (startButton != null) startButton.gameObject.SetActive(inLobby && isMaster);
            if (startButton != null) startButton.interactable = allReady && enoughPlayers;
            if (modeSandwichButton != null) modeSandwichButton.gameObject.SetActive(inLobby && isMaster);
            if (modeCoopButton != null) modeCoopButton.gameObject.SetActive(inLobby && isMaster);
            if (readyButton != null) readyButton.gameObject.SetActive(inLobby);
        }

        private void OnReadyClicked()
        {
            _localReady = !_localReady;
            if (NetworkPlayer.Local != null) NetworkPlayer.Local.SetLocalReady(_localReady);
        }

        private void OnStartClicked()
        {
            var sm = ServiceLocator.Get<GameStateMachine>();
            sm?.Rpc_RequestStart();
        }

        private void RequestMode(GameModeType mode)
        {
            var sm = ServiceLocator.Get<GameStateMachine>();
            sm?.Rpc_RequestSetMode(mode);
        }
    }
}
