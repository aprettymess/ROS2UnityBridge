using RosMessageTypes.MycobotMoverInterfaces;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class PickPlaceTrigger : MonoBehaviour
{
    [Header("ROS")]
    [SerializeField] string serviceName = "pick_place";

    [Header("Trigger")]
    [SerializeField] KeyCode triggerKey = KeyCode.P;

    ROSConnection ros;
    bool inFlight;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<PickPlaceRequest, PickPlaceResponse>(serviceName);
        Debug.Log($"PickPlaceTrigger ready. Press {triggerKey} to run pick-place.");
    }

    void Update()
    {
        if (!inFlight && Input.GetKeyDown(triggerKey))
            SendRequest();
    }

    void SendRequest()
    {
        inFlight = true;
        Debug.Log("PickPlaceTrigger: calling service...");
        PickPlaceRequest req = new PickPlaceRequest();
        req.trigger = true;
        ros.SendServiceMessage<PickPlaceResponse>(serviceName, req, OnResponse);
    }

    void OnResponse(PickPlaceResponse response)
    {
        inFlight = false;
        Debug.Log($"PickPlaceTrigger: response success={response.success}, message='{response.message}'");
    }
}
