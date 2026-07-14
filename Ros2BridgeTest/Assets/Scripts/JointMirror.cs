using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class JointMirror : MonoBehaviour
{
    [Header("ROS Topic")]
    [SerializeField] private string jointStateTopic = "/joint_states";

    [Header("Robot Reference")]
    [SerializeField] private Transform robotRoot;

    [Header("Articulation Bodies")]
    [SerializeField] private ArticulationBody joint2;
    [SerializeField] private ArticulationBody joint3;
    [SerializeField] private ArticulationBody joint4;
    [SerializeField] private ArticulationBody joint5;
    [SerializeField] private ArticulationBody joint6;
    [SerializeField] private ArticulationBody joint6Flange;

    [Header("Drive Settings")]
    [SerializeField] private float stiffness = 10000f;
    [SerializeField] private float damping = 100f;
    [SerializeField] private float forceLimit = 1000f;

    private Dictionary<string, ArticulationBody> jointLookup;

    private void Start()
    {
        AnchorRootBody();

        jointLookup = new Dictionary<string, ArticulationBody>
        {
            { "joint2_to_joint1", joint2 },
            { "joint3_to_joint2", joint3 },
            { "joint4_to_joint3", joint4 },
            { "joint5_to_joint4", joint5 },
            { "joint6_to_joint5", joint6 },
            { "joint6output_to_joint6", joint6Flange }
        };

        ConfigureDrives();

        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(jointStateTopic, OnJointState);
    }

    private void AnchorRootBody()
    {
        ArticulationBody[] bodies = robotRoot.GetComponentsInChildren<ArticulationBody>();
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i].isRoot)
            {
                bodies[i].immovable = true;
                return;
            }
        }

        Debug.LogWarning("JointMirror: no root ArticulationBody found under robotRoot.");
    }

    private void ConfigureDrives()
    {
        foreach (ArticulationBody body in jointLookup.Values)
        {
            if (body == null)
            {
                continue;
            }

            ArticulationDrive drive = body.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            body.xDrive = drive;
        }
    }

    private void OnJointState(JointStateMsg msg)
    {
        for (int i = 0; i < msg.name.Length; i++)
        {
            if (!jointLookup.TryGetValue(msg.name[i], out ArticulationBody body) || body == null)
            {
                continue;
            }

            ArticulationDrive drive = body.xDrive;
            drive.target = (float)msg.position[i] * Mathf.Rad2Deg;
            body.xDrive = drive;
        }
    }
}