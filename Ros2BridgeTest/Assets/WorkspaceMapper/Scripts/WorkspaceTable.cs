using System.Collections.Generic;
using Interactions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    [ExecuteAlways]
    public class WorkspaceTable : MonoBehaviour
    {
        public enum LengthUnit { Meters, Centimeters, Millimeters, Inches }

        [Header("Units")]
        [SerializeField, OnValueChanged("Rebuild")] LengthUnit unit = LengthUnit.Inches;

        [Header("Table Size (selected unit)")]
        [SerializeField, OnValueChanged("Rebuild")] float length = 36f;
        [SerializeField, OnValueChanged("Rebuild")] float width = 24f;
        [SerializeField, OnValueChanged("Rebuild")] float height = 30f;

        [Header("Grid (selected unit)")]
        [SerializeField, OnValueChanged("Rebuild")] bool showGrid = true;
        [SerializeField, OnValueChanged("Rebuild")] float cell = 1f;
        [SerializeField, Range(1, 10), OnValueChanged("Rebuild")] int majorEvery = 5;

        [Header("Colors")]
        [SerializeField, OnValueChanged("Rebuild")] Color tableColor = new Color(0.85f, 0.86f, 0.88f);
        [SerializeField, OnValueChanged("Rebuild")] Color minorColor = new Color(0.55f, 0.60f, 0.68f);
        [SerializeField, OnValueChanged("Rebuild")] Color majorColor = new Color(0.30f, 0.35f, 0.44f);
        [Header("Overlay")]
        [SerializeField] bool showLegend = true;

        Material _tableMat, _minorMat, _majorMat;

        float ToM => unit switch
        {
            LengthUnit.Meters => 1f, LengthUnit.Centimeters => 0.01f,
            LengthUnit.Millimeters => 0.001f, LengthUnit.Inches => 0.0254f, _ => 1f
        };
        float L => length * ToM;
        float W => width * ToM;
        float H => Mathf.Max(height * ToM, 0.001f);
        float C => Mathf.Max(cell * ToM, 0.002f);

        public float LengthMeters => L;
        public float WidthMeters => W;
        public float CellMeters => C;
        public float LengthU => FromM(L);
        public float WidthU => FromM(W);
        public float HeightU => FromM(H);
        public float CellU => FromM(C);
        public void SetLengthU(float v) { length = Mathf.Max(v, 0.01f); Rebuild(); }
        public void SetWidthU(float v) { width = Mathf.Max(v, 0.01f); Rebuild(); }
        public void SetHeightU(float v) { height = Mathf.Max(v, 0.01f); Rebuild(); }
        public void SetCellU(float v) { cell = Mathf.Max(v, 0.01f); Rebuild(); }

        void OnEnable() => Rebuild();

        [Button(ButtonSizes.Large)]
        public void Rebuild()
        {
            BuildBox();
            BuildGrid();
        }

        void BuildBox()
        {
            Transform t = FindOrCreate("TableBox");
            t.localPosition = new Vector3(0f, -H / 2f, 0f);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            MeshFilter mf = Ensure<MeshFilter>(t);
            MeshRenderer mr = Ensure<MeshRenderer>(t);
            mf.sharedMesh = BoxMesh(L, H, W);
            if (_tableMat == null) _tableMat = MaterialUtil.MakeLit();
            MaterialUtil.SetColor(_tableMat, tableColor);
            mr.sharedMaterial = _tableMat;
        }

        void BuildGrid()
        {
            Transform minor = FindOrCreate("GridMinor");
            Transform major = FindOrCreate("GridMajor");
            minor.gameObject.SetActive(showGrid);
            major.gameObject.SetActive(showGrid);
            if (!showGrid) return;

            var vMinor = new List<Vector3>();
            var vMajor = new List<Vector3>();
            float hx = L / 2f, hz = W / 2f, y = 0.001f;
            int nx = Mathf.FloorToInt(hx / C);
            int nz = Mathf.FloorToInt(hz / C);
            for (int k = -nx; k <= nx; k++)
            {
                float x = Mathf.Clamp(k * C, -hx, hx);
                var g = (k % majorEvery == 0) ? vMajor : vMinor;
                g.Add(new Vector3(x, y, -hz)); g.Add(new Vector3(x, y, hz));
            }
            for (int k = -nz; k <= nz; k++)
            {
                float z = Mathf.Clamp(k * C, -hz, hz);
                var g = (k % majorEvery == 0) ? vMajor : vMinor;
                g.Add(new Vector3(-hx, y, z)); g.Add(new Vector3(hx, y, z));
            }
            Vector3[] b = { new(-hx, y, -hz), new(hx, y, -hz), new(hx, y, hz), new(-hx, y, hz) };
            for (int i = 0; i < 4; i++) { vMajor.Add(b[i]); vMajor.Add(b[(i + 1) % 4]); }
            ApplyLines(minor, vMinor, ref _minorMat, minorColor);
            ApplyLines(major, vMajor, ref _majorMat, majorColor);
        }

        void ApplyLines(Transform t, List<Vector3> verts, ref Material mat, Color c)
        {
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            MeshFilter mf = Ensure<MeshFilter>(t);
            MeshRenderer mr = Ensure<MeshRenderer>(t);
            var mesh = new Mesh { name = t.name };
            mesh.SetVertices(verts);
            int[] idx = new int[verts.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            mesh.SetIndices(idx, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            if (mat == null) mat = MaterialUtil.MakeUnlit();
            MaterialUtil.SetColor(mat, c);
            mr.sharedMaterial = mat;
        }

        Transform FindOrCreate(string n)
        {
            Transform t = transform.Find(n);
            if (!t) { var go = new GameObject(n); t = go.transform; t.SetParent(transform, false); }
            return t;
        }
        static T Ensure<T>(Transform t) where T : Component
        {
            T c = t.GetComponent<T>();
            if (!c) c = t.gameObject.AddComponent<T>();
            return c;
        }
        static Mesh BoxMesh(float l, float h, float w)
        {
            float x = l / 2f, y = h / 2f, z = w / 2f;
            var v = new List<Vector3>
            {
                new(-x,-y,-z), new(x,-y,-z), new(x,y,-z), new(-x,y,-z),
                new(-x,-y,z), new(x,-y,z), new(x,y,z), new(-x,y,z)
            };
            int[] tri = { 0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4, 3,7,6, 3,6,2, 0,4,7, 0,7,3, 1,2,6, 1,6,5 };
            var mesh = new Mesh { name = "TableBox" };
            mesh.SetVertices(v); mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        string UnitTag => unit switch
        {
            LengthUnit.Meters => "m", LengthUnit.Centimeters => "cm",
            LengthUnit.Millimeters => "mm", LengthUnit.Inches => "in", _ => ""
        };
        float FromM(float m) => unit switch
        {
            LengthUnit.Meters => m, LengthUnit.Centimeters => m * 100f,
            LengthUnit.Millimeters => m * 1000f, LengthUnit.Inches => m / 0.0254f, _ => m
        };

        void OnGUI()
        {
            if (!showLegend) { DrawWorldLabels(); return; }
            GUILayout.BeginArea(new Rect(10, 10, 186, 70), Ui.Panel);
            GUILayout.Label("TABLE", Ui.Header);
            GUILayout.Label($"L {FromM(L):0.#} x W {FromM(W):0.#} x H {FromM(H):0.#} {UnitTag}", Ui.Label);
            GUILayout.Label($"Grid cell: {FromM(C):0.##} {UnitTag}", Ui.Label);
            GUILayout.EndArea();
            DrawWorldLabels();
        }

        void DrawWorldLabel(Camera c, Vector3 world, string text)
        {
            Vector3 sp = c.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            GUI.Label(new Rect(sp.x - 30f, Screen.height - sp.y - 8f, 90f, 18f), text, Ui.Label);
        }

        void DrawWorldLabels()
        {
            Camera c = Camera.main;
            if (!c) { var wc = FindFirstObjectByType<WorkspaceCamera>(); if (wc) c = wc.GetComponent<Camera>(); }
            if (!c) return;
            float hx = L * 0.5f, hz = W * 0.5f, y = 0.004f;
            Transform tr = transform;
            DrawWorldLabel(c, tr.TransformPoint(new Vector3(0, y, -hz)), $"L {FromM(L):0.#}{UnitTag}");
            DrawWorldLabel(c, tr.TransformPoint(new Vector3(-hx, y, 0)), $"W {FromM(W):0.#}{UnitTag}");
            DrawWorldLabel(c, tr.TransformPoint(new Vector3(-hx, -H, -hz)), $"H {FromM(H):0.#}{UnitTag}");
            int nx = Mathf.FloorToInt(L / C), nz = Mathf.FloorToInt(W / C);
            for (int k = 0; k <= nx; k += majorEvery)
                DrawWorldLabel(c, tr.TransformPoint(new Vector3(-hx + k * C, y, -hz - 0.015f)), $"{FromM(k * C):0.#}");
            for (int k = 0; k <= nz; k += majorEvery)
                DrawWorldLabel(c, tr.TransformPoint(new Vector3(-hx - 0.015f, y, -hz + k * C)), $"{FromM(k * C):0.#}");
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var style = new GUIStyle { normal = { textColor = Color.white }, fontSize = 11 };
            float hx = L / 2f, hz = W / 2f, y = 0.003f;
            Transform tr = transform;
            UnityEditor.Handles.color = Color.white;
            // dimension labels on edge midpoints
            UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(0, y, -hz)), $"L = {FromM(L):0.#} {UnitTag}", style);
            UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(-hx, y, 0)), $"W = {FromM(W):0.#} {UnitTag}", style);
            UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(-hx, -H, -hz)), $"H = {FromM(H):0.#} {UnitTag}", style);
            // corner coordinates
            UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(-hx, y, -hz)), "(0,0)", style);
            UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(hx, y, hz)), $"({FromM(L):0.#},{FromM(W):0.#})", style);
            // ruler ticks every majorEvery cells along the two near edges
            int nx = Mathf.FloorToInt(L / C), nz = Mathf.FloorToInt(W / C);
            for (int k = 0; k <= nx; k += majorEvery)
                UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(-hx + k * C, y, -hz - 0.01f)), $"{FromM(k * C):0.#}", style);
            for (int k = 0; k <= nz; k += majorEvery)
                UnityEditor.Handles.Label(tr.TransformPoint(new Vector3(-hx - 0.01f, y, -hz + k * C)), $"{FromM(k * C):0.#}", style);
        }
#endif

    }
}