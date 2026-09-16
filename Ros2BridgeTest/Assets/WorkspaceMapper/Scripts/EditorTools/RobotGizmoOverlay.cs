#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WorkspaceMapper.Scripts.EditorTools
{
    [InitializeOnLoad]
    public static class RobotGizmoOverlay
    {
        static bool _enabled = true;
        static bool _showRotation;

        static RobotGizmoOverlay()
        {
            SceneView.duringSceneGui += OnScene;
        }

        [MenuItem("Tools/Workspace Mapper/Robot Handle - Enable", false, 20)]
        static void ToggleEnabled() { _enabled = !_enabled; SceneView.RepaintAll(); }
        [MenuItem("Tools/Workspace Mapper/Robot Handle - Enable", true)]
        static bool ToggleEnabledValidate() { Menu.SetChecked("Tools/Workspace Mapper/Robot Handle - Enable", _enabled); return true; }

        [MenuItem("Tools/Workspace Mapper/Robot Handle - Rotation", false, 21)]
        static void ToggleRotation() { _showRotation = !_showRotation; SceneView.RepaintAll(); }
        [MenuItem("Tools/Workspace Mapper/Robot Handle - Rotation", true)]
        static bool ToggleRotationValidate() { Menu.SetChecked("Tools/Workspace Mapper/Robot Handle - Rotation", _showRotation); return true; }

        static Transform Target()
        {
            var binding = Object.FindFirstObjectByType<WorkspaceRobotBinding>();
            if (binding && binding.RobotRoot) return binding.RobotRoot;
            var placer = Object.FindFirstObjectByType<RobotPlacer>();
            return Selection.activeTransform;
        }

        static void OnScene(SceneView sv)
        {
            if (!_enabled) return;
            Transform t = Target();
            if (!t) return;

            EditorGUI.BeginChangeCheck();
            Vector3 p = Handles.PositionHandle(t.position, t.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Move Robot");
                t.position = p;
            }

            if (_showRotation)
            {
                EditorGUI.BeginChangeCheck();
                Quaternion r = Handles.RotationHandle(t.rotation, t.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(t, "Rotate Robot");
                    t.rotation = r;
                }
            }
        }
    }
}
#endif