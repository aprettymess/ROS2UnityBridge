using System.Collections.Generic;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class JointMirror : MonoBehaviour
{
    [System.Serializable]
    struct JointMapping
    {
        public string rosJointName;
        public string linkObjectName;
        public bool invert;
    }

    [Header("Robot")]
    [SerializeField] ArticulationBody robotRoot;

    [Header("ROS")]
    [SerializeField] string topic = "/joint_states";

    [Header("Drive Gains")]
    [SerializeField] float stiffness = 10000f;
    [SerializeField] float damping = 100f;
    [SerializeField] float forceLimit = 1000f;

    [Header("Joint Mapping")]
    [SerializeField] List<JointMapping> mappings = new List<JointMapping>
    {
        new JointMapping { rosJointName = "joint2_to_joint1", linkObjectName = "link1", invert = false },
        new JointMapping { rosJointName = "joint3_to_joint2", linkObjectName = "link2", invert = false },
        new JointMapping { rosJointName = "joint4_to_joint3", linkObjectName = "link3", invert = false },
        new JointMapping { rosJointName = "joint5_to_joint4", linkObjectName = "link4", invert = false },
        new JointMapping { rosJointName = "joint6_to_joint5", linkObjectName = "link5", invert = false },
        new JointMapping { rosJointName = "joint6output_to_joint6", linkObjectName = "link6", invert = false }
    };

    readonly Dictionary<string, ArticulationBody> bodiesByJoint = new Dictionary<string, ArticulationBody>();
    readonly Dictionary<string, bool> invertByJoint = new Dictionary<string, bool>();

    void Start()
    {
        if (robotRoot == null)
        {
            Debug.LogError("JointMirror: robotRoot not assigned.");
            enabled = false;
            return;
        }

        Dictionary<string, ArticulationBody> bodiesByName = new Dictionary<string, ArticulationBody>();
        foreach (ArticulationBody body in robotRoot.GetComponentsInChildren<ArticulationBody>())
            bodiesByName[body.gameObject.name] = body;

        foreach (JointMapping m in mappings)
        {
            if (!bodiesByName.TryGetValue(m.linkObjectName, out ArticulationBody body))
            {
                Debug.LogError($"JointMirror: link '{m.linkObjectName}' not found under robot root.");
                continue;
            }

            ArticulationDrive drive = body.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            body.xDrive = drive;

            bodiesByJoint[m.rosJointName] = body;
            invertByJoint[m.rosJointName] = m.invert;
        }

        Debug.Log($"JointMirror: mapped {bodiesByJoint.Count} joints, subscribing to {topic}.");
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(topic, OnJointState);
    }

    void OnJointState(JointStateMsg msg)
    {
        for (int i = 0; i < msg.name.Length; i++)
        {
            if (!bodiesByJoint.TryGetValue(msg.name[i], out ArticulationBody body))
                continue;

            float degrees = (float)msg.position[i] * Mathf.Rad2Deg;
            if (invertByJoint[msg.name[i]])
                degrees = -degrees;

            ArticulationDrive drive = body.xDrive;
            drive.target = degrees;
            body.xDrive = drive;
        }
    }
}