using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    public class GameHud : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] bool show = true;
        [SerializeField] float windowWidth = 214f;
        [SerializeField] float topOffset = 44f;

        [Header("Theme (set here, tweak live in Design)")]
        [SerializeField] Color panelColor = new(0.07f, 0.09f, 0.14f, 0.92f);
        [SerializeField] Color buttonColor = new(0.13f, 0.16f, 0.23f, 0.96f);
        [SerializeField] Color accentColor = new(0.16f, 0.45f, 0.95f, 1f);
        [SerializeField] Color textColor = new(0.88f, 0.91f, 0.96f, 1f);
        [SerializeField] Color headerColor = new(0.60f, 0.74f, 1f, 1f);
        [SerializeField, Range(2, 20)] int cornerRadius = 10;

        WorkspaceTable _table;
        RobotPlacer _placer;
        ReachMapper _reach;
        WorkspaceCamera _cam;
        GridPaintTool _paint;
        WorkspaceRobotBinding _binding;
        RobotStartPose _startPose;
        bool _paintTcpLive;

        Rect _rTools, _rView, _rRobot, _rDesign, _rPlace;
        bool _openTools = true, _openView = true, _openRobot = false, _openDesign = false, _openPlace = false;
        bool _colTools, _colView, _colRobot, _colDesign, _colPlace;
        Vector2 _sTools, _sView, _sRobot, _sDesign, _sPlace;

        int _resizing = -1;
        Vector2 _resizeMouseStart, _resizeSizeStart;
        bool _camLocked, _shadowsOff, _laidOut;

        readonly float[] _joints = new float[6];
        bool _override;
        float _nudge = 5f;
        int _major = 5;

        static readonly Color[] Swatches =
        {
            new(0.2f,0.6f,1f,0.6f), new(1f,0.35f,0.3f,0.6f), new(0.3f,0.85f,0.4f,0.6f),
            new(1f,0.8f,0.2f,0.6f), new(0.7f,0.4f,1f,0.6f), new(0.95f,0.95f,0.95f,0.6f)
        };
        static readonly Color[] TextCols = { Color.white, Color.black, Color.yellow, Color.cyan };
        static readonly string[] Units = { "m", "cm", "mm", "in" };
        static readonly Color[] Accents = { new(0.16f,0.45f,0.95f), new(0.20f,0.75f,0.55f), new(0.85f,0.35f,0.55f), new(0.95f,0.6f,0.2f), new(0.6f,0.4f,0.9f) };

        void Start() { ApplyTheme(); Rebind(); }

        void OnValidate() { ApplyTheme(); }

        public void ApplyTheme() => Ui.Configure(panelColor, buttonColor, accentColor, textColor, headerColor, cornerRadius);

        void Rebind()
        {
            _table = FindFirstObjectByType<WorkspaceTable>();
            _placer = FindFirstObjectByType<RobotPlacer>();
            _reach = FindFirstObjectByType<ReachMapper>();
            _cam = FindFirstObjectByType<WorkspaceCamera>();
            _paint = FindFirstObjectByType<GridPaintTool>();
            _binding = FindFirstObjectByType<WorkspaceRobotBinding>();
            _startPose = FindFirstObjectByType<RobotStartPose>();
        }

        void LayoutCentered()
        {
            float ww = windowWidth, gap = 8f, top = topOffset;
            float total = 4f * ww + 3f * gap;
            float x0 = Mathf.Max(6f, (Screen.width - total) * 0.5f);
            _rTools = new Rect(x0, top, ww, 330f);
            _rView = new Rect(x0 + (ww + gap), top, ww, 320f);
            _rRobot = new Rect(x0 + 2f * (ww + gap), top, ww, 360f);
            _rDesign = new Rect(x0 + 3f * (ww + gap), top, ww, 360f);
            _rPlace = new Rect(10f, top, windowWidth, 300f);
            _laidOut = true;
        }

        void Update()
        {
            if (_override && _binding) _binding.DriveRealAngles(_joints);
            if (_paintTcpLive && _paint && _binding && _binding.Tcp) _paint.PaintWorldPoint(_binding.Tcp.position);
            if (_resizing < 0) return;
            Mouse m = Mouse.current;
            if (m == null || !m.leftButton.isPressed) { _resizing = -1; return; }
            Vector2 cur = m.position.ReadValue();
            Vector2 d = new Vector2(cur.x - _resizeMouseStart.x, -(cur.y - _resizeMouseStart.y));
            float w = Mathf.Clamp(_resizeSizeStart.x + d.x, 150f, 640f);
            float h = Mathf.Clamp(_resizeSizeStart.y + d.y, 90f, 640f);
            if (_resizing == 1) { _rTools.width = w; _rTools.height = h; }
            else if (_resizing == 2) { _rView.width = w; _rView.height = h; }
            else if (_resizing == 3) { _rRobot.width = w; _rRobot.height = h; }
            else if (_resizing == 4) { _rDesign.width = w; _rDesign.height = h; }
            else if (_resizing == 5) { _rPlace.width = w; _rPlace.height = h; }
        }

        void OnGUI()
        {
            if (!show) return;
            if (!_laidOut) LayoutCentered();

            float barW = 540f;
            GUILayout.BeginArea(new Rect((Screen.width - barW) * 0.5f, 8f, barW, 28f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("WORKSPACE HUD", Ui.Header, GUILayout.Width(120));
            _openTools = GUILayout.Toggle(_openTools, "Tools", Ui.Button, GUILayout.Width(58));
            _openView = GUILayout.Toggle(_openView, "View", Ui.Button, GUILayout.Width(52));
            _openRobot = GUILayout.Toggle(_openRobot, "Robot", Ui.Button, GUILayout.Width(58));
            _openDesign = GUILayout.Toggle(_openDesign, "Design", Ui.Button, GUILayout.Width(62));
            _openPlace = GUILayout.Toggle(_openPlace, "Place", Ui.Button, GUILayout.Width(58));
            if (GUILayout.Button("Re-bind", Ui.Button, GUILayout.Width(62))) Rebind();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (_openTools) _rTools = Window(1, _rTools, "TOOLS", ref _colTools, ref _sTools, DrawTools);
            if (_openView) _rView = Window(2, _rView, "VIEW", ref _colView, ref _sView, DrawView);
            if (_openRobot) _rRobot = Window(3, _rRobot, "ROBOT", ref _colRobot, ref _sRobot, DrawRobot);
            if (_openDesign) _rDesign = Window(4, _rDesign, "DESIGN", ref _colDesign, ref _sDesign, DrawDesign);
            if (_openPlace) _rPlace = Window(5, _rPlace, "PLACE", ref _colPlace, ref _sPlace, DrawPlace);
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
                GUILayout.FlexibleSpace();
                GUILayout.Label(title, Ui.Header);
                GUILayout.FlexibleSpace();
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
            else if (id == 3) _openRobot = false;
            else if (id == 4) _openDesign = false;
            else if (id == 5) _openPlace = false;
        }

        void ReleaseForSweep()
        {
            _override = false;
            if (_binding) _binding.RestoreArmDrive();
        }

        void ReleaseStartHold() { if (_startPose) _startPose.ReleaseHold(); }

        void RandomPose(bool maxReach)
        {
            if (!_binding) return;
            Vector2[] lim = MyCobot320Fk.Limits;
            if (maxReach) { _joints[0] = Random.Range(lim[0].x, lim[0].y); _joints[1] = -90f; _joints[2] = 0f; _joints[3] = 0f; _joints[4] = 0f; _joints[5] = 0f; }
            else for (int j = 0; j < 6; j++) _joints[j] = Random.Range(lim[j].x, lim[j].y);
            ReleaseStartHold();
            if (_reach) _reach.StopSweep();
            _binding.EnsureDrivable();
            _binding.DriveRealAngles(_joints);
            _override = true;
        }

        void DrawPlace()
        {
            if (!_placer) { GUILayout.Label("No RobotPlacer", Ui.Label); return; }
            GUILayout.Label("Presets", Ui.Header);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Corner 0,0", Ui.Button)) _placer.PlaceAtFraction(0f, 0f, 0f);
            if (GUILayout.Button("Center", Ui.Button)) _placer.PlaceAtFraction(0.5f, 0.5f, 0f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Front", Ui.Button)) _placer.PlaceAtFraction(0.5f, 0.85f, 0f);
            if (GUILayout.Button("Left", Ui.Button)) _placer.PlaceAtFraction(0.15f, 0.5f, 0f);
            if (GUILayout.Button("Right", Ui.Button)) _placer.PlaceAtFraction(0.85f, 0.5f, 0f);
            GUILayout.EndHorizontal();
            GUILayout.Label("Fine placement (from corner 0,0)", Ui.Header);
            Slider("Pos X", _placer.PosX, 0f, Mathf.Max(1f, _placer.TableLenU), v => _placer.PosX = v);
            Slider("Pos Y", _placer.PosY, 0f, Mathf.Max(1f, _placer.TableWidU), v => _placer.PosY = v);
            Slider("Heading", _placer.HeadingDeg, -180f, 180f, v => _placer.HeadingDeg = v);
            if (GUILayout.Button("Apply / Place", Ui.Button)) _placer.PlaceRobot();
        }

        void DrawTools()
        {
            if (_table && GUILayout.Button("Rebuild table", Ui.Button)) _table.Rebuild();
            if (_placer && GUILayout.Button("Place robot", Ui.Button)) _placer.PlaceRobot();
            if (_reach)
            {
                GUILayout.Label("Reach", Ui.Header);
                if (GUILayout.Button("Compute (analytic)", Ui.Button)) _reach.Compute();
                if (GUILayout.Button("Start driven sweep", Ui.Button)) { ReleaseForSweep(); _reach.StartDrivenSweep(); }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Pause", Ui.Button)) _reach.PauseSweep();
                if (GUILayout.Button("Resume", Ui.Button)) _reach.ResumeSweep();
                if (GUILayout.Button("Stop", Ui.Button)) _reach.StopSweep();
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Cycle view", Ui.Button)) _reach.CycleMode();
                if (GUILayout.Button("Validate align", Ui.Button)) _reach.ValidateAlignment();
#if UNITY_EDITOR
                if (GUILayout.Button("Open graph", Ui.Button)) _reach.OpenGraph();
#endif
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
            Slider("Fly speed", _cam.FlySpeed, 0.1f, 30f, v => _cam.FlySpeed = v);
            Slider("Zoom speed", _cam.ZoomSpeed, 0.0002f, 0.01f, v => _cam.ZoomSpeed = v);
            Slider("Pan speed", _cam.PanSpeed, 0.0002f, 0.01f, v => _cam.PanSpeed = v);
            bool locked = GUILayout.Toggle(_camLocked, " Lock camera view", Ui.Button);
            if (locked != _camLocked) { _camLocked = locked; _cam.enabled = !_camLocked; }
            bool soff = GUILayout.Toggle(_shadowsOff, " Disable shadows", Ui.Button);
            if (soff != _shadowsOff) { _shadowsOff = soff; ApplyShadows(); }
        }

        void DrawRobot()
        {
            if (!_binding) { GUILayout.Label("No WorkspaceRobotBinding", Ui.Label); return; }
            if (!_binding.HasJoints)
            {
                GUILayout.Label("Joints not bound.", Ui.Label);
                if (GUILayout.Button("Bind joints (link1..6)", Ui.Button)) _binding.BindJoints();
            }
            if (GUILayout.Button("Load current angles", Ui.Button))
            {
                float[] cur = _binding.GetRealAnglesDeg();
                for (int i = 0; i < 6; i++) _joints[i] = cur[i];
            }
            bool ov = GUILayout.Toggle(_override, " Manual override (take control)", Ui.Button);
            if (ov != _override)
            {
                _override = ov;
                if (_override)
                {
                    ReleaseStartHold();
                    if (_reach) _reach.StopSweep();
                    float[] cur = _binding.GetRealAnglesDeg();
                    for (int i = 0; i < 6; i++) _joints[i] = cur[i];
                    _binding.EnsureDrivable();
                }
                else _binding.RestoreArmDrive();
            }
            if (_reach && _reach.Sweeping) GUILayout.Label("Sweep running - Stop it or override", Ui.Label);
            Vector2[] lim = MyCobot320Fk.Limits;
            for (int j = 0; j < 6; j++)
            {
                GUILayout.Label($"J{j + 1}   {_joints[j]:0.#}\u00b0", Ui.Label);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", Ui.Button, GUILayout.Width(24))) _joints[j] = Mathf.Clamp(_joints[j] - _nudge, lim[j].x, lim[j].y);
                float v = GUILayout.HorizontalSlider(_joints[j], lim[j].x, lim[j].y);
                if (GUILayout.Button("+", Ui.Button, GUILayout.Width(24))) _joints[j] = Mathf.Clamp(_joints[j] + _nudge, lim[j].x, lim[j].y);
                GUILayout.EndHorizontal();
                if (Mathf.Abs(v - _joints[j]) > 0.001f) _joints[j] = v;
            }
            Slider("Nudge step", _nudge, 0.5f, 20f, v => _nudge = v);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to 0", Ui.Button))
            {
                if (_reach) _reach.StopSweep();
                for (int i = 0; i < 6; i++) _joints[i] = 0f;
                _binding.EnsureDrivable();
                _binding.DriveRealAngles(_joints);
                _override = true;
            }
            if (_reach && GUILayout.Button("Restart sweep", Ui.Button)) { ReleaseForSweep(); _reach.RestartSweep(); }
            GUILayout.EndHorizontal();
            GUILayout.Label("Poses", Ui.Header);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Random pose", Ui.Button)) RandomPose(false);
            if (GUILayout.Button("Max reach", Ui.Button)) RandomPose(true);
            GUILayout.EndHorizontal();
            if (_reach)
            {
                GUILayout.Label($"Sweep speed  (wait {_reach.SweepWaitFrames} - lower=faster)", Ui.Label);
                _reach.SweepWaitFrames = Mathf.RoundToInt(GUILayout.HorizontalSlider(_reach.SweepWaitFrames, 1f, 6f));
            }
        }

        void DrawDesign()
        {
            if (_table)
            {
                GUILayout.Label("Table units", Ui.Header);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < Units.Length; i++)
                    if (GUILayout.Button(Units[i], Ui.Button)) _table.SetUnitByIndex(i);
                GUILayout.EndHorizontal();
                GUILayout.Label("Table size", Ui.Header);
                Slider("Length", _table.LengthU, 4f, 120f, _table.SetLengthU);
                Slider("Width", _table.WidthU, 4f, 120f, _table.SetWidthU);
                Slider("Height", _table.HeightU, 1f, 60f, _table.SetHeightU);
                Slider("Grid cell", _table.CellU, 0.25f, 12f, _table.SetCellU);
                GUILayout.Label($"Major line every  {_major}", Ui.Label);
                int me = Mathf.RoundToInt(GUILayout.HorizontalSlider(_major, 1f, 10f));
                if (me != _major) { _major = me; _table.SetMajorEvery(me); }
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
                _paint.BrushCells = Mathf.RoundToInt(GUILayout.HorizontalSlider(_paint.BrushCells, 1f, 6f));
                if (GUILayout.Button("Clear paint", Ui.Button)) _paint.ClearPaint();
                if (_reach && GUILayout.Button("Paint reach on grid", Ui.Button)) { var pts = _reach.TablePointsWorld; for (int i = 0; i < pts.Count; i++) _paint.PaintWorldPoint(pts[i]); }
                _paintTcpLive = GUILayout.Toggle(_paintTcpLive, " Paint TCP footprint (live)", Ui.Button);
            }
            GUILayout.Label("Text colour", Ui.Header);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < TextCols.Length; i++)
            {
                GUI.color = TextCols[i];
                if (GUILayout.Button(" ", Ui.Button, GUILayout.Width(26), GUILayout.Height(20))) { textColor = TextCols[i]; Ui.SetTextColor(TextCols[i]); }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Label("Theme", Ui.Header);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Accents.Length; i++)
            {
                GUI.color = Accents[i];
                if (GUILayout.Button(" ", Ui.Button, GUILayout.Width(26), GUILayout.Height(20))) { accentColor = Accents[i]; ApplyTheme(); }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Panel opacity  {panelColor.a:0.00}", Ui.Label);
            float pa = GUILayout.HorizontalSlider(panelColor.a, 0.4f, 1f);
            if (Mathf.Abs(pa - panelColor.a) > 0.005f) { panelColor.a = pa; ApplyTheme(); }
            GUILayout.Label($"Corner radius  {cornerRadius}", Ui.Label);
            int cr = Mathf.RoundToInt(GUILayout.HorizontalSlider(cornerRadius, 2f, 20f));
            if (cr != cornerRadius) { cornerRadius = cr; ApplyTheme(); }
        }

        static void Slider(string name, float val, float min, float max, System.Action<float> set)
        {
            GUILayout.Label($"{name}   {val:0.###}", Ui.Label);
            float v = GUILayout.HorizontalSlider(val, min, max);
            if (Mathf.Abs(v - val) > 0.00001f) set(v);
        }

        void ApplyShadows()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
                lights[i].shadows = _shadowsOff ? LightShadows.None : LightShadows.Soft;
        }
    }
}