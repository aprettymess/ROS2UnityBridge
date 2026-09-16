using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public class RobotStartPose : MonoBehaviour
    {
        [SerializeField] WorkspaceRobotBinding binding;
        [SerializeField] bool zeroOnPlay = true;
        [SerializeField] bool holdAtZero = true;

        float[] _zero = new float[6];

        void Start()
        {
            if (!binding) binding = FindFirstObjectByType<WorkspaceRobotBinding>();
            if (zeroOnPlay && binding)
            {
                binding.EnsureDrivable();
                binding.DriveRealAngles(_zero);
            }
        }

        void FixedUpdate()
        {
            if (holdAtZero && zeroOnPlay && binding) binding.DriveRealAngles(_zero);
        }

        public void ReleaseHold() { holdAtZero = false; }
    }
}