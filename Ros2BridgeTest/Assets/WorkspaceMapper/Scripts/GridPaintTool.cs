using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    public class GridPaintTool : MonoBehaviour
    {
        [SerializeField] WorkspaceTable table;
        [SerializeField] Camera cam;
        [SerializeField] Color paintColor = new Color(0.2f, 0.6f, 1f, 0.6f);
        [SerializeField] Key paintKey = Key.B;
        [SerializeField] Key eraseKey = Key.V;
        [SerializeField, Range(1, 6)] int brushCells = 1;

        readonly Dictionary<Vector2Int, GameObject> _cells = new();
        readonly Dictionary<Color, Material> _mats = new();

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
                int er = brushCells - 1;
                for (int bx = -er; bx <= er; bx++)
                for (int bz = -er; bz <= er; bz++)
                {
                    var ek = new Vector2Int(ix + bx, iz + bz);
                    if (_cells.TryGetValue(ek, out var g)) { Destroy(g); _cells.Remove(ek); }
                }
                return;
            }
            int rad = brushCells - 1;
            for (int bx = -rad; bx <= rad; bx++)
            for (int bz = -rad; bz <= rad; bz++)
                PaintCell(new Vector2Int(ix + bx, iz + bz), cM, L, W);
            return;
        }

        void PaintCell(Vector2Int key, float cM, float L, float W)
        {
            if (_cells.ContainsKey(key)) return;
            int ix = key.x, iz = key.y;
            var cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(cell.GetComponent<Collider>());
            cell.transform.SetParent(transform, false);
            cell.transform.position = table.transform.TransformPoint(new Vector3((ix + 0.5f) * cM - L / 2f, 0.004f, (iz + 0.5f) * cM - W / 2f));
            cell.transform.rotation = table.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
            cell.transform.localScale = Vector3.one * cM * 0.95f;
            cell.GetComponent<Renderer>().sharedMaterial = MatFor(paintColor);
            _cells[key] = cell;
        }

        public Color PaintColor { get => paintColor; set => paintColor = value; }
        public int BrushCells { get => brushCells; set => brushCells = Mathf.Clamp(value, 1, 6); }

        public void PaintWorldPoint(Vector3 world)
        {
            if (!table) return;
            Vector3 local = table.transform.InverseTransformPoint(world);
            float cM = table.CellMeters, L = table.LengthMeters, W = table.WidthMeters;
            int ix = Mathf.FloorToInt((local.x + L / 2f) / cM);
            int iz = Mathf.FloorToInt((local.z + W / 2f) / cM);
            PaintCell(new Vector2Int(ix, iz), cM, L, W);
        }

        Material MatFor(Color c)
        {
            if (!_mats.TryGetValue(c, out Material m))
            {
                m = MaterialUtil.MakeUnlit();
                MaterialUtil.SetColor(m, c);
                MaterialUtil.SetTransparent(m);
                _mats[c] = m;
            }
            return m;
        }

        [Sirenix.OdinInspector.Button]
        public void ClearPaint()
        {
            foreach (var kv in _cells) if (kv.Value) Destroy(kv.Value);
            _cells.Clear();
        }
    }
}