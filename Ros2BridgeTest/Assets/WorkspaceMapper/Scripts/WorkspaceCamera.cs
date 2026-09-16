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
        [SerializeField] bool _handTool;

        [Header("Clipping")]
        [SerializeField] float _nearClip = 0.003f;
        [SerializeField] float _farClip = 1000f;

        [Header("Focus")]
        [SerializeField] Transform _pivot;
        [SerializeField] Vector3 _fallbackFocus = Vector3.zero;

        [Header("Orbit")]
        [SerializeField] float _orbitSpeed = 0.2f;
        [SerializeField] float _panSpeed = 0.0018f;
        [SerializeField] float _zoomSpeed = 0.0012f;
        [SerializeField] float _distanceMin = 0.05f;
        [SerializeField] float _distanceMax = 8f;
        [SerializeField] float _pitchMin = -89f;
        [SerializeField] float _pitchMax = 89f;

        [Header("Fly")]
        [SerializeField] float _flyLookSpeed = 0.12f;
        [SerializeField] float _flyMoveSpeed = 1.2f;
        [SerializeField] float _flyBoost = 3f;
        [SerializeField] float _flySpeedMin = 0.05f;
        [SerializeField] float _flySpeedMax = 40f;

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
            Keyboard kb = Keyboard.current;
            Mouse m = Mouse.current;
            if (m == null) return;
            if (kb != null)
            {
                if (kb[_toggleModeKey].wasPressedThisFrame) _mode = _mode == Mode.Orbit ? Mode.Fly : Mode.Orbit;
                if (kb[_toggleProjectionKey].wasPressedThisFrame) SetOrthographic(!_cam.orthographic);
            }
            bool handPan = (_handTool || (kb != null && kb.spaceKey.isPressed)) && m.leftButton.isPressed;
            if (handPan) { Pan(m.delta.ReadValue()); return; }
            if (_mode == Mode.Orbit) TickOrbit(m); else TickFly(m, kb);
        }

        void Pan(Vector2 d)
        {
            _focus += (-transform.right * d.x - transform.up * d.y) * (_panSpeed * Mathf.Max(_distance, 0.3f));
            if (_pivot) _pivot.position = _focus;
            ApplyOrbit();
        }

        void TickOrbit(Mouse m)
        {
            if (_pivot) _focus = _pivot.position;
            Vector2 d = m.delta.ReadValue();
            if (m.rightButton.isPressed)
            {
                _yaw += d.x * _orbitSpeed;
                _pitch = Mathf.Clamp(_pitch - d.y * _orbitSpeed, _pitchMin, _pitchMax);
            }
            if (m.middleButton.isPressed) Pan(d);
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

        void TickFly(Mouse m, Keyboard kb)
        {
            float s = m.scroll.ReadValue().y;
            if (Mathf.Abs(s) > 0.01f) SetFlySpeed(_flyMoveSpeed * (1f + Mathf.Sign(s) * 0.12f));
            if (kb == null) return;
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

        void SetFlySpeed(float v) => _flyMoveSpeed = Mathf.Clamp(v, _flySpeedMin, _flySpeedMax);
        void SnapView(float yaw, float pitch) { _mode = Mode.Orbit; _yaw = yaw; _pitch = pitch; ApplyOrbit(); }

        public float FlySpeed { get => _flyMoveSpeed; set => SetFlySpeed(value); }
        public float ZoomSpeed { get => _zoomSpeed; set => _zoomSpeed = Mathf.Max(0.0001f, value); }
        public float PanSpeed { get => _panSpeed; set => _panSpeed = Mathf.Max(0.0001f, value); }
        public bool HandTool { get => _handTool; set => _handTool = value; }
        public bool Ortho => _cam && _cam.orthographic;
        public Mode CurrentMode => _mode;
        public void SetMode(Mode m) => _mode = m;
        public void OrbitBy(float dxPixels, float dyPixels)
        {
            _mode = Mode.Orbit;
            _yaw += dxPixels * _orbitSpeed;
            _pitch = Mathf.Clamp(_pitch - dyPixels * _orbitSpeed, _pitchMin, _pitchMax);
            ApplyOrbit();
        }
        public void ZoomBy(float scroll)
        {
            _distance = Mathf.Clamp(_distance - scroll * 0.12f * (_distance + 1f), _distanceMin, _distanceMax);
            ApplyOrbit();
        }
        public void PanBy(float dxPixels, float dyPixels) => Pan(new Vector2(dxPixels, dyPixels));
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