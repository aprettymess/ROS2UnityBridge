using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    public class RobotMoveTool : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform robotRoot;
        [SerializeField] WorkspaceTable table;
        [SerializeField] Camera cam;

        [Header("Controls")]
        [SerializeField] Key toggleKey = Key.M;
        [SerializeField] bool active = true;
        [SerializeField] bool snapToGrid;
        [SerializeField] float handleLenPx = 90f;
        [SerializeField] float nudgeMeters = 0.005f;

        int _drag = -1; // 0=X 1=Y 2=Z 3=plane
        static Texture2D _tex;
        static readonly Color[] Ax = { new(0.9f, 0.32f, 0.32f), new(0.42f, 0.85f, 0.42f), new(0.4f, 0.6f, 0.95f) };

        Camera Cam => cam ? cam : Camera.main;

        Vector3 AxisDir(int i)
        {
            if (!table) return i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
            return i == 0 ? table.transform.right : i == 1 ? Vector3.up : table.transform.forward;
        }

        void Update()
        {
            Mouse m = Mouse.current;
            Keyboard kb = Keyboard.current;
            if (m == null || kb == null || !robotRoot) return;
            if (kb[toggleKey].wasPressedThisFrame) active = !active;
            if (!active) return;
            Camera c = Cam;
            if (!c) return;

            Vector3 n = Vector3.zero;
            if (kb.leftArrowKey.isPressed) n -= AxisDir(0) * nudgeMeters;
            if (kb.rightArrowKey.isPressed) n += AxisDir(0) * nudgeMeters;
            if (kb.downArrowKey.isPressed) n -= AxisDir(2) * nudgeMeters;
            if (kb.upArrowKey.isPressed) n += AxisDir(2) * nudgeMeters;
            if (n != Vector3.zero) robotRoot.position += n;

            Vector2 mp = m.position.ReadValue();
            if (m.leftButton.wasPressedThisFrame) _drag = HitTest(mp, c);
            if (!m.leftButton.isPressed)
            {
                if (_drag >= 0 && (snapToGrid || kb.leftCtrlKey.isPressed)) SnapToGrid();
                _drag = -1;
            }
            if (_drag < 0) return;

            if (_drag == 3)
            {
                Ray r = c.ScreenPointToRay(mp);
                Plane pl = new Plane(table ? table.transform.up : Vector3.up, robotRoot.position);
                if (pl.Raycast(r, out float t))
                {
                    Vector3 hit = r.GetPoint(t);
                    RobotMover.Move(robotRoot, new Vector3(hit.x, robotRoot.position.y, hit.z), robotRoot.rotation);
                }
                return;
            }
            Vector3 o = robotRoot.position, a = AxisDir(_drag);
            Vector3 so = c.WorldToScreenPoint(o), st = c.WorldToScreenPoint(o + a * 0.1f);
            Vector2 sdir = (Vector2)(st - so);
            float pxPerWorld = sdir.magnitude / 0.1f;
            if (pxPerWorld < 1e-3f) return;
            sdir /= sdir.magnitude;
            float along = Vector2.Dot(m.delta.ReadValue(), sdir) / pxPerWorld;
            RobotMover.Move(robotRoot, o + a * along, robotRoot.rotation);
        }

        int HitTest(Vector2 mp, Camera c)
        {
            Vector3 so = c.WorldToScreenPoint(robotRoot.position);
            if (so.z < 0f) return -1;
            if (((Vector2)so - mp).magnitude < 12f) return 3;
            for (int i = 0; i < 3; i++)
            {
                Vector3 tip = c.WorldToScreenPoint(robotRoot.position + AxisDir(i) * WorldLen(i, c));
                if (DistToSeg(mp, (Vector2)so, (Vector2)tip) < 10f) return i;
            }
            return -1;
        }

        float WorldLen(int i, Camera c)
        {
            Vector3 so = c.WorldToScreenPoint(robotRoot.position);
            Vector3 st = c.WorldToScreenPoint(robotRoot.position + AxisDir(i) * 0.1f);
            float pxPerWorld = ((Vector2)(st - so)).magnitude / 0.1f;
            return pxPerWorld < 1e-3f ? 0.1f : handleLenPx / pxPerWorld;
        }

        static float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (p - (a + ab * t)).magnitude;
        }

        void SnapToGrid()
        {
            if (!table) return;
            float cM = table.CellMeters;
            Vector3 lp = table.transform.InverseTransformPoint(robotRoot.position);
            lp.x = Mathf.Round(lp.x / cM) * cM;
            lp.z = Mathf.Round(lp.z / cM) * cM;
            lp.y = 0f;
            robotRoot.position = table.transform.TransformPoint(lp);
        }

        void OnGUI()
        {
            if (!active || !robotRoot) return;
            Camera c = Cam;
            if (!c) return;
            Vector3 so = c.WorldToScreenPoint(robotRoot.position);
            if (so.z < 0f) return;
            Vector2 go = new Vector2(so.x, Screen.height - so.y);
            for (int i = 0; i < 3; i++)
            {
                Vector3 st = c.WorldToScreenPoint(robotRoot.position + AxisDir(i) * WorldLen(i, c));
                Vector2 gt = new Vector2(st.x, Screen.height - st.y);
                GUI.color = _drag == i ? Color.yellow : Ax[i];
                DrawLine(go, gt, _drag == i ? 4f : 3f);
                GUI.DrawTexture(new Rect(gt.x - 4, gt.y - 4, 8, 8), Tex());
            }
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(go.x - 5, go.y - 5, 10, 10), Tex());
            GUI.Label(new Rect(go.x + 12, go.y + 8, 300, 20), "Move (M) · drag axes · Ctrl=snap · arrows=nudge", Ui.Label);
        }

        static Texture2D Tex()
        {
            if (_tex) return _tex;
            _tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _tex.SetPixel(0, 0, Color.white); _tex.Apply();
            return _tex;
        }
        static void DrawLine(Vector2 a, Vector2 b, float w)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 mat = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, a);
            GUI.DrawTexture(new Rect(a.x, a.y - w * 0.5f, len, w), Tex());
            GUI.matrix = mat;
        }
    }
}