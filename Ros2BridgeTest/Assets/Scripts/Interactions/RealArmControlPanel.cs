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
        [SerializeField] string setSpeedTopic = "/set_speed";

        [Header("Safety & Control Mode")]
        [SerializeField] bool armEnabled;
        
        [Tooltip("If true, manual buttons are hidden and moving Absolute Target sliders publishes commands instantly.")]
        [SerializeField] bool liveExecution;

        [Header("J6 Offset")]
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

        // [Header("Nudge Size (deg)")]
        // [SerializeField, Range(1f, 120f)] float globalNudgeSize = 5f;

        [Header("Absolute Target (real robot deg)")]
        [OnValueChanged("OnTargetSliderMoved")]
        [SerializeField, Range(-163f, 163f)] float targetJ1;
        [OnValueChanged("OnTargetSliderMoved")]
        [SerializeField, Range(-134f, 134f)] float targetJ2;
        [OnValueChanged("OnTargetSliderMoved")]
        [SerializeField, Range(-163f, 163f)] float targetJ3;
        [OnValueChanged("OnTargetSliderMoved")]
        [SerializeField, Range(-163f, 163f)] float targetJ4;
        [OnValueChanged("OnTargetSliderMoved")]
        [SerializeField, Range(-163f, 163f)] float targetJ5;
        [OnValueChanged("OnTargetSliderMoved")]
        [SerializeField, Range(-173f, 173f)] float targetJ6;

        [Header("Gripper Angle")]
        [SerializeField, Range(1, 100)] int gripperAngle = 50;

        [Header("Speed")]
        [SerializeField, Range(1, 100)] int robotSpeed = 30;

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
            ros.RegisterPublisher<Int32Msg>(setSpeedTopic);
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

        // void PublishDelta(int jointIndex, float signedNudge)
        // {
        //     if (!armEnabled)
        //     {
        //         Debug.LogWarning("RealArmControlPanel: armEnabled is OFF; jog ignored.");
        //         return;
        //     }
        //     double[] data = new double[6];
        //     data[jointIndex] = signedNudge;
        //     ros.Publish(armDeltaTopic, new Float64MultiArrayMsg { data = data });
        // }

        // // -------------------------------------------------------------
        // // NUDGE BUTTONS (Hidden when liveExecution is true)
        // // -------------------------------------------------------------
        //
        // [HideIf("liveExecution")]
        // [BoxGroup("J1"), HorizontalGroup("J1/row"), Button("- J1")]
        // void J1Minus() => PublishDelta(0, -globalNudgeSize);
        // [HideIf("liveExecution")]
        // [HorizontalGroup("J1/row"), Button("+ J1")]
        // void J1Plus() => PublishDelta(0, globalNudgeSize);
        //
        // [HideIf("liveExecution")]
        // [BoxGroup("J2"), HorizontalGroup("J2/row"), Button("- J2")]
        // void J2Minus() => PublishDelta(1, -globalNudgeSize);
        // [HideIf("liveExecution")]
        // [HorizontalGroup("J2/row"), Button("+ J2")]
        // void J2Plus() => PublishDelta(1, globalNudgeSize);
        //
        // [HideIf("liveExecution")]
        // [BoxGroup("J3"), HorizontalGroup("J3/row"), Button("- J3")]
        // void J3Minus() => PublishDelta(2, -globalNudgeSize);
        // [HideIf("liveExecution")]
        // [HorizontalGroup("J3/row"), Button("+ J3")]
        // void J3Plus() => PublishDelta(2, globalNudgeSize);
        //
        // [HideIf("liveExecution")]
        // [BoxGroup("J4"), HorizontalGroup("J4/row"), Button("- J4")]
        // void J4Minus() => PublishDelta(3, -globalNudgeSize);
        // [HideIf("liveExecution")]
        // [HorizontalGroup("J4/row"), Button("+ J4")]
        // void J4Plus() => PublishDelta(3, globalNudgeSize);
        //
        // [HideIf("liveExecution")]
        // [BoxGroup("J5"), HorizontalGroup("J5/row"), Button("- J5")]
        // void J5Minus() => PublishDelta(4, -globalNudgeSize);
        // [HideIf("liveExecution")]
        // [HorizontalGroup("J5/row"), Button("+ J5")]
        // void J5Plus() => PublishDelta(4, globalNudgeSize);
        //
        // [HideIf("liveExecution")]
        // [BoxGroup("J6"), HorizontalGroup("J6/row"), Button("- J6")]
        // void J6Minus() => PublishDelta(5, -globalNudgeSize);
        // [HideIf("liveExecution")]
        // [HorizontalGroup("J6/row"), Button("+ J6")]
        // void J6Plus() => PublishDelta(5, globalNudgeSize);

        // -------------------------------------------------------------
        // TARGET EXECUTION
        // -------------------------------------------------------------

        void OnTargetSliderMoved()
        {
            // If the toggle is ON and the arm is enabled, automatically publish changes directly
            if (liveExecution && armEnabled)
            {
                double[] data = { targetJ1, targetJ2, targetJ3, targetJ4, targetJ5, targetJ6 };
                ros.Publish(armTargetTopic, new Float64MultiArrayMsg { data = data });
            }
        }

        [HideIf("liveExecution")]
        [BoxGroup("Absolute Target"), Button(ButtonSizes.Large), GUIColor(0.5f, 0.9f, 0.5f)]
        void GoToTarget()
        {
            if (!armEnabled)
            {
                Debug.LogWarning("RealArmControlPanel: armEnabled is OFF; Go To Target ignored.");
                return;
            }
            double[] data = { targetJ1, targetJ2, targetJ3, targetJ4, targetJ5, targetJ6 };
            ros.Publish(armTargetTopic, new Float64MultiArrayMsg { data = data });
        }

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

        // -------------------------------------------------------------
        // SYSTEM & GRIPPER
        // -------------------------------------------------------------

        [BoxGroup("System"), Button("Set Robot Speed")]
        void SetRobotSpeed() => ros.Publish(setSpeedTopic, new Int32Msg { data = robotSpeed });

        [BoxGroup("System"), Button(ButtonSizes.Large), GUIColor(1f, 0.6f, 0.4f)]
        void ZeroAllJoints()
        {
            if (!armEnabled) { Debug.LogWarning("armEnabled OFF; zero ignored."); return; }
            double[] data = { 0, 0, 0, 0, 0, -(j6OffsetDeg) };
            ros.Publish(armTargetTopic, new Float64MultiArrayMsg { data = data });
        }
        
        [BoxGroup("Gripper"), HorizontalGroup("Gripper/row"), Button("Open")]
        void GripperOpen() => ros.Publish(gripperCommandTopic, new Int32Msg { data = 0 });
        [HorizontalGroup("Gripper/row"), Button("Close")]
        void GripperClose() => ros.Publish(gripperCommandTopic, new Int32Msg { data = 1 });

        [BoxGroup("Gripper"), Button("Send Angle")]
        void GripperSendAngle() => ros.Publish(gripperAngleTopic, new Int32Msg { data = gripperAngle });

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