using Sirenix.OdinInspector;
using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public class WorkspaceRobotBinding : MonoBehaviour
    {
        [Header("Scene References")]
        [Required, SerializeField] Transform robotRoot;
        [SerializeField] ArticulationBody[] joints = new ArticulationBody[6];
        [SerializeField] Transform tcp;

        [Header("Conventions")]
        [SerializeField] float j6OffsetDeg = 45f;

        [Header("Preview Jog (offline planning only)")]
        [InfoBox("Turn OFF the live ROS joint-state drive before enabling this, or the incoming joint states fight the sliders.")]
        [SerializeField] bool previewMode;

        [OnValueChanged("ApplyPreview")] [SerializeField, Range(-163f, 163f)] float j1;
        [OnValueChanged("ApplyPreview")] [SerializeField, Range(-134f, 134f)] float j2;
        [OnValueChanged("ApplyPreview")] [SerializeField, Range(-163f, 163f)] float j3;
        [OnValueChanged("ApplyPreview")] [SerializeField, Range(-163f, 163f)] float j4;
        [OnValueChanged("ApplyPreview")] [SerializeField, Range(-163f, 163f)] float j5;
        [OnValueChanged("ApplyPreview")] [SerializeField, Range(-173f, 173f)] float j6;

        [ShowInInspector, ReadOnly, LabelText("Twin deg (live)")]
        readonly float[] twinDeg = new float[6];
        [ShowInInspector, ReadOnly, LabelText("Real deg (live)")]
        readonly float[] realDeg = new float[6];

        [ShowInInspector, ReadOnly, LabelText("TCP world (m)")]
        Vector3 TcpWorldView => tcp ? tcp.position : Vector3.zero;
        [ShowInInspector, ReadOnly, LabelText("TCP vs base (mm)")]
        Vector3 TcpBaseMmView => (tcp && robotRoot) ? robotRoot.InverseTransformPoint(tcp.position) * 1000f : Vector3.zero;

        public Transform RobotRoot => robotRoot;
        public Transform Tcp => tcp;
        public Vector3 GetTcpWorld() => tcp ? tcp.position : Vector3.zero;
        public float[] GetRealAnglesDeg() => (float[])realDeg.Clone();

        public void DriveRealAngles(float[] real)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!joints[i]) continue;
                float twinTarget = i == 5 ? real[i] + j6OffsetDeg : real[i];
                ArticulationDrive d = joints[i].xDrive;
                d.target = twinTarget;
                joints[i].xDrive = d;
            }
        }

        void Update()
        {
            for (int i = 0; i < 6; i++)
            {
                if (!joints[i]) continue;
                float deg = joints[i].jointPosition[0] * Mathf.Rad2Deg;
                twinDeg[i] = deg;
                realDeg[i] = i == 5 ? deg - j6OffsetDeg : deg;
            }
        }

        void ApplyPreview()
        {
            if (!previewMode) return;
            float[] real = { j1, j2, j3, j4, j5, j6 };
            for (int i = 0; i < 6; i++)
            {
                if (!joints[i]) continue;
                float twinTarget = i == 5 ? real[i] + j6OffsetDeg : real[i];
                ArticulationDrive d = joints[i].xDrive;
                d.target = twinTarget;
                joints[i].xDrive = d;
            }
        }

        [Button("Auto-bind link1..link6")]
        void AutoBind()
        {
            if (!robotRoot) { Debug.LogWarning("Assign robotRoot first."); return; }
            for (int i = 0; i < 6; i++)
            {
                Transform t = FindDeep(robotRoot, "link" + (i + 1));
                joints[i] = t ? t.GetComponent<ArticulationBody>() : null;
                if (!joints[i]) Debug.LogWarning($"link{i + 1} ArticulationBody not found under robotRoot.");
            }
        }

        [Button("Sync sliders from twin")]
        void SyncFromTwin()
        {
            j1 = realDeg[0]; j2 = realDeg[1]; j3 = realDeg[2];
            j4 = realDeg[3]; j5 = realDeg[4]; j6 = realDeg[5];
        }

        static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform r = FindDeep(parent.GetChild(i), name);
                if (r) return r;
            }
            return null;
        }
    }
}