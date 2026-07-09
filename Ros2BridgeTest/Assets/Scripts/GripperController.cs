using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperController : MonoBehaviour
{
    [System.Serializable]
    private struct GripperJoint
    {
        public string linkName;
        public float multiplier;
    }

    [Header("Robot")]
    [SerializeField] private Transform robotRoot;

    [Header("Motion")]
    [SerializeField] private float closedAngleRad = 0.8f;
    [SerializeField] private float moveDuration = 0.5f;

    [Header("Drive")]
    [SerializeField] private float stiffness = 3000f;
    [SerializeField] private float damping = 100f;
    [SerializeField] private float forceLimit = 1000f;

    [SerializeField] private GripperJoint[] gripperJoints = new GripperJoint[]
    {
        new GripperJoint { linkName = "robotiq_85_left_knuckle_link",        multiplier =  1f },
        new GripperJoint { linkName = "robotiq_85_right_knuckle_link",       multiplier = -1f },
        new GripperJoint { linkName = "robotiq_85_left_inner_knuckle_link",  multiplier =  1f },
        new GripperJoint { linkName = "robotiq_85_right_inner_knuckle_link", multiplier = -1f },
        new GripperJoint { linkName = "robotiq_85_left_finger_tip_link",     multiplier = -1f },
        new GripperJoint { linkName = "robotiq_85_right_finger_tip_link",    multiplier =  1f },
    };

    private readonly List<(ArticulationBody body, float multiplier)> joints =
        new List<(ArticulationBody, float)>();

    private void Awake()
    {
        ArticulationBody[] bodies = robotRoot.GetComponentsInChildren<ArticulationBody>();
        foreach (GripperJoint gj in gripperJoints)
        {
            foreach (ArticulationBody body in bodies)
            {
                if (body.name != gj.linkName)
                    continue;

                ArticulationDrive drive = body.xDrive;
                drive.stiffness = stiffness;
                drive.damping = damping;
                drive.forceLimit = forceLimit;
                body.xDrive = drive;

                joints.Add((body, gj.multiplier));
                break;
            }
        }
    }

    public IEnumerator SetClosure(float closure)
    {
        Dictionary<ArticulationBody, float> from = new Dictionary<ArticulationBody, float>();
        foreach ((ArticulationBody body, float _) in joints)
            from[body] = body.xDrive.target;

        float start = Time.time;
        while (Time.time - start < moveDuration)
        {
            float t = (Time.time - start) / moveDuration;
            Apply(from, closure, t);
            yield return null;
        }
        Apply(from, closure, 1f);
    }

    private void Apply(Dictionary<ArticulationBody, float> from, float closure, float t)
    {
        foreach ((ArticulationBody body, float multiplier) in joints)
        {
            float goal = multiplier * closure * closedAngleRad * Mathf.Rad2Deg;
            ArticulationDrive drive = body.xDrive;
            drive.target = Mathf.Lerp(from[body], goal, t);
            body.xDrive = drive;
        }
    }
}