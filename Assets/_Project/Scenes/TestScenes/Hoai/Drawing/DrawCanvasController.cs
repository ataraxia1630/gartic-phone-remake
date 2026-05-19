using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using UnityEngine;

namespace InkEcho.Hoai.Drawing
{
    // Gắn cùng GameObject với DrawingManager trong UI_Draw scene.
    // Reset canvas mỗi khi enter Draw phase mới để player có canvas trắng.
    [RequireComponent(typeof(DrawingManager))]
    public class DrawCanvasController : MonoBehaviour
    {
        private DrawingManager _drawing;
        private PhaseType _lastPhase = PhaseType.None;
        private byte _lastDrawRound = byte.MaxValue;

        private void Awake()
        {
            _drawing = GetComponent<DrawingManager>();
        }

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            if (pm == null) return;

            // Reset khi entering Draw phase (round mới).
            if (pm.CurrentPhase == PhaseType.Draw && (_lastPhase != PhaseType.Draw || _lastDrawRound != pm.RoundIndex))
            {
                if (_drawing != null && _drawing.HasTexture)
                {
                    _drawing.ResetCanvas();
                }
                _lastDrawRound = pm.RoundIndex;
            }

            _lastPhase = pm.CurrentPhase;
        }
    }
}
