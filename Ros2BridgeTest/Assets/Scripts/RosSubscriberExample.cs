using RosMessageTypes.UnityRoboticsDemo;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class RosSubscriberExample : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] private string topicName = "color";

    [Header("Scene")]
    [SerializeField] private GameObject cube;

    private Renderer cubeRenderer;

    private void Start()
    {
        cubeRenderer = cube.GetComponent<Renderer>();
        ROSConnection.GetOrCreateInstance().Subscribe<UnityColorMsg>(topicName, ColorChange);
    }

    private void ColorChange(UnityColorMsg colorMessage)
    {
        cubeRenderer.material.color = new Color32(
            (byte)colorMessage.r,
            (byte)colorMessage.g,
            (byte)colorMessage.b,
            (byte)colorMessage.a);
    }
}