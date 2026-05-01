using InkEcho.Network.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Network.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ConnectionPanel : MonoBehaviour
    {
        private CanvasGroup _group;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField codeInput;

        [Header("Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;

        [Header("Output")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text roomCodeText;

        [Header("Persistence Keys")]
        [SerializeField] private string nicknamePrefKey = "gartic_nickname";

        private const string DefaultStatus = "Enter name to start";

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        }

        private void Start()
        {
            if (nameInput != null) nameInput.text = PlayerPrefs.GetString(nicknamePrefKey, "");
            SetStatus(DefaultStatus);
            SetRoomCode("");
        }

        private void Update()
        {
            var bootstrap = ResolveBootstrap();
            bool connected = bootstrap != null && bootstrap.Runner != null && bootstrap.Runner.IsRunning;
            SetVisible(!connected);
        }

        private void SetVisible(bool visible)
        {
            if (_group == null) return;
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        private async void OnHostClicked()
        {
            if (!ValidateName()) return;
            SaveName();
            SetButtonsInteractable(false);
            SetStatus("Hosting...");

            var bootstrap = ResolveBootstrap();
            if (bootstrap == null) { Restore("Bootstrap not found"); return; }

            var ok = await bootstrap.Host();
            if (ok)
            {
                SetRoomCode(bootstrap.Session.Code);
                SetStatus($"Hosted room {bootstrap.Session.Code}");
            }
            else
            {
                Restore("Host failed");
            }
        }

        private async void OnJoinClicked()
        {
            if (!ValidateName()) return;
            if (codeInput == null || string.IsNullOrWhiteSpace(codeInput.text))
            {
                SetStatus("Enter room code");
                return;
            }
            SaveName();
            SetButtonsInteractable(false);
            SetStatus($"Joining {codeInput.text.ToUpperInvariant()}...");

            var bootstrap = ResolveBootstrap();
            if (bootstrap == null) { Restore("Bootstrap not found"); return; }

            var ok = await bootstrap.Join(codeInput.text);
            if (ok)
            {
                SetRoomCode(bootstrap.Session.Code);
                SetStatus($"Joined room {bootstrap.Session.Code}");
            }
            else
            {
                Restore("Join failed");
            }
        }

        private NetworkBootstrap ResolveBootstrap()
        {
            return ServiceLocator.Get<NetworkBootstrap>() ?? NetworkBootstrap.Instance;
        }

        private bool ValidateName()
        {
            if (nameInput == null || string.IsNullOrWhiteSpace(nameInput.text))
            {
                SetStatus("Enter your name");
                return false;
            }
            return true;
        }

        private void SaveName()
        {
            PlayerPrefs.SetString(nicknamePrefKey, nameInput.text.Trim());
            PlayerPrefs.Save();
        }

        private void SetButtonsInteractable(bool on)
        {
            if (hostButton != null) hostButton.interactable = on;
            if (joinButton != null) joinButton.interactable = on;
        }

        private void Restore(string status)
        {
            SetButtonsInteractable(true);
            SetStatus(status);
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        private void SetRoomCode(string code)
        {
            if (roomCodeText != null) roomCodeText.text = string.IsNullOrEmpty(code) ? "" : $"Code: {code}";
        }
    }
}
