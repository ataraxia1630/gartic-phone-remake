using UnityEngine;
using TMPro;
using Fusion;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;

public class PhaseTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _phaseNameText;

    private bool _hasLoggedNull = false; // Tránh spam log

    void Update()
    {
        var phaseManager = ServiceLocator.Get<PhaseManager>();

        // Cửa số 1: Check xem PhaseManager đã đẻ ra chưa
        if (phaseManager == null)
        {
            if (!_hasLoggedNull)
            {
                Debug.LogWarning("[UI] Đang chờ PhaseManager load...");
                _hasLoggedNull = true;
            }
            return;
        }
        _hasLoggedNull = false;

        // Cửa số 2: Check xem game đã ra khỏi Lobby chưa
        if (phaseManager.CurrentPhase == PhaseType.None)
        {
            if (_phaseNameText != null) _phaseNameText.text = "Waiting to Start...";
            return;
        }

        // Cửa số 3: Chạy bình thường
        if (_phaseNameText != null)
            _phaseNameText.text = "Phase: " + phaseManager.CurrentPhase.ToString();

        if (phaseManager.PhaseTimer.IsRunning)
        {
            float timeLeft = phaseManager.PhaseTimer.RemainingTime(phaseManager.Runner).GetValueOrDefault();
            // Max(0, ...) để tránh số âm khi vừa hết giờ
            int minutes = Mathf.FloorToInt(Mathf.Max(0, timeLeft) / 60f);
            int seconds = Mathf.FloorToInt(Mathf.Max(0, timeLeft) % 60f);
            _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            _timerText.text = "00:00";
        }
    }
}