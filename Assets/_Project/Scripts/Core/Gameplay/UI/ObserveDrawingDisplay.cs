using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Gameplay.UI
{
    public class ObserveDrawingDisplay : MonoBehaviour
    {
        [SerializeField] private RawImage displayTarget;

        private bool _rendered;

        private void OnEnable()
        {
            _rendered = false;
            if (displayTarget == null)
                displayTarget = GetComponentInChildren<RawImage>(true);
        }

        private void Update()
        {
            if (!_rendered)
                TryRender();
        }

        private void OnDisable()
        {
            _rendered = false;
            if (displayTarget != null) displayTarget.texture = null;
        }

        private void TryRender()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null) return;

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            int prevLink = assignment.ChainLinkIndex - 1;
            if (prevLink < 0) { _rendered = true; return; }

            var tex = DrawingTextureStore.GetTexture(prevLink, assignment.AlbumOriginSlotIndex);
            if (tex == null) return; // poll next frame until texture arrives

            displayTarget.texture = tex;
            displayTarget.color = Color.white;
            _rendered = true;
        }
    }
}