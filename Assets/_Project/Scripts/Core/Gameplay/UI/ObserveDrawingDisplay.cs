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

        private Texture2D _shownTexture;

        private void OnEnable()
        {
            _shownTexture = null;
            if (displayTarget == null)
                displayTarget = GetComponentInChildren<RawImage>(true);
        }

        private void Update()
        {
            TryRender();
        }

        private void OnDisable()
        {
            _shownTexture = null;
            if (displayTarget != null) displayTarget.texture = null;
        }

        private void TryRender()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null) return;

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            int prevLink = assignment.ChainLinkIndex - 1;
            if (prevLink < 0) return;

            var tex = DrawingTextureStore.GetTexture(prevLink, assignment.AlbumOriginSlotIndex);
            if (tex == null) return; // poll next frame until texture arrives
            if (tex == _shownTexture) return;

            _shownTexture = tex;
            displayTarget.texture = tex;
            displayTarget.color = Color.white;
        }
    }
}