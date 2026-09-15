using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    [RequireComponent(typeof(Camera))]
    public class WorkspaceCamera : MonoBehaviour
    {
        public enum Mode { Orbit, Fly }

        [Header("Mode / keys")]
        [SerializeField] Mode _mode = Mode.Orbit;
        [SerializeField] Key _toggleModeKey = Key.F;
        [SerializeField] Key _toggleProjectionKey = Key.O;
        [SerializeField] Key _handKey = Key.H;

        [Header("Clipping")]
        [SerializeField] float _nearClip = 0.003f;
        [SerializeField] float _farClip = 1000f;

        [Header("Focus (orbit)")]
        [SerializeField] Transform _pivot;
        [SerializeField] Vector3 _fallbackFocus = Vector3.zero;

        [Header("Orbit speeds")]
        [SerializeField] float _orbitSpeed = 0.18f;
        [SerializeField] float _panSpeed = 0.0016f;
        [SerializeField] float _zoomSpeed = 0.001f;
        [SerializeField] float _distanceMin = 0.05f;
        [SerializeField] float _distanceMax = 8f;
        [SerializeField] float _pitchMin = -89f;
        [SerializeField] float _pitchMax = 89f;

        [Header("Fly speeds")]
        [SerializeField] float _flyLookSpeed = 0.12f;
        [SerializeField] float _flyMoveSpeed = 1.2f;
        [SerializeField] float _flyBoost = 3f;
        [SerializeField] float _flySpeedMin = 0.05f;
        [SerializeField] float _flySpeedMax = 30f;

        [Header("HUD")]
        [SerializeField] bool _showHud = true;
        [SerializeField] bool _handTool;

        [Header("Start")]
        [SerializeField] float _startYaw = 45f;
        [SerializeField] float _startPitch = 35f;
        [SerializeField] float _startDistance = 1.5f;

        Camera _cam;
        Vector3 _focus;
        float _yaw, _pitch, _distance;
        float _speedToast;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.nearClipPlane = _nearClip;
            _cam.farClipPlane = _farClip;
            _focus = _pivot ? _pivot.position : _fallbackFocus;
            _yaw = _startYaw;
            _pitch = Mathf.Clamp(_startPitch, _pitchMin, _pitchMax);
            _distance = Mathf.Clamp(_startDistance, _distanceMin, _distanceMax);
            ApplyOrbit();
        }

        void Update()
        {
            if (_cam) _cam.nearClipPlane = _nearClip;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb[_toggleModeKey].wasPressedThisFrame) _mode = _mode == Mode.Orbit ? Mode.Fly : Mode.Orbit;
                if (kb[_toggleProjectionKey].wasPressedThisFrame) SetOrthographic(!_cam.orthographic);
                if (kb[_handKey].wasPressedThisFrame) _handTool = !_handTool;
                if (kb.periodKey.isPressed) SetFlySpeed(_flyMoveSpeed * 1.03f);
                if (kb.commaKey.isPressed) SetFlySpeed(_flyMoveSpeed * 0.97f);
            }
            if (_speedToast > 0f) _speedToast -= Time.unscaledDeltaTime;
            if (_mode == Mode.Orbit) TickOrbit(); else TickFly();
        }

        void SetFlySpeed(float v)
        {
            _flyMoveSpeed = Mathf.Clamp(v, _flySpeedMin, _flySpeedMax);
            _speedToast = 1.2f;
        }

        bool LeftPan(Mouse m)
        {
            if (!_handTool || !m.leftButton.isPressed) return false;
            Vector2 d = m.delta.ReadValue();
            _focus += (-transform.right * d.x - transform.up * d.y) * (_panSpeed * Mathf.Max(_distance, 0.3f));
            if (_pivot) _pivot.position = _focus;
            ApplyOrbit();
            return true;
        }

        void TickOrbit()
        {
            Mouse m = Mouse.current;
            if (m == null) return;
            if (_pivot) _focus = _pivot.position;
            if (LeftPan(m)) return;
            Vector2 d = m.delta.ReadValue();
            if (m.rightButton.isPressed)
            {
                _yaw += d.x * _orbitSpeed;
                _pitch = Mathf.Clamp(_pitch - d.y * _orbitSpeed, _pitchMin, _pitchMax);
            }
            if (m.middleButton.isPressed)
            {
                _focus += (-transform.right * d.x - transform.up * d.y) * (_panSpeed * _distance);
                if (_pivot) _pivot.position = _focus;
            }
            float s = m.scroll.ReadValue().y;
            if (Mathf.Abs(s) > 0.01f)
                _distance = Mathf.Clamp(_distance - s * _zoomSpeed * (_distance + 1f), _distanceMin, _distanceMax);
            ApplyOrbit();
        }

        void ApplyOrbit()
        {
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = _focus - rot * Vector3.forward * _distance;
            transform.rotation = rot;
            if (_cam.orthographic) _cam.orthographicSize = Mathf.Max(0.05f, _distance * 0.5f);
        }

        void TickFly()
        {
            Mouse m = Mouse.current;
            Keyboard kb = Keyboard.current;
            if (m == null || kb == null) return;
            if (LeftPan(m)) return;

            float s = m.scroll.ReadValue().y;
            if (Mathf.Abs(s) > 0.01f) SetFlySpeed(_flyMoveSpeed * (1f + Mathf.Sign(s) * 0.12f));

            if (m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue();
                _yaw += d.x * _flyLookSpeed;
                _pitch = Mathf.Clamp(_pitch - d.y * _flyLookSpeed, -89f, 89f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            float boost = kb.leftShiftKey.isPressed ? _flyBoost : 1f;
            Vector3 move = Vector3.zero;
            if (kb.wKey.isPressed) move += transform.forward;
            if (kb.sKey.isPressed) move -= transform.forward;
            if (kb.dKey.isPressed) move += transform.right;
            if (kb.aKey.isPressed) move -= transform.right;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move -= Vector3.up;
            if (move != Vector3.zero)
                transform.position += move.normalized * (_flyMoveSpeed * boost * Time.unscaledDeltaTime);
        }

        void SetOrthographic(bool ortho)
        {
            _cam.orthographic = ortho;
            if (ortho) _cam.orthographicSize = Mathf.Max(0.05f, _distance * 0.5f);
        }

        void SnapView(float yaw, float pitch) { _mode = Mode.Orbit; _yaw = yaw; _pitch = pitch; ApplyOrbit(); }

        void OnGUI()
        {
            if (!_showHud) return;
            GUILayout.BeginArea(new Rect(Screen.width - 196, 10, 186, 210), Ui.Panel);
            GUILayout.Label($"{_mode} | {(_cam && _cam.orthographic ? "Ortho" : "Persp")} | {(_handTool ? "Hand ON" : "Hand off")}", Ui.Header);
            GUILayout.Label($"Fly speed  {_flyMoveSpeed:0.00}   ( , / . )", _speedToast > 0f ? Ui.Header : Ui.Label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Orbit", Ui.Button)) _mode = Mode.Orbit;
            if (GUILayout.Button("Fly", Ui.Button)) _mode = Mode.Fly;
            if (GUILayout.Button("Hand", Ui.Button)) _handTool = !_handTool;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Persp", Ui.Button)) SetOrthographic(false);
            if (GUILayout.Button("Ortho", Ui.Button)) SetOrthographic(true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Top", Ui.Button)) SnapView(0f, 89f);
            if (GUILayout.Button("Front", Ui.Button)) SnapView(0f, 0f);
            if (GUILayout.Button("Side", Ui.Button)) SnapView(90f, 0f);
            GUILayout.EndHorizontal();
            GUILayout.Label("RMB look/orbit · MMB pan · Hand=LMB pan", Ui.Label);
            GUILayout.EndArea();
        }

        public float FlySpeed { get => _flyMoveSpeed; set => SetFlySpeed(value); }
        public bool HandTool { get => _handTool; set => _handTool = value; }
        public bool Ortho => _cam && _cam.orthographic;
        public Mode CurrentMode => _mode;
        public void SetMode(Mode m) => _mode = m;
        public void ToggleProjection() => SetOrthographic(!_cam.orthographic);
        public void ToggleHand() => _handTool = !_handTool;
        public void FrameFocus() { _mode = Mode.Orbit; ApplyOrbit(); }
        public void SnapTop() => SnapView(0f, 89f);
        public void SnapFront() => SnapView(0f, 0f);
        public void SnapSide() => SnapView(90f, 0f);

        public void LookFromDirection(Vector3 worldDir)
        {
            _mode = Mode.Orbit;
            Vector3 fwd = -worldDir.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            Vector3 e = Quaternion.LookRotation(fwd, up).eulerAngles;
            _pitch = e.x > 180f ? e.x - 360f : e.x;
            _yaw = e.y;
            ApplyOrbit();
        }
    }
}