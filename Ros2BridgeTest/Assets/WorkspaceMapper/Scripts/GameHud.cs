using Interactions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    public class GameHud : MonoBehaviour
    {
        [SerializeField] bool show = true;

        WorkspaceTable _table;
        RobotPlacer _placer;
        ReachMapper _reach;
        WorkspaceCamera _cam;
        GridPaintTool _paint;

        Rect _rTools = new(10, 42, 214, 360);
        Rect _rView = new(232, 42, 214, 320);
        Rect _rDesign = new(454, 42, 240, 360);
        bool _openTools = true, _openView = true, _openDesign = false;
        bool _colTools, _colView, _colDesign;
        Vector2 _sTools, _sView, _sDesign;

        int _resizing = -1;
        Vector2 _resizeMouseStart, _resizeSizeStart;
        bool _camLocked, _shadowsOff;

        static readonly Color[] Swatches =
        {
            new(0.2f,0.6f,1f,0.6f), new(1f,0.35f,0.3f,0.6f), new(0.3f,0.85f,0.4f,0.6f),
            new(1f,0.8f,0.2f,0.6f), new(0.7f,0.4f,1f,0.6f), new(0.9f,0.9f,0.9f,0.6f)
        };

        void Start() => Rebind();
        void Rebind()
        {
            _table = FindFirstObjectByType<WorkspaceTable>();
            _placer = FindFirstObjectByType<RobotPlacer>();
            _reach = FindFirstObjectByType<ReachMapper>();
            _cam = FindFirstObjectByType<WorkspaceCamera>();
            _paint = FindFirstObjectByType<GridPaintTool>();
        }

        void Update()
        {
            if (_resizing < 0) return;
            Mouse m = Mouse.current;
            if (m == null || !m.leftButton.isPressed) { _resizing = -1; return; }
            Vector2 cur = m.position.ReadValue();
            Vector2 delta = new Vector2(cur.x - _resizeMouseStart.x, -(cur.y - _resizeMouseStart.y));
            float w = Mathf.Clamp(_resizeSizeStart.x + delta.x, 150f, 640f);
            float h = Mathf.Clamp(_resizeSizeStart.y + delta.y, 90f, 640f);
            if (_resizing == 1) { _rTools.width = w; _rTools.height = h; }
            else if (_resizing == 2) { _rView.width = w; _rView.height = h; }
            else if (_resizing == 3) { _rDesign.width = w; _rDesign.height = h; }
        }

        void OnGUI()
        {
            if (!show) return;
            GUILayout.BeginArea(new Rect(10, 8, 690, 28));
            GUILayout.BeginHorizontal();
            GUILayout.Label("WORKSPACE HUD", Ui.Header, GUILayout.Width(120));
            _openTools = GUILayout.Toggle(_openTools, "Tools", Ui.Button, GUILayout.Width(64));
            _openView = GUILayout.Toggle(_openView, "View", Ui.Button, GUILayout.Width(64));
            _openDesign = GUILayout.Toggle(_openDesign, "Design", Ui.Button, GUILayout.Width(64));
            if (GUILayout.Button("Re-bind", Ui.Button, GUILayout.Width(70))) Rebind();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (_openTools) _rTools = Window(1, _rTools, "TOOLS", ref _colTools, ref _sTools, DrawTools);
            if (_openView) _rView = Window(2, _rView, "VIEW", ref _colView, ref _sView, DrawView);
            if (_openDesign) _rDesign = Window(3, _rDesign, "DESIGN", ref _colDesign, ref _sDesign, DrawDesign);
        }

        Rect Window(int id, Rect rect, string title, ref bool collapsed, ref Vector2 scroll, System.Action content)
        {
            float h = collapsed ? 30f : rect.height;
            Vector2 sc = scroll;
            bool col = collapsed;
            float rw = rect.width;
            Rect drawn = GUI.Window(id, new Rect(rect.x, rect.y, rw, h), _ =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(col ? "+" : "\u2013", Ui.Button, GUILayout.Width(26))) col = !col;
                GUILayout.Label(title, Ui.Header);
                if (GUILayout.Button("x", Ui.Button, GUILayout.Width(24))) CloseWindow(id);
                GUILayout.EndHorizontal();
                if (!col)
                {
                    sc = GUILayout.BeginScrollView(sc);
                    content();
                    GUILayout.EndScrollView();
                    Rect grip = new Rect(rw - 18, h - 18, 16, 16);
                    GUI.Box(grip, "\u25e2");
                    Event e = Event.current;
                    if (e.type == EventType.MouseDown && grip.Contains(e.mousePosition))
                    {
                        _resizing = id;
                        _resizeSizeStart = new Vector2(rw, h);
                        if (Mouse.current != null) _resizeMouseStart = Mouse.current.position.ReadValue();
                        e.Use();
                    }
                }
                GUI.DragWindow(new Rect(0, 0, rw, 28));
            }, GUIContent.none, Ui.Panel);
            collapsed = col;
            scroll = sc;
            return new Rect(drawn.x, drawn.y, rect.width, rect.height);
        }

        void CloseWindow(int id)
        {
            if (id == 1) _openTools = false;
            else if (id == 2) _openView = false;
            else if (id == 3) _openDesign = false;
        }

        void DrawTools()
        {
            if (_table && GUILayout.Button("Rebuild table", Ui.Button)) _table.Rebuild();
            if (_placer && GUILayout.Button("Place robot", Ui.Button)) _placer.PlaceRobot();
            if (_reach)
            {
                GUILayout.Label("Reach", Ui.Header);
                if (GUILayout.Button("Compute (analytic)", Ui.Button)) _reach.Compute();
                if (GUILayout.Button("Start driven sweep", Ui.Button)) _reach.StartDrivenSweep();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Pause", Ui.Button)) _reach.PauseSweep();
                if (GUILayout.Button("Resume", Ui.Button)) _reach.ResumeSweep();
                if (GUILayout.Button("Stop", Ui.Button)) _reach.StopSweep();
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Cycle view", Ui.Button)) _reach.CycleMode();
                if (GUILayout.Button("Validate align", Ui.Button)) _reach.ValidateAlignment();
                if (GUILayout.Button("Save PNG", Ui.Button)) _reach.SaveReachPng();
                if (GUILayout.Button("Clear", Ui.Button)) _reach.Clear();
            }
        }

        void DrawView()
        {
            if (!_cam) { GUILayout.Label("No WorkspaceCamera", Ui.Label); return; }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Orbit", Ui.Button)) _cam.SetMode(WorkspaceCamera.Mode.Orbit);
            if (GUILayout.Button("Fly", Ui.Button)) _cam.SetMode(WorkspaceCamera.Mode.Fly);
            if (GUILayout.Button("Hand", Ui.Button)) _cam.ToggleHand();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Persp", Ui.Button)) _cam.ToggleProjection();
            if (GUILayout.Button("Top", Ui.Button)) _cam.SnapTop();
            if (GUILayout.Button("Front", Ui.Button)) _cam.SnapFront();
            if (GUILayout.Button("Side", Ui.Button)) _cam.SnapSide();
            GUILayout.EndHorizontal();
            GUILayout.Label($"Fly speed  {_cam.FlySpeed:0.0}", Ui.Label);
            float fs = GUILayout.HorizontalSlider(_cam.FlySpeed, 0.1f, 30f);
            if (Mathf.Abs(fs - _cam.FlySpeed) > 0.001f) _cam.FlySpeed = fs;

            bool locked = GUILayout.Toggle(_camLocked, " Lock camera view", Ui.Button);
            if (locked != _camLocked) { _camLocked = locked; _cam.enabled = !_camLocked; }
            bool soff = GUILayout.Toggle(_shadowsOff, " Disable shadows", Ui.Button);
            if (soff != _shadowsOff) { _shadowsOff = soff; ApplyShadows(); }
        }

        void DrawDesign()
        {
            if (_table)
            {
                GUILayout.Label("Table size", Ui.Header);
                Slider("Length", _table.LengthU, 4f, 120f, _table.SetLengthU);
                Slider("Width", _table.WidthU, 4f, 120f, _table.SetWidthU);
                Slider("Height", _table.HeightU, 1f, 60f, _table.SetHeightU);
                Slider("Grid cell", _table.CellU, 0.25f, 12f, _table.SetCellU);
            }
            GUILayout.Label("Paint (B+LMB / erase V+LMB)", Ui.Header);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Swatches.Length; i++)
            {
                GUI.color = new Color(Swatches[i].r, Swatches[i].g, Swatches[i].b, 1f);
                if (GUILayout.Button(" ", Ui.Button, GUILayout.Width(26), GUILayout.Height(20)) && _paint) _paint.PaintColor = Swatches[i];
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            if (_paint)
            {
                GUILayout.Label($"Brush  {_paint.BrushCells}", Ui.Label);
                int b = Mathf.RoundToInt(GUILayout.HorizontalSlider(_paint.BrushCells, 1f, 6f));
                _paint.BrushCells = b;
                if (GUILayout.Button("Clear paint", Ui.Button)) _paint.ClearPaint();
            }
        }

        static void Slider(string name, float val, float min, float max, System.Action<float> set)
        {
            GUILayout.Label($"{name}  {val:0.#}", Ui.Label);
            float v = GUILayout.HorizontalSlider(val, min, max);
            if (Mathf.Abs(v - val) > 0.0001f) set(v);
        }

        void ApplyShadows()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
                lights[i].shadows = _shadowsOff ? LightShadows.None : LightShadows.Soft;
        }
    }
}