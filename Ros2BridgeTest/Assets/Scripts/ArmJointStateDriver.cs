using System.Collections.Generic;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class ArmJointStateDriver : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] private string topicName = "/joint_command";

    [Header("Robot")]
    [SerializeField] private Transform robotRoot;
    [SerializeField] private int drivenJointCount = 6;

    [Header("Drive")]
    [SerializeField] private float stiffness = 10000f;
    [SerializeField] private float damping = 100f;
    [SerializeField] private float forceLimit = 1000f;

    private readonly List<ArticulationBody> joints = new List<ArticulationBody>();
    private double[] latestPositions;

    private void Start()
    {
        ArticulationBody[] bodies = robotRoot.GetComponentsInChildren<ArticulationBody>();
        if (bodies.Length > 0)
            bodies[0].immovable = true;

        CollectRevoluteJoints(bodies);
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(topicName, OnJointState);
    }

    private void CollectRevoluteJoints(ArticulationBody[] bodies)
    {
        foreach (ArticulationBody body in bodies)
        {
            if (body.jointType != ArticulationJointType.RevoluteJoint)
                continue;

            ArticulationDrive drive = body.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            body.xDrive = drive;

            joints.Add(body);
        }
    }

    private void OnJointState(JointStateMsg message)
    {
        latestPositions = message.position;
    }

    private void Update()
    {
        if (latestPositions == null)
            return;

        int count = Mathf.Min(drivenJointCount, Mathf.Min(joints.Count, latestPositions.Length));
        for (int i = 0; i < count; i++)
        {
            ArticulationDrive drive = joints[i].xDrive;
            drive.target = (float)latestPositions[i] * Mathf.Rad2Deg;
            joints[i].xDrive = drive;
        }
    }
}