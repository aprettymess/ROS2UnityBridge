using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace Interactions
{
    public class GraspAttach : MonoBehaviour
    {
        [Header("ROS")]
        [SerializeField] string gripperCommandTopic = "/gripper_command";

        [Header("References")]
        [SerializeField] Transform gripperBase;
        [SerializeField] Transform cube;

        [Header("Grasp Offset (local to gripper)")]
        [SerializeField] Vector3 graspOffset = Vector3.zero;

        [Header("Debug")]
        [SerializeField] bool held;

        ROSConnection ros;
        bool pendingGrab;
        bool pendingRelease;

        void Start()
        {
            if (gripperBase == null || cube == null)
            {
                Debug.LogError("GraspAttach: assign gripperBase and cube.");
                enabled = false;
                return;
            }
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<Int32Msg>(gripperCommandTopic, OnGripperCommand);
        }

        void OnGripperCommand(Int32Msg msg)
        {
            if (msg.data == 1)
                pendingGrab = true;
            else if (msg.data == 0)
                pendingRelease = true;
        }

        void Update()
        {
            if (pendingGrab)
            {
                pendingGrab = false;
                Grab();
            }
            if (pendingRelease)
            {
                pendingRelease = false;
                Release();
            }
        }

        void Grab()
        {
            if (held)
                return;
            cube.SetParent(gripperBase, true);
            cube.localPosition = graspOffset == Vector3.zero ? cube.localPosition : graspOffset;
            held = true;
            Debug.Log("GraspAttach: cube grabbed.");
        }

        void Release()
        {
            if (!held)
                return;
            cube.SetParent(null, true);
            held = false;
            Debug.Log("GraspAttach: cube released.");
        }
    }
}