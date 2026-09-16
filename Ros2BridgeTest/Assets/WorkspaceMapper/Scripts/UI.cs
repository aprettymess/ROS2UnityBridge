using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public static class Ui
    {
        static GUIStyle _panel, _header, _button, _buttonOn, _label, _sub;
        static Texture2D _panelTex, _btnTex, _btnHover, _btnOn;
        static Color _cPanel = new(0.07f, 0.09f, 0.14f, 0.92f);
        static Color _cButton = new(0.13f, 0.16f, 0.23f, 0.96f);
        static Color _cAccent = new(0.16f, 0.45f, 0.95f, 1f);
        static Color _cText = new(0.88f, 0.91f, 0.96f, 1f);
        static Color _cHeader = new(0.60f, 0.74f, 1f, 1f);
        static int _radius = 10;
        static bool _built;

        public static void Configure(Color panel, Color button, Color accent, Color text, Color header, int radius)
        {
            _cPanel = panel; _cButton = button; _cAccent = accent; _cText = text; _cHeader = header; _radius = Mathf.Clamp(radius, 2, 20);
            _built = false;
        }

        static Texture2D Round(Color col)
        {
            int s = _radius * 2 + 4;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = new Color(col.r, col.g, col.b, col.a * CornerAlpha(x, y, s, _radius));
            t.SetPixels(px); t.Apply();
            return t;
        }

        static float CornerAlpha(int x, int y, int s, int r)
        {
            float dx = 0f, dy = 0f;
            if (x < r) dx = r - x; else if (x >= s - r) dx = x - (s - r - 1);
            if (y < r) dy = r - y; else if (y >= s - r) dy = y - (s - r - 1);
            if (dx <= 0f && dy <= 0f) return 1f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(r - d + 0.5f);
        }

        static void Build()
        {
            if (_built) return;
            _panelTex = Round(_cPanel);
            _btnTex = Round(_cButton);
            _btnHover = Round(new Color(_cAccent.r, _cAccent.g, _cAccent.b, 0.9f));
            _btnOn = Round(new Color(_cAccent.r * 0.7f, _cAccent.g * 0.7f, _cAccent.b * 0.8f, 1f));
            var b = new RectOffset(_radius, _radius, _radius, _radius);

            _panel = new GUIStyle { padding = new RectOffset(12, 12, 10, 12), border = b };
            _panel.normal.background = _panelTex;

            _header = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 12, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 4, 6) };
            _header.normal.textColor = _cHeader;

            _button = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 12, padding = new RectOffset(10, 10, 7, 7), margin = new RectOffset(0, 0, 3, 3), border = b };
            _button.normal.background = _btnTex; _button.normal.textColor = _cText;
            _button.hover.background = _btnHover; _button.hover.textColor = Color.white;
            _button.active.background = _btnOn; _button.active.textColor = Color.white;

            _buttonOn = new GUIStyle(_button);
            _buttonOn.normal.background = _btnOn; _buttonOn.normal.textColor = Color.white;
            _buttonOn.onNormal.background = _btnOn; _buttonOn.onNormal.textColor = Color.white;

            _label = new GUIStyle { fontSize = 11, margin = new RectOffset(2, 2, 2, 2) };
            _label.normal.textColor = _cText;

            _sub = new GUIStyle(_label) { fontSize = 10 };
            _sub.normal.textColor = new Color(_cText.r, _cText.g, _cText.b, 0.6f);

            _built = true;
        }

        public static GUIStyle Panel { get { Build(); return _panel; } }
        public static GUIStyle Header { get { Build(); return _header; } }
        public static GUIStyle Button { get { Build(); return _button; } }
        public static GUIStyle ButtonOn { get { Build(); return _buttonOn; } }
        public static GUIStyle Label { get { Build(); return _label; } }
        public static GUIStyle Sub { get { Build(); return _sub; } }

        public static void SetTextColor(Color c) { Build(); _label.normal.textColor = c; _sub.normal.textColor = new Color(c.r, c.g, c.b, 0.6f); }
        public static void SetAccent(Color c) { _cAccent = c; _built = false; }
    }
}