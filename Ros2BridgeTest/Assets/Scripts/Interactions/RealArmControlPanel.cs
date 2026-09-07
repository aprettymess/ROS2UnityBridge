using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Sirenix.OdinInspector;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace Interactions
{
    public class RealArmControlPanel : MonoBehaviour
    {
        [Header("ROS Topics")]
        [SerializeField] string jointStateTopic = "/robot_joint_states";
        [SerializeField] string armDeltaTopic = "/arm_delta";
        [SerializeField] string armTargetTopic = "/arm_target";
        [SerializeField] string gripperCommandTopic = "/gripper_command";
        [SerializeField] string gripperAngleTopic = "/gripper_angle";
        [SerializeField] string executeTopic = "/execute_real";
        [SerializeField] string setMaxStepTopic = "/set_max_step";

        [Header("Safety")]
        [SerializeField] bool armEnabled;

        [Header("J6 Offset (must match bridge publish offset)")]
        [SerializeField] float j6OffsetDeg = 45f;

        static readonly string[] RosJointNames =
        {
            "joint2_to_joint1",
            "joint3_to_joint2",
            "joint4_to_joint3",
            "joint5_to_joint4",
            "joint6_to_joint5",
            "joint6output_to_joint6"
        };

        [Header("Per-Joint Nudge Size (deg)")]
        [SerializeField, Range(1f, 60f)] float nudgeJ1 = 5f;
        [SerializeField, Range(1f, 60f)] float nudgeJ2 = 5f;
        [SerializeField, Range(1f, 60f)] float nudgeJ3 = 5f;
        [SerializeField, Range(1f, 60f)] float nudgeJ4 = 5f;
        [SerializeField, Range(1f, 60f)] float nudgeJ5 = 5f;
        [SerializeField, Range(1f, 60f)] float nudgeJ6 = 5f;

        [Header("Absolute Target (real robot deg)")]
        [SerializeField, Range(-163f, 163f)] float targetJ1;
        [SerializeField, Range(-134f, 134f)] float targetJ2;
        [SerializeField, Range(-163f, 163f)] float targetJ3;
        [SerializeField, Range(-163f, 163f)] float targetJ4;
        [SerializeField, Range(-163f, 163f)] float targetJ5;
        [SerializeField, Range(-173f, 173f)] float targetJ6;

        [Header("Gripper Angle (1=open .. 100=closed)")]
        [SerializeField, Range(1, 100)] int gripperAngle = 50;

        [Header("Max Step Cap Push (deg, bridge ceiling 60)")]
        [SerializeField, Range(1, 60)] int maxStepToPush = 20;

        [ShowInInspector, ReadOnly, LabelText("Twin (RViz) deg")]
        readonly float[] twinDeg = new float[6];

        [ShowInInspector, ReadOnly, LabelText("Real robot deg")]
        readonly float[] realDeg = new float[6];

        ROSConnection ros;
        readonly int[] rosIndex = new int[6];
        bool indexReady;
        bool targetSeeded;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<JointStateMsg>(jointStateTopic, OnJointState);
            ros.RegisterPublisher<Float64MultiArrayMsg>(armDeltaTopic);
            ros.RegisterPublisher<Float64MultiArrayMsg>(armTargetTopic);
            ros.RegisterPublisher<Int32Msg>(gripperCommandTopic);
            ros.RegisterPublisher<Int32Msg>(gripperAngleTopic);
            ros.RegisterPublisher<EmptyMsg>(executeTopic);
            ros.RegisterPublisher<Int32Msg>(setMaxStepTopic);
        }

        void OnJointState(JointStateMsg msg)
        {
            if (!indexReady)
            {
                for (int i = 0; i < 6; i++)
                {
                    rosIndex[i] = -1;
                    for (int j = 0; j < msg.name.Length; j++)
                    {
                        if (msg.name[j] == RosJointNames[i])
                        {
                            rosIndex[i] = j;
                            break;
                        }
                    }
                }
                indexReady = true;
            }

            for (int i = 0; i < 6; i++)
            {
                if (rosIndex[i] < 0 || rosIndex[i] >= msg.position.Length)
                    continue;
                float deg = (float)msg.position[rosIndex[i]] * Mathf.Rad2Deg;
                twinDeg[i] = deg;
                realDeg[i] = i == 5 ? deg - j6OffsetDeg : deg;
            }

            if (!targetSeeded)
            {
                SyncTargets();
                targetSeeded = true;
            }
        }

        void PublishDelta(int jointIndex, float signedNudge)
        {
            if (!armEnabled)
            {
                Debug.LogWarning("RealArmControlPanel: armEnabled is OFF; jog ignored.");
                return;
            }
            double[] data = new double[6];
            data[jointIndex] = signedNudge;
            ros.Publish(armDeltaTopic, new Float64MultiArrayMsg { data = data });
        }

        [BoxGroup("J1"), HorizontalGroup("J1/row"), Button("- J1")]
        void J1Minus() => PublishDelta(0, -nudgeJ1);
        [HorizontalGroup("J1/row"), Button("+ J1")]
        void J1Plus() => PublishDelta(0, nudgeJ1);

        [BoxGroup("J2"), HorizontalGroup("J2/row"), Button("- J2")]
        void J2Minus() => PublishDelta(1, -nudgeJ2);
        [HorizontalGroup("J2/row"), Button("+ J2")]
        void J2Plus() => PublishDelta(1, nudgeJ2);

        [BoxGroup("J3"), HorizontalGroup("J3/row"), Button("- J3")]
        void J3Minus() => PublishDelta(2, -nudgeJ3);
        [HorizontalGroup("J3/row"), Button("+ J3")]
        void J3Plus() => PublishDelta(2, nudgeJ3);

        [BoxGroup("J4"), HorizontalGroup("J4/row"), Button("- J4")]
        void J4Minus() => PublishDelta(3, -nudgeJ4);
        [HorizontalGroup("J4/row"), Button("+ J4")]
        void J4Plus() => PublishDelta(3, nudgeJ4);

        [BoxGroup("J5"), HorizontalGroup("J5/row"), Button("- J5")]
        void J5Minus() => PublishDelta(4, -nudgeJ5);
        [HorizontalGroup("J5/row"), Button("+ J5")]
        void J5Plus() => PublishDelta(4, nudgeJ5);

        [BoxGroup("J6"), HorizontalGroup("J6/row"), Button("- J6")]
        void J6Minus() => PublishDelta(5, -nudgeJ6);
        [HorizontalGroup("J6/row"), Button("+ J6")]
        void J6Plus() => PublishDelta(5, nudgeJ6);

        [BoxGroup("Absolute Target"), Button("Sync Sliders To Current")]
        void SyncTargets()
        {
            targetJ1 = realDeg[0];
            targetJ2 = realDeg[1];
            targetJ3 = realDeg[2];
            targetJ4 = realDeg[3];
            targetJ5 = realDeg[4];
            targetJ6 = realDeg[5];
        }

        [BoxGroup("Absolute Target"), Button(ButtonSizes.Large), GUIColor(0.5f, 0.9f, 0.5f)]
        void GoToTarget()
        {
            if (!armEnabled)
            {
                Debug.LogWarning("RealArmControlPanel: armEnabled is OFF; Go To Target ignored.");
                return;
            }
            double[] data =
            {
                targetJ1, targetJ2, targetJ3, targetJ4, targetJ5, targetJ6
            };
            ros.Publish(armTargetTopic, new Float64MultiArrayMsg { data = data });
        }

        [BoxGroup("Gripper"), HorizontalGroup("Gripper/row"), Button("Open")]
        void GripperOpen() => ros.Publish(gripperCommandTopic, new Int32Msg { data = 0 });
        [HorizontalGroup("Gripper/row"), Button("Close")]
        void GripperClose() => ros.Publish(gripperCommandTopic, new Int32Msg { data = 1 });

        [BoxGroup("Gripper"), Button("Send Angle")]
        void GripperSendAngle() => ros.Publish(gripperAngleTopic, new Int32Msg { data = gripperAngle });

        [BoxGroup("System"), Button("Push Max Step Cap")]
        void PushMaxStep() => ros.Publish(setMaxStepTopic, new Int32Msg { data = maxStepToPush });

        [BoxGroup("System"), Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        void ExecuteCachedPlan()
        {
            if (!armEnabled)
            {
                Debug.LogWarning("RealArmControlPanel: armEnabled is OFF; execute ignored.");
                return;
            }
            ros.Publish(executeTopic, new EmptyMsg());
        }
    }
}