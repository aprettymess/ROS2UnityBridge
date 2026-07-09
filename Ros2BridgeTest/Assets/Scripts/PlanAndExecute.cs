using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.Trajectory;
using RosMessageTypes.Ur5RobotiqMover;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class PlanAndExecute : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] private string serviceName = "plan_to_pose";

    [Header("Robot")]
    [SerializeField] private Transform robotRoot;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Execution")]
    [SerializeField] private float pointDwell = 0.1f;

    [Header("Drive")]
    [SerializeField] private float stiffness = 10000f;
    [SerializeField] private float damping = 100f;
    [SerializeField] private float forceLimit = 1000f;

    private static readonly string[] ArmJointNames =
    {
        "shoulder_pan_joint", "shoulder_lift_joint", "elbow_joint",
        "wrist_1_joint", "wrist_2_joint", "wrist_3_joint"
    };

    private ROSConnection ros;
    private readonly Dictionary<string, ArticulationBody> jointMap =
        new Dictionary<string, ArticulationBody>();
    private bool busy;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<PlanToPoseRequest, PlanToPoseResponse>(serviceName);
        MapArmJoints();
    }

    private void MapArmJoints()
    {
        ArticulationBody[] bodies = robotRoot.GetComponentsInChildren<ArticulationBody>();
        if (bodies.Length > 0)
            bodies[0].immovable = true;

        List<ArticulationBody> revolutes = new List<ArticulationBody>();
        foreach (ArticulationBody b in bodies)
        {
            if (b.jointType != ArticulationJointType.RevoluteJoint)
                continue;

            ArticulationDrive drive = b.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            b.xDrive = drive;

            revolutes.Add(b);
        }

        for (int i = 0; i < ArmJointNames.Length && i < revolutes.Count; i++)
            jointMap[ArmJointNames[i]] = revolutes[i];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !busy)
            SendRequest();
    }

    private void SendRequest()
    {
        busy = true;

        JointStateMsg js = new JointStateMsg
        {
            name = ArmJointNames,
            position = new double[ArmJointNames.Length]
        };
        for (int i = 0; i < ArmJointNames.Length; i++)
            js.position[i] = jointMap[ArmJointNames[i]].jointPosition[0];

        Vector3 localPos = robotRoot.InverseTransformPoint(target.position);
        Quaternion localRot = Quaternion.Inverse(robotRoot.rotation) * target.rotation;

        PoseMsg pose = new PoseMsg
        {
            position = localPos.To<FLU>(),
            orientation = localRot.To<FLU>()
        };

        PlanToPoseRequest request = new PlanToPoseRequest
        {
            joints_input = js,
            target_pose = pose
        };

        Debug.Log($"[PlanAndExecute] Sending base-frame FLU target: " +
                  $"{pose.position.x:F3}, {pose.position.y:F3}, {pose.position.z:F3}");
        ros.SendServiceMessage<PlanToPoseResponse>(serviceName, request, OnResponse);
    }

    private void OnResponse(PlanToPoseResponse response)
    {
        if (!response.success || response.trajectory.points.Length == 0)
        {
            Debug.LogError("[PlanAndExecute] Planning failed or empty trajectory.");
            busy = false;
            return;
        }

        Debug.Log($"[PlanAndExecute] Trajectory: {response.trajectory.points.Length} points.");
        StartCoroutine(Execute(response.trajectory));
    }

    private IEnumerator Execute(JointTrajectoryMsg trajectory)
    {
        string[] names = trajectory.joint_names;
        foreach (JointTrajectoryPointMsg point in trajectory.points)
        {
            for (int j = 0; j < names.Length; j++)
            {
                if (!jointMap.TryGetValue(names[j], out ArticulationBody body))
                    continue;

                ArticulationDrive drive = body.xDrive;
                drive.target = (float)point.positions[j] * Mathf.Rad2Deg;
                body.xDrive = drive;
            }
            yield return new WaitForSeconds(pointDwell);
        }
        busy = false;
    }
}