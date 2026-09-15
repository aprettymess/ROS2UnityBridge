using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public static class Ui
    {
        static GUIStyle _panel, _header, _button, _label;
        static Texture2D _panelBg, _btnBg, _btnHover, _btnActive;

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply();
            return t;
        }

        static void Init()
        {
            if (_panel != null) return;
            _panelBg = Solid(new Color(0.08f, 0.09f, 0.12f, 0.93f));
            _btnBg = Solid(new Color(0.17f, 0.19f, 0.25f, 1f));
            _btnHover = Solid(new Color(0.24f, 0.40f, 0.85f, 1f));
            _btnActive = Solid(new Color(0.15f, 0.28f, 0.6f, 1f));
            _panel = new GUIStyle { padding = new RectOffset(10, 10, 8, 10), border = new RectOffset(8, 8, 8, 8) };
            _panel.normal.background = _panelBg;
            _header = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 12, margin = new RectOffset(0, 0, 2, 6) };
            _header.normal.textColor = new Color(0.62f, 0.75f, 1f);
            _button = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 12, padding = new RectOffset(8, 8, 7, 7), margin = new RectOffset(0, 0, 3, 3) };
            _button.normal.background = _btnBg; _button.normal.textColor = new Color(0.92f, 0.94f, 0.98f);
            _button.hover.background = _btnHover; _button.hover.textColor = Color.white;
            _button.active.background = _btnActive; _button.active.textColor = Color.white;
            _label = new GUIStyle { fontSize = 11 };
            _label.normal.textColor = new Color(0.82f, 0.86f, 0.94f);
        }

        public static GUIStyle Panel { get { Init(); return _panel; } }
        public static GUIStyle Header { get { Init(); return _header; } }
        public static GUIStyle Button { get { Init(); return _button; } }
        public static GUIStyle Label { get { Init(); return _label; } }
    }
}