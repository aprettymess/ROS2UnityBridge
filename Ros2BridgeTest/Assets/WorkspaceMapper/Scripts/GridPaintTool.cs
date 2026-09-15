using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace WorkspaceMapper.Scripts
{
    public class GridPaintTool : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int Cull = Shader.PropertyToID("_Cull");
        [SerializeField] WorkspaceTable table;
        [SerializeField] Camera cam;
        [SerializeField] Color paintColor = new Color(0.2f, 0.6f, 1f, 0.6f);
        [SerializeField] Key paintKey = Key.B;
        [SerializeField] Key eraseKey = Key.V;

        readonly Dictionary<Vector2Int, GameObject> _cells = new();
        Material _mat;

        void Update()
        {
            Mouse m = Mouse.current;
            Keyboard kb = Keyboard.current;
            if (m == null || kb == null || !table) return;
            bool paint = kb[paintKey].isPressed && m.leftButton.isPressed;
            bool erase = kb[eraseKey].isPressed && m.leftButton.isPressed;
            if (!paint && !erase) return;

            Camera c = cam ? cam : Camera.main;
            if (!c) return;
            Ray r = c.ScreenPointToRay(m.position.ReadValue());
            Plane plane = new Plane(table.transform.up, table.transform.position);
            if (!plane.Raycast(r, out float d)) return;

            Vector3 local = table.transform.InverseTransformPoint(r.GetPoint(d));
            float cM = table.CellMeters, L = table.LengthMeters, W = table.WidthMeters;
            int ix = Mathf.FloorToInt((local.x + L / 2f) / cM);
            int iz = Mathf.FloorToInt((local.z + W / 2f) / cM);
            var key = new Vector2Int(ix, iz);

            if (erase)
            {
                if (_cells.TryGetValue(key, out var g)) { Destroy(g); _cells.Remove(key); }
                return;
            }
            if (_cells.ContainsKey(key)) return;

            var cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(cell.GetComponent<Collider>());
            cell.transform.SetParent(transform, false);
            cell.transform.position = table.transform.TransformPoint(new Vector3((ix + 0.5f) * cM - L / 2f, 0.004f, (iz + 0.5f) * cM - W / 2f));
            cell.transform.rotation = table.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
            cell.transform.localScale = Vector3.one * (cM * 0.95f);
            if (_mat == null)
            {
                _mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (_mat.HasProperty(BaseColor)) _mat.SetColor(BaseColor, paintColor);
                _mat.color = paintColor;
                _mat.SetInt(nameID: SrcBlend, (int)BlendMode.SrcAlpha);
                _mat.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
                _mat.SetInt(nameID: ZWrite, 0);
                _mat.SetInt(Cull, 0);
                _mat.renderQueue = (int)RenderQueue.Transparent;
            }
            cell.GetComponent<Renderer>().sharedMaterial = _mat;
            _cells[key] = cell;
        }

        [Sirenix.OdinInspector.Button]
        void ClearPaint()
        {
            foreach (var kv in _cells) if (kv.Value) Destroy(kv.Value);
            _cells.Clear();
        }
    }
}