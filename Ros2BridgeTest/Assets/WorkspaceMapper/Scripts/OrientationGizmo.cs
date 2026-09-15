using UnityEngine;
using WorkspaceMapper.Scripts;

namespace Interactions
{
    [RequireComponent(typeof(Camera))]
    public class OrientationGizmo : MonoBehaviour
    {
        [SerializeField] WorkspaceCamera controller;
        [SerializeField] float radius = 30f;
        [SerializeField] Vector2 marginFromBottomRight = new Vector2(70f, 80f);

        Camera _cam;
        static readonly Vector3[] Axes = { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };
        static readonly string[] Names = { "X", "Y", "Z", "", "", "" };
        static readonly Color[] Cols =
        {
            new Color(0.90f, 0.33f, 0.33f), new Color(0.45f, 0.82f, 0.45f), new Color(0.40f, 0.60f, 0.95f),
            new Color(0.55f, 0.30f, 0.30f), new Color(0.30f, 0.50f, 0.32f), new Color(0.28f, 0.40f, 0.62f)
        };
        static Texture2D _dot;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (!controller) controller = GetComponent<WorkspaceCamera>();
        }

        static Texture2D Dot()
        {
            if (_dot) return _dot;
            _dot = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _dot.SetPixel(0, 0, Color.white); _dot.Apply();
            return _dot;
        }

        void OnGUI()
        {
            Vector2 c = new Vector2(Screen.width - marginFromBottomRight.x, Screen.height - marginFromBottomRight.y);
            Quaternion inv = Quaternion.Inverse(transform.rotation);

            var order = new int[6];
            for (int i = 0; i < 6; i++) order[i] = i;
            System.Array.Sort(order, (a, b) => (inv * Axes[a]).z.CompareTo((inv * Axes[b]).z));

            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            for (int oi = 0; oi < 6; oi++)
            {
                int i = order[oi];
                Vector3 v = inv * Axes[i];
                Vector2 p = c + new Vector2(v.x, -v.y) * radius;
                float depth = (v.z + 1f) * 0.5f;
                float size = Mathf.Lerp(12f, 22f, depth);
                if (i < 3)
                {
                    GUI.color = new Color(Cols[i].r, Cols[i].g, Cols[i].b, 0.5f);
                    DrawLine(c, p, 2f);
                }
                Rect r = new Rect(p.x - size / 2f, p.y - size / 2f, size, size);
                GUI.color = Cols[i];
                if (GUI.Button(r, GUIContent.none)) controller?.LookFromDirection(Axes[i]);
                if (i < 3)
                {
                    GUI.color = Color.white;
                    GUI.Label(new Rect(p.x - 5, p.y - 8, 16, 16), Names[i]);
                }
            }
            GUI.color = Color.white;
        }

        static void DrawLine(Vector2 a, Vector2 b, float w)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, a);
            GUI.DrawTexture(new Rect(a.x, a.y - w / 2f, len, w), Dot());
            GUI.matrix = m;
        }
    }
}