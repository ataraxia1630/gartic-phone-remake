using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Hoai.Mock
{
    // Gắn vào bất kỳ GameObject nào trong scene để debug phase flow.
    // Dùng [ContextMenu] bằng cách right-click component trong Inspector → chọn lệnh.
    // Phím (Editor only): F5 = jump to Results,  F6 = advance 1 phase, F7 = inject 1 fake drawing,
    //                     F8 = print status,      F9 = inject fake drawings tất cả slots,
    //                     F10 = force init AlbumStore (dùng khi PlayerCount=0)
    public class PhaseDebugHelper : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            DontDestroyOnLoad(gameObject);
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F5)) ForceJumpToResults();
            if (Input.GetKeyDown(KeyCode.F6)) ForceAdvance();
            if (Input.GetKeyDown(KeyCode.F7)) InjectFakeDrawing();
            if (Input.GetKeyDown(KeyCode.F8)) PrintStatus();
            if (Input.GetKeyDown(KeyCode.F9)) InjectFakeDrawingsAll();
            if (Input.GetKeyDown(KeyCode.F10)) ForceInitAlbumStore();
#endif
        }

        [ContextMenu("1 — Print Status")]
        public void PrintStatus()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();

            if (pm == null)
            {
                Debug.LogWarning("[PhaseDebug] PhaseManager = NULL — game chưa start hoặc chưa connect.");
                return;
            }

            int texCount = 0;
            foreach (var _ in DrawingTextureStore.GetAllRawPngs()) texCount++;

            Debug.Log(
                $"[PhaseDebug] ═══════════════════════\n" +
                $"  Phase:        {pm.CurrentPhase}\n" +
                $"  IsHost:       {pm.HasStateAuthority}\n" +
                $"  RoundIndex:   {pm.RoundIndex} / {pm.TotalRounds}\n" +
                $"  RevealState:  album={pm.RevealAlbumIndex} link={pm.RevealLinkIndex}\n" +
                $"  Players:      {(album != null ? album.PlayerCount.ToString() : "?")}  |  LinksPerChain: {(album != null ? album.LinksPerChain.ToString() : "?")}\n" +
                $"  DrawingStore: {texCount} raw PNGs\n" +
                $"═══════════════════════"
            );
        }

        [ContextMenu("2 — Force Advance Phase (host only)")]
        public void ForceAdvance()
        {
            var pm = ServiceLocator.Get<PhaseManager>();

            if (pm == null)
            {
                Debug.LogWarning("[PhaseDebug] PhaseManager = NULL. Cần: kết nối mạng + game đã StartGame().");
                return;
            }

            if (!pm.HasStateAuthority)
            {
                Debug.LogWarning("[PhaseDebug] Không phải host (HasStateAuthority=false). Chỉ master client mới advance được.");
                return;
            }

            if (pm.CurrentPhase == PhaseType.None)
            {
                Debug.LogWarning("[PhaseDebug] Phase = None — game chưa gọi StartGame(). Cần vào Lobby và nhấn Start trước.");
                return;
            }

            Debug.Log($"[PhaseDebug] Force advance từ [{pm.CurrentPhase}] → round {pm.RoundIndex + 1}/{pm.TotalRounds}");
            pm.AdvancePhase();
        }

        [ContextMenu("3 — Inject Fake Drawing (link=1 slot=0)")]
        public void InjectFakeDrawing()
        {
            byte link = 1, slot = 0;
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var fill = new Color(Random.value, Random.value, Random.value);
            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            tex.SetPixels(pixels);
            tex.Apply();
            var png = tex.EncodeToPNG();
            DrawingTextureStore.StoreTexture(link, slot, png);
            Debug.Log($"[PhaseDebug] Injected fake drawing → chainLink={link}, originSlot={slot}");
        }

        // F5 — nhảy thẳng vào Results/Reveal mà không cần chơi hết phases.
        // Prerequisite: host đã connect và StartGame() đã được gọi (phases đang chạy).
        [ContextMenu("4 — Force Jump to Results (F5)")]
        public void ForceJumpToResults()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            if (pm == null) { Debug.LogWarning("[PhaseDebug] PhaseManager = NULL. Cần start game trước."); return; }
            if (!pm.HasStateAuthority) { Debug.LogWarning("[PhaseDebug] Không phải host."); return; }
            if (pm.CurrentPhase == PhaseType.None) { Debug.LogWarning("[PhaseDebug] Phase = None — StartGame() chưa được gọi."); return; }

            Debug.Log($"[PhaseDebug] Force jump to Results từ [{pm.CurrentPhase}] round {pm.RoundIndex}/{pm.TotalRounds}");
            pm.IsGameFinished = true;
            // PlayingState.Tick() sẽ phát hiện IsGameFinished và chuyển GSM sang Results trong frame tiếp theo.
        }

        // F10 — force init AlbumStore khi StartGame() bị skip hoặc Init chưa được gọi.
        [ContextMenu("6 — Force Init AlbumStore (F10)")]
        public void ForceInitAlbumStore()
        {
            var album = ServiceLocator.Get<AlbumStore>();

            if (album == null)
            {
                // ServiceLocator bị null sau scene change — tìm lại bằng FindObjectOfType
                album = Object.FindAnyObjectByType<AlbumStore>();
                if (album != null)
                {
                    ServiceLocator.Register<AlbumStore>(album);
                    Debug.Log("[PhaseDebug] AlbumStore tìm lại được, đã re-register vào ServiceLocator.");
                }
            }

            if (album == null)
            {
                Debug.LogWarning("[PhaseDebug] AlbumStore không tìm thấy trong scene. PlayerSpawner có thể chưa spawn nó.");
                return;
            }
            if (!album.HasStateAuthority)
            {
                Debug.LogWarning("[PhaseDebug] Không phải host (HasStateAuthority=false). Init phải chạy từ host.");
                return;
            }
            var registry = ServiceLocator.Get<PlayerRegistry>();
            byte playerCount = registry != null ? (byte)registry.ConnectedCount() : (byte)2;
            album.Init(playerCount);
            Debug.Log($"[PhaseDebug] Force Init AlbumStore: PlayerCount={playerCount}, LinksPerChain={album.LinksPerChain}");
        }

        // F9 — inject ảnh giả cho tất cả slots để RevealAlbumPanel có gì mà hiển thị.
        // Gọi trước F5 nếu muốn xem ảnh thay vì "(chưa tải)".
        [ContextMenu("5 — Inject Fake Drawings All Slots (F9)")]
        public void InjectFakeDrawingsAll()
        {
            var album = ServiceLocator.Get<AlbumStore>();
            if (album == null)
            {
                Debug.LogWarning("[PhaseDebug] AlbumStore = NULL trong ServiceLocator — prefab chưa được spawn (kiểm tra PlayerSpawner Inspector).");
                return;
            }
            if (album.PlayerCount == 0 || album.LinksPerChain == 0)
            {
                Debug.LogWarning($"[PhaseDebug] AlbumStore tìm thấy nhưng chưa Init: PlayerCount={album.PlayerCount}, LinksPerChain={album.LinksPerChain}. Gọi ForceInitAlbumStore() hoặc start game đúng cách.");
                return;
            }

            var colors = new[] { Color.cyan, Color.green, Color.yellow, Color.magenta, Color.blue, Color.red };
            int count = 0;
            for (byte link = 1; link < album.LinksPerChain - 1; link++)
            {
                for (byte slot = 0; slot < album.PlayerCount; slot++)
                {
                    var fill = colors[count % colors.Length];
                    var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                    var pixels = new Color[128 * 128];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    DrawingTextureStore.StoreTexture(link, slot, tex.EncodeToPNG());
                    Destroy(tex);
                    count++;
                }
            }
            Debug.Log($"[PhaseDebug] Injected {count} fake drawings cho {album.PlayerCount} players, links 1..{album.LinksPerChain - 2}");
        }
    }
}
