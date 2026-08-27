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

    [Header("Debug")]
    [SerializeField] bool logToConsole = true;
    [SerializeField] bool logOnlyOnChange = true;

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
    readonly List<(ArticulationBody body, string name, float mult, float off, bool invert)> mimicBodies =
        new List<(ArticulationBody, string, float, float, bool)>();

    float lastLogged = float.NaN;

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
            mimicBodies.Add((body, m.linkObjectName, m.multiplier, m.offset, m.invertUnitySign));
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
        drive.lowerLimit = -90f;
        drive.upperLimit = 90f;
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

            bool doLog = logToConsole && (!logOnlyOnChange || Mathf.Abs(rad - lastLogged) > 0.001f);
            System.Text.StringBuilder sb = null;
            if (doLog)
            {
                lastLogged = rad;
                sb = new System.Text.StringBuilder();
                sb.AppendLine("===== GripperMirror snapshot =====");
                sb.AppendLine($"  actuated rad={rad:+0.0000} -> {actuatedLinkName} target={actuatedDeg:+0.00} deg (invertActuated={invertActuated})");
            }

            foreach ((ArticulationBody body, string artname, float mult, float off, bool invert) in mimicBodies)
            {
                float mimicRad = rad * mult + off;
                float mimicDeg = mimicRad * Mathf.Rad2Deg;
                if (invert)
                    mimicDeg = -mimicDeg;
                SetTarget(body, mimicDeg);

                if (sb != null)
                    sb.AppendLine($"  {artname,-16} mult={mult:+0.0} invert={invert,-5} -> target={mimicDeg:+0.00} deg");
            }

            if (sb != null)
                Debug.Log(sb.ToString());

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