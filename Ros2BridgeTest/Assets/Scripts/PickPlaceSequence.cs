using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.Trajectory;
using RosMessageTypes.Ur5RobotiqMover;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class PickPlaceSequence : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] private string serviceName = "plan_to_pose";

    [Header("Robot")]
    [SerializeField] private Transform robotRoot;
    [SerializeField] private GripperController gripper;
    [SerializeField] private Transform gripperAttachPoint;

    [Header("Scene")]
    [SerializeField] private Transform can;
    [SerializeField] private Transform placeTarget;

    [Header("Grasp geometry")]
    [SerializeField] private float approachHeight = 0.12f;
    [SerializeField] private Vector3 graspEulerDown = new Vector3(90f, 0f, 0f);
    [SerializeField] private float graspClosure = 0.55f;

    [Header("Execution")]
    [SerializeField] private float pointDwell = 0.05f;
    [SerializeField] private float settleWait = 0.3f;

    [Header("Arm drive")]
    [SerializeField] private float stiffness = 10000f;
    [SerializeField] private float damping = 100f;
    [SerializeField] private float forceLimit = 1000f;

    private static readonly string[] ArmJointNames =
    {
        "shoulder_pan_joint", "shoulder_lift_joint", "elbow_joint",
        "wrist_1_joint", "wrist_2_joint", "wrist_3_joint"
    };

    private ROSConnection ros;
    private readonly Dictionary<string, ArticulationBody> armMap =
        new Dictionary<string, ArticulationBody>();
    private Rigidbody canBody;
    private bool busy;
    private bool stepOk;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<PlanToPoseRequest, PlanToPoseResponse>(serviceName);
        MapArmJoints();
        canBody = can.GetComponent<Rigidbody>();
    }

    private void MapArmJoints()
    {
        ArticulationBody[] bodies = robotRoot.GetComponentsInChildren<ArticulationBody>();
        if (bodies.Length > 0)
            bodies[0].immovable = true;

        List<ArticulationBody> revolutes = new List<ArticulationBody>();
        foreach (ArticulationBody b in bodies)
        {
            if (b.jointType == ArticulationJointType.RevoluteJoint)
                revolutes.Add(b);
        }
        for (int i = 0; i < ArmJointNames.Length && i < revolutes.Count; i++)
        {
            ArticulationBody body = revolutes[i];
            ArticulationDrive drive = body.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            body.xDrive = drive;
            armMap[ArmJointNames[i]] = body;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !busy)
            StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        busy = true;
        Quaternion down = Quaternion.Euler(graspEulerDown);
        Vector3 up = Vector3.up * approachHeight;
        Vector3 graspPos = can.position;
        Vector3 placePos = placeTarget.position;

        yield return MoveTo(graspPos + up, down);   if (!stepOk) { busy = false; yield break; }
        yield return MoveTo(graspPos, down);        if (!stepOk) { busy = false; yield break; }

        yield return gripper.SetClosure(graspClosure);
        Attach();
        yield return new WaitForSeconds(settleWait);

        yield return MoveTo(graspPos + up, down);    if (!stepOk) { busy = false; yield break; }
        yield return MoveTo(placePos + up, down);    if (!stepOk) { busy = false; yield break; }
        yield return MoveTo(placePos, down);         if (!stepOk) { busy = false; yield break; }

        Detach();
        yield return gripper.SetClosure(0f);
        yield return new WaitForSeconds(settleWait);

        yield return MoveTo(placePos + up, down);

        busy = false;
    }

    private IEnumerator MoveTo(Vector3 worldPos, Quaternion worldRot)
    {
        stepOk = false;

        JointStateMsg js = new JointStateMsg
        {
            name = ArmJointNames,
            position = new double[ArmJointNames.Length]
        };
        for (int i = 0; i < ArmJointNames.Length; i++)
            js.position[i] = armMap[ArmJointNames[i]].jointPosition[0];

        Vector3 localPos = robotRoot.InverseTransformPoint(worldPos);
        Quaternion localRot = Quaternion.Inverse(robotRoot.rotation) * worldRot;

        PlanToPoseRequest request = new PlanToPoseRequest
        {
            joints_input = js,
            target_pose = new PoseMsg
            {
                position = localPos.To<FLU>(),
                orientation = localRot.To<FLU>()
            }
        };

        bool done = false;
        JointTrajectoryMsg traj = null;
        ros.SendServiceMessage<PlanToPoseResponse>(serviceName, request, (response) =>
        {
            stepOk = response.success && response.trajectory.points.Length > 0;
            traj = response.trajectory;
            done = true;
        });

        while (!done)
            yield return null;

        if (!stepOk)
        {
            Debug.LogError($"[PickPlace] Plan failed for {worldPos}. Sequence halted.");
            yield break;
        }

        yield return ExecuteTrajectory(traj);
    }

    private IEnumerator ExecuteTrajectory(JointTrajectoryMsg trajectory)
    {
        string[] names = trajectory.joint_names;
        foreach (JointTrajectoryPointMsg point in trajectory.points)
        {
            for (int j = 0; j < names.Length; j++)
            {
                if (!armMap.TryGetValue(names[j], out ArticulationBody body))
                    continue;
                ArticulationDrive drive = body.xDrive;
                drive.target = (float)point.positions[j] * Mathf.Rad2Deg;
                body.xDrive = drive;
            }
            yield return new WaitForSeconds(pointDwell);
        }
        yield return new WaitForSeconds(0.2f);
    }

    private void Attach()
    {
        if (canBody != null)
            canBody.isKinematic = true;
        can.SetParent(gripperAttachPoint, true);
        Debug.Log("[PickPlace] Can attached.");
    }

    private void Detach()
    {
        can.SetParent(null, true);
        if (canBody != null)
            canBody.isKinematic = false;
        Debug.Log("[PickPlace] Can released.");
    }
}