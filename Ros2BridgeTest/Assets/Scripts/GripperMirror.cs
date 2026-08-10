using System.Collections.Generic;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class GripperMirror : MonoBehaviour
{
    [System.Serializable]
    struct MimicMapping
    {
        public string linkObjectName;
        public float multiplier;
        public float offset;
        public bool invertUnitySign;
    }

    [Header("Robot")]
    [SerializeField] ArticulationBody robotRoot;

    [Header("ROS")]
    [SerializeField] string topic = "/joint_states";
    [SerializeField] string actuatedJointName = "gripper_controller";
    [SerializeField] string actuatedLinkName = "gripper_left3";

    [Header("Drive Gains")]
    [SerializeField] float stiffness = 10000f;
    [SerializeField] float damping = 100f;
    [SerializeField] float forceLimit = 1000f;

    [Header("Actuated Joint Sign")]
    [SerializeField] bool invertActuated;

    [Header("Mimic Joints")]
    [SerializeField] List<MimicMapping> mimics = new List<MimicMapping>
    {
        new MimicMapping { linkObjectName = "gripper_left2",  multiplier = 1.0f,  offset = 0f, invertUnitySign = false },
        new MimicMapping { linkObjectName = "gripper_left1",  multiplier = -1.0f, offset = 0f, invertUnitySign = false },
        new MimicMapping { linkObjectName = "gripper_right3", multiplier = -1.0f, offset = 0f, invertUnitySign = false },
        new MimicMapping { linkObjectName = "gripper_right2", multiplier = -1.0f, offset = 0f, invertUnitySign = false },
        new MimicMapping { linkObjectName = "gripper_right1", multiplier = 1.0f,  offset = 0f, invertUnitySign = false }
    };

    ArticulationBody actuatedBody;
    readonly List<(ArticulationBody body, float mult, float off, bool invert)> mimicBodies =
        new List<(ArticulationBody, float, float, bool)>();

    void Start()
    {
        if (robotRoot == null)
        {
            Debug.LogError("GripperMirror: robotRoot not assigned.");
            enabled = false;
            return;
        }

        Dictionary<string, ArticulationBody> bodiesByName = new Dictionary<string, ArticulationBody>();
        foreach (ArticulationBody body in robotRoot.GetComponentsInChildren<ArticulationBody>())
            bodiesByName[body.gameObject.name] = body;

        if (!bodiesByName.TryGetValue(actuatedLinkName, out actuatedBody))
        {
            Debug.LogError($"GripperMirror: actuated link '{actuatedLinkName}' not found.");
            enabled = false;
            return;
        }
        ApplyGains(actuatedBody);

        foreach (MimicMapping m in mimics)
        {
            if (!bodiesByName.TryGetValue(m.linkObjectName, out ArticulationBody body))
            {
                Debug.LogError($"GripperMirror: mimic link '{m.linkObjectName}' not found.");
                continue;
            }
            ApplyGains(body);
            mimicBodies.Add((body, m.multiplier, m.offset, m.invertUnitySign));
        }

        Debug.Log($"GripperMirror: actuated + {mimicBodies.Count} mimics, subscribing to {topic}.");
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(topic, OnJointState);
    }

    void ApplyGains(ArticulationBody body)
    {
        ArticulationDrive drive = body.xDrive;
        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.forceLimit = forceLimit;
        body.xDrive = drive;
    }

    void OnJointState(JointStateMsg msg)
    {
        for (int i = 0; i < msg.name.Length; i++)
        {
            if (msg.name[i] != actuatedJointName)
                continue;

            float rad = (float)msg.position[i];
            float actuatedDeg = rad * Mathf.Rad2Deg;
            if (invertActuated)
                actuatedDeg = -actuatedDeg;

            SetTarget(actuatedBody, actuatedDeg);

            foreach ((ArticulationBody body, float mult, float off, bool invert) in mimicBodies)
            {
                float mimicRad = rad * mult + off;
                float mimicDeg = mimicRad * Mathf.Rad2Deg;
                if (invert)
                    mimicDeg = -mimicDeg;
                SetTarget(body, mimicDeg);
            }
            return;
        }
    }

    void SetTarget(ArticulationBody body, float degrees)
    {
        ArticulationDrive drive = body.xDrive;
        drive.target = degrees;
        body.xDrive = drive;
    }
}