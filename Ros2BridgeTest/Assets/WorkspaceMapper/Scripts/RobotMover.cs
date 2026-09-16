using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public static class RobotMover
    {
        public static ArticulationBody FindBase(Transform root)
        {
            if (!root) return null;
            var abs = root.GetComponentsInChildren<ArticulationBody>(true);
            for (int i = 0; i < abs.Length; i++) if (abs[i].isRoot) return abs[i];
            return abs.Length > 0 ? abs[0] : null;
        }

        public static void Move(Transform robotRoot, Vector3 pos, Quaternion rot)
        {
            if (!robotRoot) return;
            robotRoot.SetPositionAndRotation(pos, rot);
            if (!Application.isPlaying) return;
            ArticulationBody baseAb = FindBase(robotRoot);
            if (baseAb) baseAb.TeleportRoot(baseAb.transform.position, baseAb.transform.rotation);
        }
    }
}