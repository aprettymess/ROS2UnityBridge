#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public class WorkspaceToolbar : EditorWindow
    {
        Vector2 _scroll;

        [MenuItem("Tools/Workspace Mapper/Toolbar")]
        static void Open() => GetWindow<WorkspaceToolbar>("Workspace");

        void OnEnable() => SceneView.duringSceneGui += OnScene;
        void OnDisable() => SceneView.duringSceneGui -= OnScene;

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawButtons();
            EditorGUILayout.EndScrollView();
        }

        static void DrawButtons()
        {
            GUILayout.Label("Table", EditorStyles.boldLabel);
            WorkspaceTable table = FindFirstObjectByType<WorkspaceTable>();
            using (new EditorGUI.DisabledScope(!table))
            {
                if (GUILayout.Button("Rebuild table") && table) table.Rebuild();
                if (GUILayout.Button("Select table") && table) Selection.activeObject = table;
            }

            GUILayout.Space(6);
            GUILayout.Label("Robot", EditorStyles.boldLabel);
            RobotPlacer placer = FindFirstObjectByType<RobotPlacer>();
            using (new EditorGUI.DisabledScope(!placer))
                if (GUILayout.Button("Place robot on table") && placer) placer.PlaceRobot();

            GUILayout.Space(6);
            GUILayout.Label("Camera", EditorStyles.boldLabel);
            WorkspaceCamera cam = FindFirstObjectByType<WorkspaceCamera>();
            using (new EditorGUI.DisabledScope(!cam))
            {
                if (GUILayout.Button("Toggle ortho / perspective") && cam) cam.ToggleProjection();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Orbit") && cam) cam.SetMode(WorkspaceCamera.Mode.Orbit);
                    if (GUILayout.Button("Fly") && cam) cam.SetMode(WorkspaceCamera.Mode.Fly);
                }
                if (GUILayout.Button("Frame focus") && cam) cam.FrameFocus();
            }

            GUILayout.Space(6);
            GUILayout.Label("Reach", EditorStyles.boldLabel);
            ReachMapper reach = FindFirstObjectByType<ReachMapper>();
            using (new EditorGUI.DisabledScope(!reach))
            {
                if (GUILayout.Button("Compute reach") && reach) reach.Compute();
                if (GUILayout.Button("Cycle view (points/heat/volume)") && reach) reach.CycleMode();
                if (GUILayout.Button("Validate alignment") && reach) reach.ValidateAlignment();
                if (GUILayout.Button("Clear") && reach) reach.Clear();
            }
        }

        void OnScene(SceneView sv)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 220, 360), GUI.skin.box);
            DrawButtons();
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
#endif
