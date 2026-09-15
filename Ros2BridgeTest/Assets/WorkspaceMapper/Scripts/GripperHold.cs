using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public class GripperHold : MonoBehaviour
    {
        [SerializeField] Transform robotRoot;
        [SerializeField] string[] jointNames =
        {
            "gripper_right1", "gripper_right2", "gripper_right3",
            "gripper_left1", "gripper_left2", "gripper_left3"
        };
        [SerializeField] float stiffness = 100000f;
        [SerializeField] float damping = 1000f;
        [SerializeField] bool holdClosed = true;

        ArticulationBody[] _joints;
        float[] _rest;

        void Start() => Bind();

        [Button("Bind & hold gripper")]
        public void Bind()
        {
            if (!robotRoot) { Debug.LogWarning("Assign robotRoot."); return; }
            var list = new List<ArticulationBody>();
            var rest = new List<float>();
            foreach (var t1 in jointNames)
            {
                Transform t = FindDeep(robotRoot, t1);
                ArticulationBody ab = t ? t.GetComponent<ArticulationBody>() : null;
                if (!ab) continue;
                list.Add(ab);
                rest.Add(holdClosed ? 0f : ab.jointPosition[0] * Mathf.Rad2Deg);
            }
            _joints = list.ToArray();
            _rest = rest.ToArray();
            Apply();
            Debug.Log($"GripperHold: holding {_joints.Length} joints.");
        }

        void Apply()
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                ArticulationDrive d = _joints[i].xDrive;
                d.stiffness = stiffness;
                d.damping = damping;
                d.target = _rest[i];
                _joints[i].xDrive = d;
            }
        }

        void FixedUpdate()
        {
            if (_joints == null) return;
            for (int i = 0; i < _joints.Length; i++)
            {
                if (!_joints[i]) continue;
                ArticulationDrive d = _joints[i].xDrive;
                d.target = _rest[i];
                _joints[i].xDrive = d;
            }
        }

        static Transform FindDeep(Transform p, string name)
        {
            if (p.name == name) return p;
            for (int i = 0; i < p.childCount; i++)
            {
                Transform r = FindDeep(p.GetChild(i), name);
                if (r) return r;
            }
            return null;
        }
    }
}