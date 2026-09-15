using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public class RuntimeToolbar : MonoBehaviour
    {
        [SerializeField] bool show = true;

        WorkspaceTable _table;
        RobotPlacer _placer;
        WorkspaceCamera _cam;
        ReachMapper _reach;

        void Start() => Rebind();

        void Rebind()
        {
            _table = FindFirstObjectByType<WorkspaceTable>();
            _placer = FindFirstObjectByType<RobotPlacer>();
            _cam = FindFirstObjectByType<WorkspaceCamera>();
            _reach = FindFirstObjectByType<ReachMapper>();
        }

        void OnGUI()
        {
            if (!show) return;
            GUILayout.BeginArea(new Rect(10, Screen.height - 250, 210, 240), GUI.skin.box);
            GUILayout.Label("Workspace Mapper");
            if (_table && GUILayout.Button("Rebuild table")) _table.Rebuild();
            if (_placer && GUILayout.Button("Place robot")) _placer.PlaceRobot();
            if (_reach)
            {
                if (GUILayout.Button("Compute reach")) _reach.Compute();
                if (GUILayout.Button("Cycle view")) _reach.CycleMode();
                if (GUILayout.Button("Validate align")) _reach.ValidateAlignment();
                if (GUILayout.Button("Save PNG")) _reach.SaveReachPng();
                if (GUILayout.Button("Clear")) _reach.Clear();
            }
            if (GUILayout.Button("Re-bind")) Rebind();
            GUILayout.EndArea();
        }
    }
}