using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class JointStateReceiver : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] private string topicName = "/joint_command";

    [Header("Joint")]
    [SerializeField] private Transform joint;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    private float targetAngleDeg;

    private void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(topicName, OnJointState);
    }

    private void OnJointState(JointStateMsg message)
    {
        if (message.position.Length == 0)
            return;

        targetAngleDeg = (float)message.position[0] * Mathf.Rad2Deg;
    }

    private void Update()
    {
        joint.localRotation = Quaternion.AngleAxis(targetAngleDeg, rotationAxis);
    }
}