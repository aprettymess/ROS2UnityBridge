#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WorkspaceMapper.Scripts.EditorTools
{
    public class ReachGraphWindow : EditorWindow
    {
        static ReachMapper _mapper;
        float _pointSize = 3f;
        int _maxDraw = 20000;

        public static void Open(ReachMapper m)
        {
            _mapper = m;
            var w = GetWindow<ReachGraphWindow>("Reach Graph");
            w.minSize = new Vector2(440, 480);
            w.Show();
        }

        void OnGUI()
        {
            if (_mapper == null) _mapper = FindFirstObjectByType<ReachMapper>();
            if (_mapper == null || _mapper.Table == null)
            {
                EditorGUILayout.HelpBox("No ReachMapper with a table found. Compute reach, then reopen.", MessageType.Info);
                if (GUILayout.Button("Bind active ReachMapper")) _mapper = FindFirstObjectByType<ReachMapper>();
                return;
            }
            var pts = _mapper.TablePointsWorld;
            EditorGUILayout.LabelField($"Table-level points: {pts.Count:N0}");
            _pointSize = EditorGUILayout.Slider("Point size", _pointSize, 1f, 8f);
            _maxDraw = EditorGUILayout.IntSlider("Max drawn", _maxDraw, 2000, 100000);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export CSV")) ExportCsv(pts);
                if (GUILayout.Button("Refresh")) Repaint();
            }
            Rect area = GUILayoutUtility.GetRect(400, 360, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawPlot(area, pts);
        }

        void DrawPlot(Rect area, IReadOnlyList<Vector3> pts)
        {
            EditorGUI.DrawRect(area, new Color(0.11f, 0.12f, 0.15f));
            var table = _mapper.Table;
            float L = table.LengthMeters, W = table.WidthMeters;
            int stride = pts.Count > _maxDraw ? Mathf.CeilToInt((float)pts.Count / _maxDraw) : 1;
            var col = new Color(0.2f, 0.9f, 0.45f, 0.8f);
            for (int i = 0; i < pts.Count; i += stride)
            {
                Vector3 lp = table.transform.InverseTransformPoint(pts[i]);
                float u = (lp.x + L * 0.5f) / L, v = (lp.z + W * 0.5f) / W;
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                float px = area.x + u * area.width;
                float py = area.y + (1f - v) * area.height;
                EditorGUI.DrawRect(new Rect(px - _pointSize * 0.5f, py - _pointSize * 0.5f, _pointSize, _pointSize), col);
            }
            GUI.Label(new Rect(area.x + 6, area.y + 4, 260, 18), "Top-down table reach  (X →, Y ↑)");
        }

        void ExportCsv(IReadOnlyList<Vector3> pts)
        {
            string path = EditorUtility.SaveFilePanel("Export reach CSV", "", "table_reach.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            var table = _mapper.Table;
            float L = table.LengthMeters, W = table.WidthMeters;
            var sb = new System.Text.StringBuilder("world_x,world_y,world_z,table_x,table_y\n");
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 lp = table.transform.InverseTransformPoint(pts[i]);
                sb.AppendLine($"{pts[i].x:F4},{pts[i].y:F4},{pts[i].z:F4},{lp.x:F4},{lp.z:F4}");
            }
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log("Reach CSV exported: " + path);
        }
    }
}
#endif