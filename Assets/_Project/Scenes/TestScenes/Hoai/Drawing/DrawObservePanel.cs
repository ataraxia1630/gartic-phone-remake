using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Hoai.Drawing
{
    // Trong Observe phase: local player được phân chain (slot, link). Tranh cần xem là tranh
    // ở link TRƯỚC ĐÓ (link - 1) trong cùng chain — do người chơi liền trước vừa vẽ.
    // Panel này load Texture2D từ DrawingChannel local cache rồi gán vào RawImage.
    public class DrawObservePanel : MonoBehaviour
    {
        [SerializeField] private RawImage targetImage;
        [SerializeField] private TextMeshProUGUI authorLabel;
        [SerializeField] private TextMeshProUGUI countdownLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private string authorFormat = "Tranh của: {0}";

        private byte _lastShownLink = byte.MaxValue;
        private byte _lastShownSlot = byte.MaxValue;
        private DrawingChannel _channelSub;

        private void OnEnable()
        {
            _lastShownLink = byte.MaxValue;
            _lastShownSlot = byte.MaxValue;
            ClearImage();
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_channelSub != null) _channelSub.OnDrawingReady -= OnDrawingReady;
            _channelSub = null;
        }

        private void Update()
        {
            TrySubscribe();
            Refresh();
        }

        private void TrySubscribe()
        {
            if (_channelSub != null) return;
            var ch = ServiceLocator.Get<DrawingChannel>();
            if (ch == null) return;
            _channelSub = ch;
            _channelSub.OnDrawingReady += OnDrawingReady;
        }

        private void OnDrawingReady(byte link, byte slot, PlayerRef author)
        {
            // Force refresh nếu đúng tranh đang cần xem (case worker nhận trễ).
            if (link == _lastShownLink && slot == _lastShownSlot) return;
            _lastShownLink = byte.MaxValue;
        }

        private void Refresh()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var channel = ServiceLocator.Get<DrawingChannel>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || channel == null || runner == null) return;

            if (countdownLabel != null)
            {
                var remaining = pm.PhaseTimer.RemainingTime(pm.Runner);
                countdownLabel.text = remaining.HasValue ? Mathf.CeilToInt(remaining.Value).ToString() : "—";
            }

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
            {
                ClearImage();
                if (statusLabel != null) statusLabel.text = "Không có chain để quan sát.";
                return;
            }

            // Tranh cần xem = link liền trước.
            if (assignment.ChainLinkIndex == 0)
            {
                ClearImage();
                if (statusLabel != null) statusLabel.text = "(Link đầu, không có tranh trước).";
                return;
            }

            byte targetLink = (byte)(assignment.ChainLinkIndex - 1);
            byte targetSlot = assignment.AlbumOriginSlotIndex;
            if (targetLink == _lastShownLink && targetSlot == _lastShownSlot && targetImage != null && targetImage.texture != null)
            {
                return;
            }

            if (channel.TryGetDrawing(targetLink, targetSlot, out var texture, out var author))
            {
                if (targetImage != null)
                {
                    targetImage.texture = texture;
                    targetImage.enabled = true;
                }
                if (authorLabel != null)
                {
                    var name = ResolvePlayerName(author);
                    authorLabel.text = string.Format(authorFormat, name);
                }
                if (statusLabel != null) statusLabel.text = string.Empty;
                _lastShownLink = targetLink;
                _lastShownSlot = targetSlot;
            }
            else
            {
                ClearImage();
                if (statusLabel != null) statusLabel.text = "Đang tải tranh…";
            }
        }

        private void ClearImage()
        {
            if (targetImage != null) { targetImage.texture = null; targetImage.enabled = false; }
            if (authorLabel != null) authorLabel.text = string.Empty;
        }

        private static string ResolvePlayerName(PlayerRef player)
        {
            if (!player.IsRealPlayer) return "?";
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry != null && registry.TryGetSlot(player, out var slot))
            {
                return slot.DisplayName.Value;
            }
            return $"Player{player.PlayerId}";
        }
    }
}
