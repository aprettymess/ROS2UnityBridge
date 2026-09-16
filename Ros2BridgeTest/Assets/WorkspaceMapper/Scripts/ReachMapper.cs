using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorkspaceMapper.Scripts
{
    public class ReachMapper : MonoBehaviour
    {
        public enum VizMode { TablePoints, Heatmap, Volume }
        enum SweepState { Idle, Running, Paused }

        [Header("References")]
        [Required, SerializeField] Transform robotRoot;
        [SerializeField] WorkspaceRobotBinding binding;
        [SerializeField] WorkspaceTable table;

        [Header("Tool")]
        [SerializeField] float toolLengthMm = 170f;

        [Header("Base-frame alignment (verify with Validate)")]
        [SerializeField] Vector3 baseEulerOffset = Vector3.zero;
        [SerializeField] Vector3 basePosOffsetMeters = Vector3.zero;

        [Header("Sweep steps (deg) - larger = faster")]
        [SerializeField] float stepJ1 = 12f;
        [SerializeField] float stepJ2 = 12f;
        [SerializeField] float stepJ3 = 12f;
        [SerializeField] float stepJ4 = 45f;
        [SerializeField] float stepJ5 = 45f;

        [Header("Driven sweep (Play mode)")]
        [InfoBox("Drives the TWIN joints (not the real arm - no wear). Points draw live; table hits are painted red. Use coarse steps.")]
        [SerializeField] int liveUpdateEvery = 25;
        [SerializeField] int sweepWaitFrames = 2;
        [SerializeField] float drivenMinStep = 18f;

        [Header("Filters")]
        [SerializeField] float tableBandMm = 60f;
        [SerializeField] bool stopAtTable = true;
        [SerializeField] bool gripperDownOnly = true;
        [SerializeField, Range(0f, 60f)] float downConeDeg = 30f;

        [Header("Visualization")]
        [SerializeField] VizMode mode = VizMode.Heatmap;
        [SerializeField] Color pointColor = new Color(0.15f, 0.8f, 0.35f);
        [SerializeField] Color tableHitColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color boundaryColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] Color volumeColor = new Color(0.2f, 0.8f, 0.4f, 0.35f);
        [SerializeField] bool showBoundary = true;
        [SerializeField] bool volumeAsBlob = true;
        [SerializeField] float voxelSizeM = 0.03f;
        [SerializeField, Range(16, 256)] int heatmapRes = 128;
        [SerializeField] int maxRenderedPoints = 300000;

        readonly List<Vector3> _tablePts = new();
        readonly List<Vector3> _volumePts = new();
        readonly List<Vector3> _hitPts = new();
        SweepState _sweep = SweepState.Idle;
        Material _ptMat, _hitMat, _heatMat, _lineMat, _volMat;
        Texture2D _heatTex;

        void OnEnable() { _sweep = SweepState.Idle; }
        public bool Sweeping => _sweep != SweepState.Idle;
        public IReadOnlyList<Vector3> TablePointsWorld => _tablePts;
        public int SweepWaitFrames { get => sweepWaitFrames; set => sweepWaitFrames = Mathf.Clamp(value, 1, 6); }
        public void RestartSweep() { _sweep = SweepState.Idle; StopAllCoroutines(); StartDrivenSweep(); }
        public WorkspaceTable Table => table;

        // ---------- analytic (instant, no motion) ----------
        [Button(ButtonSizes.Large), PropertyOrder(-2)]
        public void Compute()
        {
            _tablePts.Clear(); _volumePts.Clear(); _hitPts.Clear();
            Vector2[] lim = MyCobot320Fk.Limits;
            float[] a = new float[6];
            float downDot = -Mathf.Cos(downConeDeg * Mathf.Deg2Rad);
            float bandM = tableBandMm * 0.001f;
            foreach (float j1 in Range(lim[0], stepJ1))
            foreach (float j2 in Range(lim[1], stepJ2))
            foreach (float j3 in Range(lim[2], stepJ3))
            foreach (float j4 in Range(lim[3], stepJ4))
            foreach (float j5 in Range(lim[4], stepJ5))
            {
                a[0] = j1; a[1] = j2; a[2] = j3; a[3] = j4; a[4] = j5; a[5] = 0f;
                Matrix4x4 t = MyCobot320Fk.TcpBaseMatrix(a, toolLengthMm);
                Vector3 pMm = MyCobot320Fk.PosMm(t);
                Vector3 world = BaseMmToWorld(pMm);
                _volumePts.Add(world);
                bool down = !gripperDownOnly || MyCobot320Fk.ToolZ(t).z <= downDot;
                if (Mathf.Abs(pMm.z) <= tableBandMm && down) _tablePts.Add(world);
            }
            Debug.Log($"Analytic: volume {_volumePts.Count:N0}, table {_tablePts.Count:N0}");
            Render();
        }

        // ---------- driven, live, with controls ----------
        [Button("Start Driven Sweep (live)"), PropertyOrder(-1)]
        public void StartDrivenSweep()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Play mode only. Disable the live ROS drive first."); return; }
            if (!binding) { Debug.LogWarning("Assign binding."); return; }
            if (_sweep != SweepState.Idle) { Debug.Log("Sweep already running."); return; }
            StopAllCoroutines();
            _tablePts.Clear(); _volumePts.Clear(); _hitPts.Clear();
            StartCoroutine(DrivenSweep());
        }

        [Button("Pause"), HorizontalGroup("sweepctl"), PropertyOrder(-1)]
        public void PauseSweep() { if (_sweep == SweepState.Running) _sweep = SweepState.Paused; }
        [Button("Resume"), HorizontalGroup("sweepctl"), PropertyOrder(-1)]
        public void ResumeSweep() { if (_sweep == SweepState.Paused) _sweep = SweepState.Running; }
        [Button("Stop"), HorizontalGroup("sweepctl"), PropertyOrder(-1)]
        public void StopSweep() { _sweep = SweepState.Idle; StopAllCoroutines(); }

        IEnumerator DrivenSweep()
        {
            _sweep = SweepState.Running;
            Vector2[] lim = MyCobot320Fk.Limits;
            float[] a = new float[6];
            float s1 = Mathf.Max(stepJ1, drivenMinStep), s2 = Mathf.Max(stepJ2, drivenMinStep), s3 = Mathf.Max(stepJ3, drivenMinStep);
            float s4 = Mathf.Max(stepJ4, drivenMinStep * 2f), s5 = Mathf.Max(stepJ5, drivenMinStep * 2f);
            float bandM = tableBandMm * 0.001f;
            int i = 0;
            var wait = new WaitForFixedUpdate();
            foreach (float j1 in Range(lim[0], s1))
            foreach (float j2 in Range(lim[1], s2))
            foreach (float j3 in Range(lim[2], s3))
            foreach (float j4 in Range(lim[3], s4))
            foreach (float j5 in Range(lim[4], s5))
            {
                while (_sweep == SweepState.Paused) yield return null;
                if (_sweep == SweepState.Idle) { FinishSweep(); yield break; }
                a[0] = j1; a[1] = j2; a[2] = j3; a[3] = j4; a[4] = j5; a[5] = 0f;
                if (stopAtTable && MyCobot320Fk.PosMm(MyCobot320Fk.TcpBaseMatrix(a, toolLengthMm)).z < 0f) continue;
                binding.DriveRealAngles(a);
                for (int wf = 0; wf < Mathf.Max(1, sweepWaitFrames); wf++) yield return wait;
                Vector3 world = binding.GetTcpWorld();
                _volumePts.Add(world);
                if (table)
                {
                    Vector3 tl = table.transform.InverseTransformPoint(world);
                    if (Mathf.Abs(tl.y) <= bandM) { _tablePts.Add(world); _hitPts.Add(world); }
                }
                if (++i % liveUpdateEvery == 0) RenderLive();
            }
            FinishSweep();
        }

        void FinishSweep()
        {
            _sweep = SweepState.Idle;
            RenderLive();
            Debug.Log($"Driven sweep done: volume {_volumePts.Count:N0}, table hits {_hitPts.Count:N0}");
        }

        void RenderLive()
        {
            RenderPointsInto("ReachPoints", _volumePts, ref _ptMat, pointColor);
            RenderPointsInto("TableHits", _hitPts, ref _hitMat, tableHitColor);
            SetActiveChild("ReachPoints", true);
            SetActiveChild("TableHits", true);
            SetActiveChild("ReachHeatmap", false);
            SetActiveChild("ReachVolume", false);
        }

        // ---------- helpers ----------
        IEnumerable<float> Range(Vector2 lim, float step)
        {
            step = Mathf.Max(step, 0.5f);
            for (float v = lim.x; v < lim.y; v += step) yield return v;
            yield return lim.y;
        }

        Vector3 BaseMmToWorld(Vector3 pMm)
        {
            Vector3 pM = pMm * 0.001f;
            Vector3 flu = new Vector3(-pM.y, pM.z, pM.x);
            Vector3 local = Quaternion.Euler(baseEulerOffset) * flu + basePosOffsetMeters;
            return robotRoot ? robotRoot.TransformPoint(local) : local;
        }

        public void SetMode(VizMode m) { mode = m; Render(); }
        public void CycleMode() { mode = (VizMode)(((int)mode + 1) % 3); Render(); }

        void Render()
        {
            bool heat = mode == VizMode.Heatmap;
            bool blob = mode == VizMode.Volume && volumeAsBlob;
            SetActiveChild("ReachHeatmap", heat);
            SetActiveChild("ReachVolume", blob);
            SetActiveChild("ReachPoints", !heat && !blob);
            SetActiveChild("TableHits", _hitPts.Count > 0 && !heat);
            if (heat) RenderHeatmap();
            else if (blob) RenderVolumeBlob();
            else RenderPointsInto("ReachPoints", mode == VizMode.Volume ? _volumePts : _tablePts, ref _ptMat, pointColor);
            if (_hitPts.Count > 0) RenderPointsInto("TableHits", _hitPts, ref _hitMat, tableHitColor);
            RenderBoundary();
        }

        void RenderPointsInto(string child, List<Vector3> pts, ref Material mat, Color color)
        {
            Transform t = Child(child);
            t.position = Vector3.zero; t.rotation = Quaternion.identity;
            MeshFilter mf = Ensure<MeshFilter>(t);
            MeshRenderer mr = Ensure<MeshRenderer>(t);
            int cap = Mathf.Min(pts.Count, maxRenderedPoints);
            int stride = pts.Count > 0 ? Mathf.Max(1, pts.Count / Mathf.Max(1, cap)) : 1;
            var verts = new List<Vector3>(cap);
            for (int i = 0; i < pts.Count && verts.Count < cap; i += stride) verts.Add(pts[i]);
            int[] idx = new int[verts.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            var mesh = new Mesh { name = child, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetIndices(idx, MeshTopology.Points, 0);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            if (mat == null) mat = MaterialUtil.MakeUnlit();
            MaterialUtil.SetColor(mat, color);
            mr.sharedMaterial = mat;
        }

        void RenderVolumeBlob()
        {
            Transform t = Child("ReachVolume");
            t.position = Vector3.zero; t.rotation = Quaternion.identity;
            MeshFilter mf = Ensure<MeshFilter>(t);
            MeshRenderer mr = Ensure<MeshRenderer>(t);
            float vs = Mathf.Max(voxelSizeM, 0.005f);
            var occ = new HashSet<Vector3Int>();
            foreach (var w in _volumePts)
            {
                occ.Add(new Vector3Int(Mathf.FloorToInt(w.x / vs), Mathf.FloorToInt(w.y / vs), Mathf.FloorToInt(w.z / vs)));
            }
            Vector3Int[] nb = { new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1) };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int cap = 60000, added = 0;
            foreach (var v in occ)
            {
                bool surface = false;
                for (int n = 0; n < 6; n++) if (!occ.Contains(v + nb[n])) { surface = true; break; }
                if (!surface) continue;
                if (added++ > cap) break;
                AddCube(verts, tris, new Vector3((v.x + 0.5f) * vs, (v.y + 0.5f) * vs, (v.z + 0.5f) * vs), vs * 0.5f);
            }
            var mesh = new Mesh { name = "ReachVolume", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            if (_volMat == null) _volMat = MaterialUtil.MakeUnlit();
            MaterialUtil.SetColor(_volMat, volumeColor);
            MaterialUtil.SetTransparent(_volMat);
            mr.sharedMaterial = _volMat;
        }

        static void AddCube(List<Vector3> v, List<int> tri, Vector3 c, float h)
        {
            int b = v.Count;
            v.Add(c + new Vector3(-h, -h, -h)); v.Add(c + new Vector3(h, -h, -h)); v.Add(c + new Vector3(h, h, -h)); v.Add(c + new Vector3(-h, h, -h));
            v.Add(c + new Vector3(-h, -h, h)); v.Add(c + new Vector3(h, -h, h)); v.Add(c + new Vector3(h, h, h)); v.Add(c + new Vector3(-h, h, h));
            int[] f = { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2, 0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5 };
            foreach (var t in f)
                tri.Add(b + t);
        }

        void RenderHeatmap()
        {
            if (!table) { Debug.LogWarning("Assign a WorkspaceTable for the heatmap."); return; }
            float L = table.LengthMeters, W = table.WidthMeters;
            int rx = heatmapRes;
            int rz = Mathf.Max(8, Mathf.RoundToInt(heatmapRes * W / Mathf.Max(0.001f, L)));
            int[] count = new int[rx * rz];
            int maxC = 1;
            for (int p = 0; p < _tablePts.Count; p++)
            {
                Vector3 lp = table.transform.InverseTransformPoint(_tablePts[p]);
                float u = (lp.x + L * 0.5f) / L, v = (lp.z + W * 0.5f) / W;
                if (u < 0f || u >= 1f || v < 0f || v >= 1f) continue;
                int ix = (int)(u * rx), iz = (int)(v * rz);
                int k = iz * rx + ix;
                count[k]++;
                if (count[k] > maxC) maxC = count[k];
            }
            if (_heatTex == null || _heatTex.width != rx || _heatTex.height != rz)
                _heatTex = new Texture2D(rx, rz, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var cols = new Color[rx * rz];
            float inv = 1f / maxC;
            for (int k = 0; k < cols.Length; k++)
                cols[k] = count[k] == 0 ? new Color(0, 0, 0, 0) : HeatColor(count[k] * inv);
            _heatTex.SetPixels(cols);
            _heatTex.Apply();
            Transform t = Child("ReachHeatmap");
            MeshFilter mf = Ensure<MeshFilter>(t);
            MeshRenderer mr = Ensure<MeshRenderer>(t);
            mf.sharedMesh = Quad(L, W);
            t.position = table.transform.position + table.transform.up * 0.002f;
            t.rotation = table.transform.rotation;
            if (_heatMat == null) _heatMat = MaterialUtil.MakeUnlit();
            MaterialUtil.SetColor(_heatMat, Color.white);
            MaterialUtil.SetTexture(_heatMat, _heatTex);
            MaterialUtil.SetTransparent(_heatMat);
            mr.sharedMaterial = _heatMat;
        }

        void RenderBoundary()
        {
            Transform t = Child("ReachBoundary");
            LineRenderer lr = Ensure<LineRenderer>(t);
            bool on = showBoundary && _tablePts.Count > 2 && table;
            t.gameObject.SetActive(on);
            if (!on) return;
            var pts2d = new List<Vector2>(_tablePts.Count);
            for (int p = 0; p < _tablePts.Count; p++)
            {
                Vector3 lp = table.transform.InverseTransformPoint(_tablePts[p]);
                pts2d.Add(new Vector2(lp.x, lp.z));
            }
            List<Vector2> hull = ConvexHull(pts2d);
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.widthMultiplier = 0.004f;
            lr.positionCount = hull.Count;
            for (int i = 0; i < hull.Count; i++)
                lr.SetPosition(i, table.transform.TransformPoint(new Vector3(hull[i].x, 0.004f, hull[i].y)));
            if (_lineMat == null) _lineMat = MaterialUtil.MakeUnlit();
            MaterialUtil.SetColor(_lineMat, boundaryColor);
            lr.sharedMaterial = _lineMat;
        }

        [Button("Save reach PNG")]
        public void SaveReachPng()
        {
            if (_heatTex == null) { Debug.LogWarning("Compute in Heatmap mode first."); return; }
            byte[] png = _heatTex.EncodeToPNG();
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save reach PNG", "", "table_reach.png", "png");
            if (string.IsNullOrEmpty(path)) return;
#else
            string path = System.IO.Path.Combine(Application.persistentDataPath, "table_reach.png");
#endif
            System.IO.File.WriteAllBytes(path, png);
            Debug.Log("Saved reach PNG: " + path);
        }

#if UNITY_EDITOR
        [Button("Open Graph Window")]
        public void OpenGraph() => EditorTools.ReachGraphWindow.Open(this);
#endif

        [Button]
        public void Clear()
        {
            _tablePts.Clear(); _volumePts.Clear(); _hitPts.Clear();
            SetActiveChild("ReachPoints", false);
            SetActiveChild("TableHits", false);
            SetActiveChild("ReachHeatmap", false);
            SetActiveChild("ReachVolume", false);
            SetActiveChild("ReachBoundary", false);
        }

        [Button("Validate Alignment (Play mode, twin live)")]
        public void ValidateAlignment()
        {
            if (!binding) { Debug.LogWarning("Assign the WorkspaceRobotBinding."); return; }
            float[] real = binding.GetRealAnglesDeg();
            Matrix4x4 t = MyCobot320Fk.TcpBaseMatrix(real, toolLengthMm);
            Vector3 world = BaseMmToWorld(MyCobot320Fk.PosMm(t));
            Vector3 actual = binding.GetTcpWorld();
            Debug.Log($"Reach align error = {(world - actual).magnitude * 1000f:F1} mm | analytic {world} vs TCP {actual}");
        }

        static List<Vector2> ConvexHull(List<Vector2> pts)
        {
            if (pts.Count < 3) return pts;
            pts.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new List<Vector2>();
            for (int i = 0; i < pts.Count; i++)
            {
                while (h.Count >= 2 && Cross(h[h.Count - 2], h[h.Count - 1], pts[i]) <= 0f) h.RemoveAt(h.Count - 1);
                h.Add(pts[i]);
            }
            int lower = h.Count + 1;
            for (int i = pts.Count - 2; i >= 0; i--)
            {
                while (h.Count >= lower && Cross(h[h.Count - 2], h[h.Count - 1], pts[i]) <= 0f) h.RemoveAt(h.Count - 1);
                h.Add(pts[i]);
            }
            h.RemoveAt(h.Count - 1);
            return h;
        }
        static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        static Color HeatColor(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.25f) return Color.Lerp(new Color(0.10f, 0.10f, 0.60f), Color.cyan, t * 4f);
            if (t < 0.50f) return Color.Lerp(Color.cyan, Color.green, (t - 0.25f) * 4f);
            if (t < 0.75f) return Color.Lerp(Color.green, Color.yellow, (t - 0.50f) * 4f);
            return Color.Lerp(Color.yellow, Color.red, (t - 0.75f) * 4f);
        }

        static Mesh Quad(float l, float w)
        {
            float x = l * 0.5f, z = w * 0.5f;
            var m = new Mesh { name = "HeatQuad" };
            m.SetVertices(new List<Vector3> { new(-x, 0, -z), new(x, 0, -z), new(x, 0, z), new(-x, 0, z) });
            m.SetUVs(0, new List<Vector2> { new(0, 0), new(1, 0), new(1, 1), new(0, 1) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        Transform Child(string n)
        {
            Transform t = transform.Find(n);
            if (!t) { var go = new GameObject(n); t = go.transform; t.SetParent(transform, false); }
            return t;
        }
        void SetActiveChild(string n, bool on) { Transform t = transform.Find(n); if (t) t.gameObject.SetActive(on); }
        static T Ensure<T>(Transform t) where T : Component
        {
            T c = t.GetComponent<T>();
            if (!c) c = t.gameObject.AddComponent<T>();
            return c;
        }
    }
}