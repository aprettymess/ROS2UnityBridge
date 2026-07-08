using RosMessageTypes.UnityRoboticsDemo;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class RosPublisherExample : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] private string topicName = "pos_rot";
    [SerializeField] private float publishFrequency = 0.5f;

    [Header("Scene")]
    [SerializeField] private GameObject cube;

    private ROSConnection ros;
    private float timeElapsed;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PosRotMsg>(topicName);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed < publishFrequency)
            return;

        cube.transform.rotation = Random.rotation;

        PosRotMsg message = new PosRotMsg(
            cube.transform.position.x,
            cube.transform.position.y,
            cube.transform.position.z,
            cube.transform.rotation.x,
            cube.transform.rotation.y,
            cube.transform.rotation.z,
            cube.transform.rotation.w
        );

        ros.Publish(topicName, message);
        timeElapsed = 0f;
    }
}