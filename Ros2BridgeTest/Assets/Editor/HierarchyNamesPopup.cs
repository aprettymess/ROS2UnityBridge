using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace Editor
{
    public static class HierarchyNamesPopup
    {
        [MenuItem("Tools/Hierarchy/Show Selected Children (Names Only)", priority = 1000)]
        private static void ShowSelectedChildren()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Selection",
                    "Select one or more GameObjects in the Hierarchy, then run:\nTools > Hierarchy > Show Selected Children (Names Only)",
                    "OK"
                );
                return;
            }

            string output = BuildMultipleHierarchiesText(selected);
            string header = selected.Length == 1
                ? $"Selected: {selected[0].name}"
                : $"Selected: {selected.Length} GameObjects";

            HierarchyNamesPopupWindow.Show(
                title: "Hierarchy (Names Only)",
                header: header,
                content: output
            );
        }

        [MenuItem("Tools/Hierarchy/Show Selected Children (Names Only)", validate = true)]
        private static bool ValidateShowSelectedChildren()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TYPE-HINT HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a short human-readable label describing what kind of object
        /// this GameObject "primarily is" — e.g. [TMP Text], [Canvas], [FBX Mesh].
        /// Returns null / empty string when nothing notable is detected.
        /// </summary>
        private static string GetGameObjectTypeHint(GameObject go)
        {
            // ── UI ──────────────────────────────────────────────────────────
            if (go.GetComponent<Canvas>())                          return "[Canvas]";
            if (go.GetComponent<TMPro.TextMeshProUGUI>())           return "[TMP Text (UI)]";
            if (go.GetComponent<TMPro.TextMeshPro>())               return "[TMP Text (World)]";
            if (go.GetComponent<UnityEngine.UI.Text>())             return "[Legacy Text]";
            if (go.GetComponent<UnityEngine.UI.Button>())           return "[UI Button]";
            if (go.GetComponent<UnityEngine.UI.Image>())            return "[UI Image]";
            if (go.GetComponent<UnityEngine.UI.RawImage>())         return "[UI RawImage]";
            if (go.GetComponent<UnityEngine.UI.Slider>())           return "[UI Slider]";
            if (go.GetComponent<UnityEngine.UI.Toggle>())           return "[UI Toggle]";
            if (go.GetComponent<UnityEngine.UI.InputField>())       return "[UI InputField]";
            if (go.GetComponent<TMPro.TMP_InputField>())            return "[TMP InputField]";
            if (go.GetComponent<TMPro.TMP_Dropdown>())              return "[TMP Dropdown]";
            if (go.GetComponent<UnityEngine.UI.ScrollRect>())       return "[UI ScrollView]";
            if (go.GetComponent<UnityEngine.RectTransform>())       return "[UI RectTransform]";

            // ── 3-D Mesh / Model ────────────────────────────────────────────
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                string meshName = smr.sharedMesh != null ? smr.sharedMesh.name : "?";
                bool looksLikeFbx = meshName.Contains("|") || meshName.Contains("@") ||
                                    go.name.EndsWith("_root") || go.name.EndsWith("_geo");
                return looksLikeFbx ? $"[FBX Skinned: {meshName}]" : $"[Skinned Mesh: {meshName}]";
            }

            var mf = go.GetComponent<MeshFilter>();
            if (mf != null)
            {
                string meshName = mf.sharedMesh != null ? mf.sharedMesh.name : "?";
                bool looksLikeFbx = meshName.Contains("|") || meshName.Contains("@");
                return looksLikeFbx ? $"[FBX Mesh: {meshName}]" : $"[Mesh: {meshName}]";
            }

            // ── Audio / Video ───────────────────────────────────────────────
            if (go.GetComponent<AudioSource>())                     return "[Audio Source]";
            if (go.GetComponent<AudioListener>())                   return "[Audio Listener]";
            if (go.GetComponent<UnityEngine.Video.VideoPlayer>())   return "[Video Player]";

            // ── Camera / Lights ─────────────────────────────────────────────
            if (go.GetComponent<Camera>())                          return "[Camera]";
            if (go.GetComponent<Light>())
            {
                var l = go.GetComponent<Light>();
                return $"[Light: {l.type}]";
            }

            // ── Physics ─────────────────────────────────────────────────────
            if (go.GetComponent<Rigidbody>())                       return "[Rigidbody]";
            if (go.GetComponent<Rigidbody2D>())                     return "[Rigidbody2D]";
            if (go.GetComponent<Collider>())
            {
                var col = go.GetComponent<Collider>();
                return $"[Collider: {col.GetType().Name}]";
            }
            if (go.GetComponent<Collider2D>())
            {
                var col2d = go.GetComponent<Collider2D>();
                return $"[Collider2D: {col2d.GetType().Name}]";
            }

            // ── Particle System ─────────────────────────────────────────────
            if (go.GetComponent<ParticleSystem>())                  return "[Particle System]";

            // ── Animation / Animator ────────────────────────────────────────
            if (go.GetComponent<Animator>())
            {
                var anim = go.GetComponent<Animator>();
                string ctrlName = anim.runtimeAnimatorController != null
                    ? anim.runtimeAnimatorController.name : "none";
                return $"[Animator: {ctrlName}]";
            }
            if (go.GetComponent<Animation>())                       return "[Animation (Legacy)]";

            // ── Empty / Container ───────────────────────────────────────────
            // Only a Transform attached → plain empty GameObject
            var comps = go.GetComponents<Component>();
            if (comps.Length == 1)                                  return "[Empty]";

            return string.Empty;   // Has components but nothing matched above — list them below
        }

        /// <summary>
        /// Returns a compact component list string, e.g.
        ///   "  └─ components: BoxCollider, Rigidbody, MyScript"
        /// Skips Transform (always present) and components matched by the type hint
        /// so there's no duplication.
        /// </summary>
        private static string GetComponentsLine(GameObject go, string indentPrefix)
        {
            var components = go.GetComponents<Component>();
            var names = new List<string>(components.Length);

            foreach (var c in components)
            {
                if (c == null) continue;                    // missing script guard
                string typeName = c.GetType().Name;
                if (typeName == "Transform" || typeName == "RectTransform") continue;
                names.Add(typeName);
            }

            if (names.Count == 0) return string.Empty;

            // Use a continuation indent so it visually hangs under the node name.
            return $"{indentPrefix}    ↳ {string.Join(", ", names)}";
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HIERARCHY BUILDERS  (unchanged API, extended output)
        // ─────────────────────────────────────────────────────────────────────

        private static string BuildMultipleHierarchiesText(GameObject[] gameObjects)
        {
            var sb = new StringBuilder(4096);

            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i] == null) continue;

                sb.AppendLine(BuildHierarchyText(gameObjects[i].transform));

                if (i < gameObjects.Length - 1)
                {
                    sb.AppendLine();
                    sb.AppendLine("═══════════════════════════════════════");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string BuildHierarchyText(Transform root)
        {
            var sb = new StringBuilder(2048);

            // Root name + type hint
            string typeHint  = GetGameObjectTypeHint(root.gameObject);
            string rootLabel = string.IsNullOrEmpty(typeHint)
                ? root.name
                : $"{root.name}  {typeHint}";
            sb.AppendLine(rootLabel);

            // Root components (no indent prefix needed at depth 0)
            string rootComps = GetComponentsLine(root.gameObject, string.Empty);
            if (!string.IsNullOrEmpty(rootComps))
                sb.AppendLine(rootComps);

            // Children
            for (int i = 0; i < root.childCount; i++)
            {
                AppendChild(sb, root.GetChild(i), indentLevel: 1, isLast: i == root.childCount - 1);
            }

            return sb.ToString();
        }

        private static void AppendChild(StringBuilder sb, Transform node, int indentLevel, bool isLast)
        {
            string nodePrefix = GetIndentPrefix(indentLevel, isLast);

            // Node name + type hint
            string typeHint  = GetGameObjectTypeHint(node.gameObject);
            string nodeLabel = string.IsNullOrEmpty(typeHint)
                ? node.name
                : $"{node.name}  {typeHint}";

            sb.Append(nodePrefix);
            sb.AppendLine(nodeLabel);

            // Components line — indented to align under the node name
            string compsLine = GetComponentsLine(node.gameObject, nodePrefix);
            if (!string.IsNullOrEmpty(compsLine))
                sb.AppendLine(compsLine);

            // Recurse
            int childCount = node.childCount;
            for (int i = 0; i < childCount; i++)
            {
                AppendChild(sb, node.GetChild(i), indentLevel + 1, isLast: i == childCount - 1);
            }
        }

        private static string GetIndentPrefix(int indentLevel, bool isLast)
        {
            var sb = new StringBuilder(indentLevel * 3);
            for (int i = 0; i < indentLevel - 1; i++)
                sb.Append("   ");

            sb.Append(isLast ? "└─ " : "├─ ");
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EDITOR WINDOW  (unchanged)
        // ─────────────────────────────────────────────────────────────────────

        private class HierarchyNamesPopupWindow : EditorWindow
        {
            private string _header;
            private string _content;
            private Vector2 _scroll;

            public static void Show(string title, string header, string content)
            {
                var window = CreateInstance<HierarchyNamesPopupWindow>();
                window.titleContent = new GUIContent(title);
                window._header  = header;
                window._content = content;
                window.minSize  = new Vector2(520, 420);
                window.ShowUtility();
                window.Focus();
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField(_header, EditorStyles.boldLabel);
                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Copy", GUILayout.Width(120), GUILayout.Height(26)))
                        EditorGUIUtility.systemCopyBuffer = _content;

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Close", GUILayout.Width(120), GUILayout.Height(26)))
                        Close();
                }

                EditorGUILayout.Space(8);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                EditorGUILayout.TextArea(_content, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }
    }
}