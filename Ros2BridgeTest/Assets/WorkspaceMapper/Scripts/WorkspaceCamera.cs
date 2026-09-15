using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    [RequireComponent(typeof(Camera))]
    public class WorkspaceCamera : MonoBehaviour
    {
        public enum Mode { Orbit, Fly }

        [Header("Mode")]
        [SerializeField] Mode _mode = Mode.Orbit;
        [SerializeField] Key _toggleModeKey = Key.F;
        [SerializeField] Key _toggleProjectionKey = Key.O;

        [Header("Clipping (fixes near-culling)")]
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

        [Header("Start")]
        [SerializeField] float _startYaw = 45f;
        [SerializeField] float _startPitch = 35f;
        [SerializeField] float _startDistance = 1.5f;

        Camera _cam;
        Vector3 _focus;
        float _yaw, _pitch, _distance;

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
            }
            if (HandPan()) return;              // space + LMB pans in either mode
            if (_mode == Mode.Orbit) TickOrbit(); else TickFly();
        }

        bool HandPan()
        {
            Mouse m = Mouse.current; Keyboard kb = Keyboard.current;
            if (m == null || kb == null) return false;
            if (!kb.spaceKey.isPressed || !m.leftButton.isPressed) return false;
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

            // scroll always adjusts fly speed (like Unity scene-view fly)
            float s = m.scroll.ReadValue().y;
            if (Mathf.Abs(s) > 0.01f)
                _flyMoveSpeed = Mathf.Clamp(_flyMoveSpeed * (1f + Mathf.Sign(s) * 0.12f), _flySpeedMin, _flySpeedMax);

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

        void SnapView(float yaw, float pitch)
        {
            _mode = Mode.Orbit; _yaw = yaw; _pitch = pitch; ApplyOrbit();
        }

        void OnGUI()
        {
            if (!_showHud) return;
            GUILayout.BeginArea(new Rect(Screen.width - 172, 10, 162, 250), GUI.skin.box);
            GUILayout.Label($"{_mode} | {(_cam && _cam.orthographic ? "Ortho" : "Persp")}");
            GUILayout.Label($"Fly speed: {_flyMoveSpeed:0.00}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Orbit")) _mode = Mode.Orbit;
            if (GUILayout.Button("Fly")) _mode = Mode.Fly;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Persp")) SetOrthographic(false);
            if (GUILayout.Button("Ortho")) SetOrthographic(true);
            GUILayout.EndHorizontal();
            GUILayout.Label("Snap view");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Top")) SnapView(0f, 89f);
            if (GUILayout.Button("Front")) SnapView(0f, 0f);
            if (GUILayout.Button("Side")) SnapView(90f, 0f);
            GUILayout.EndHorizontal();
            GUILayout.Label("RMB look/orbit · MMB or Space+LMB pan");
            GUILayout.EndArea();
        }

        public void SetMode(Mode m) => _mode = m;
        public void ToggleProjection() => SetOrthographic(!_cam.orthographic);
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