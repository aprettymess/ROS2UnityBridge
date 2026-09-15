#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace WorkspaceMapper.Scripts
{
    [Overlay(typeof(SceneView), "Workspace Mapper", true)]
    public class WorkspaceOverlay : Overlay
    {
        public override VisualElement CreatePanelContent() => new IMGUIContainer(Draw);

        static void Draw()
        {
            GUILayout.Label("Table", EditorStyles.boldLabel);
            WorkspaceTable table = Object.FindFirstObjectByType<WorkspaceTable>();
            using (new EditorGUI.DisabledScope(!table))
            {
                if (GUILayout.Button("Rebuild table") && table) table.Rebuild();
                if (GUILayout.Button("Select table") && table) Selection.activeObject = table;
            }

            GUILayout.Space(4);
            GUILayout.Label("Robot", EditorStyles.boldLabel);
            RobotPlacer placer = Object.FindFirstObjectByType<RobotPlacer>();
            using (new EditorGUI.DisabledScope(!placer))
                if (GUILayout.Button("Place robot") && placer) placer.PlaceRobot();

            GUILayout.Space(4);
            GUILayout.Label("Camera", EditorStyles.boldLabel);
            WorkspaceCamera cam = Object.FindFirstObjectByType<WorkspaceCamera>();
            using (new EditorGUI.DisabledScope(!cam))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Orbit") && cam) cam.SetMode(WorkspaceCamera.Mode.Orbit);
                    if (GUILayout.Button("Fly") && cam) cam.SetMode(WorkspaceCamera.Mode.Fly);
                    if (GUILayout.Button("O/P") && cam) cam.ToggleProjection();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Top") && cam) cam.SnapTop();
                    if (GUILayout.Button("Front") && cam) cam.SnapFront();
                    if (GUILayout.Button("Side") && cam) cam.SnapSide();
                }
            }

            GUILayout.Space(4);
            GUILayout.Label("Reach", EditorStyles.boldLabel);
            ReachMapper reach = Object.FindFirstObjectByType<ReachMapper>();
            using (new EditorGUI.DisabledScope(!reach))
            {
                if (GUILayout.Button("Compute") && reach) reach.Compute();
                if (GUILayout.Button("Cycle view") && reach) reach.CycleMode();
                if (GUILayout.Button("Validate align") && reach) reach.ValidateAlignment();
                if (GUILayout.Button("Save PNG") && reach) reach.SaveReachPng();
                if (GUILayout.Button("Clear") && reach) reach.Clear();
            }
        }
    }
}
#endif