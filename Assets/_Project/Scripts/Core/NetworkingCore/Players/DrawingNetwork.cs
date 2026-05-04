using System.Collections.Generic;
using Fusion;
using UnityEngine;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;

namespace InkEcho.Network.Players
{
    public class DrawingNetwork : NetworkBehaviour
    {
        [Header("Line")]
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private float minDistance = 0.05f;
        [Header("Drawing Area")]
        [SerializeField] private Collider2D drawingArea;

        // map player id -> current line renderer
        private readonly Dictionary<int, LineRenderer> _currentLines = new Dictionary<int, LineRenderer>();
        private readonly Dictionary<int, List<Vector3>> _points = new Dictionary<int, List<Vector3>>();
        private readonly List<Vector3> _localPoints = new List<Vector3>();
        private int _localStrokeCount;
        private Vector3 _lastSentPoint;
        private bool _hasLastSentPoint;

        public override void Spawned()
        {
            if (drawingArea == null)
            {
                drawingArea = GetComponentInChildren<Collider2D>(true);
            }

            if (drawingArea == null)
            {
                Debug.LogWarning("[DrawingNetwork] drawingArea is not assigned and no Collider2D was found in children");
            }
        }

        // Called from local input Update to start a stroke
        public void StartLocalStroke(Vector3 worldPos)
        {
            _localPoints.Clear();
            _localPoints.Add(worldPos);
            _lastSentPoint = worldPos;
            _hasLastSentPoint = true;
            Rpc_StartStroke();
            Rpc_AddPoint(worldPos.x, worldPos.y);
        }

        public void AddLocalPoint(Vector3 worldPos)
        {
            if (_hasLastSentPoint && Vector3.Distance(_lastSentPoint, worldPos) <= minDistance)
            {
                return;
            }

            _localPoints.Add(worldPos);
            _lastSentPoint = worldPos;
            _hasLastSentPoint = true;
            Rpc_AddPoint(worldPos.x, worldPos.y);
        }

        public void EndLocalStroke()
        {
            Rpc_EndStroke();

            _localStrokeCount++;

            ulong hash = 1469598103934665603UL;
            unchecked
            {
                foreach (var point in _localPoints)
                {
                    var fx = (int)(point.x * 1000f);
                    var fy = (int)(point.y * 1000f);
                    hash ^= (ulong)(fx & 0xFFFFFFFF);
                    hash *= 1099511628211UL;
                    hash ^= (ulong)(fy & 0xFFFFFFFF);
                    hash *= 1099511628211UL;
                }

                hash ^= (ulong)_localStrokeCount;
                hash *= 1099511628211UL;
            }

            var albumStore = ServiceLocator.Get<AlbumStore>();
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            if (albumStore != null && phaseManager != null && phaseManager.TryGetAssignment(Runner.LocalPlayer, out var assignment))
            {
                albumStore.Rpc_SubmitDrawing(assignment.AlbumOriginSlotIndex, hash, (ushort)_localStrokeCount);
            }

            _localPoints.Clear();
        }

        private void Update()
        {
            // Input reading is local - not tied to network authority
            // Each client reads their own mouse input
            if (Input.GetMouseButtonDown(0))
            {
                var mp = Input.mousePosition; mp.z = 10f;
                var world = Camera.main.ScreenToWorldPoint(mp); world.z = 0f;
                if (drawingArea != null && !drawingArea.OverlapPoint(world)) return;
                StartLocalStroke(world);
            }

            if (Input.GetMouseButton(0))
            {
                var mp = Input.mousePosition; mp.z = 10f;
                var world = Camera.main.ScreenToWorldPoint(mp); world.z = 0f;
                if (drawingArea != null && !drawingArea.OverlapPoint(world)) return;
                AddLocalPoint(world);
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndLocalStroke();
                _hasLastSentPoint = false;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_StartStroke(RpcInfo info = default)
        {
            var playerId = info.Source.PlayerId;
            if (_currentLines.ContainsKey(playerId)) return;

            var lineObj = Instantiate(linePrefab);
            var lr = lineObj.GetComponent<LineRenderer>();
            if (lr == null) lr = lineObj.AddComponent<LineRenderer>();

            lr.useWorldSpace = true;
            lr.positionCount = 0;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.sortingOrder = 100;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            // color per player
            var color = PlayerColorFor(playerId);
            lr.startColor = color;
            lr.endColor = color;

            _currentLines[playerId] = lr;
            _points[playerId] = new List<Vector3>();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_AddPoint(float x, float y, RpcInfo info = default)
        {
            var playerId = info.Source.PlayerId;
            if (!_currentLines.TryGetValue(playerId, out var lr)) return;

            var pt = new Vector3(x, y, 0f);
            var pts = _points[playerId];
            if (pts.Count == 0 || Vector3.Distance(pts[pts.Count - 1], pt) > minDistance)
            {
                pts.Add(pt);
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_EndStroke(RpcInfo info = default)
        {
            var playerId = info.Source.PlayerId;
            if (!_currentLines.ContainsKey(playerId)) return;
            _currentLines.Remove(playerId);
            _points.Remove(playerId);
        }

        private Color PlayerColorFor(int playerId)
        {
            var palette = new[]
            {
                Color.black,
                Color.blue,
                Color.red,
                Color.green,
                Color.magenta,
                Color.cyan,
                Color.yellow,
                new Color(0.8f, 0.4f, 0.1f)
            };
            return palette[playerId % palette.Length];
        }

        private GameObject CreateFallbackLinePrefab()
        {
            var lineObj = new GameObject("RuntimeLine");
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 0;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.sortingOrder = 100;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            return lineObj;
        }

    }
}
