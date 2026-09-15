using Sirenix.OdinInspector;
using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    [ExecuteAlways]
    public class RobotPlacer : MonoBehaviour
    {
        public enum LengthUnit { Meters, Centimeters, Millimeters, Inches }

        [Header("References")]
        [Required, SerializeField] Transform robotRoot;
        [Required, SerializeField] WorkspaceTable table;

        [Header("Placement (selected unit, from table centre)")]
        [SerializeField] LengthUnit unit = LengthUnit.Inches;
        [SerializeField, OnValueChanged("PlaceRobot")] float posX;
        [SerializeField, OnValueChanged("PlaceRobot")] float posY;
        [SerializeField, OnValueChanged("PlaceRobot")] float headingDeg;
        [SerializeField, OnValueChanged("PlaceRobot")] bool liveUpdate = true;

        float ToM => unit switch
        {
            LengthUnit.Meters => 1f, LengthUnit.Centimeters => 0.01f,
            LengthUnit.Millimeters => 0.001f, LengthUnit.Inches => 0.0254f, _ => 1f
        };

        [Button(ButtonSizes.Large)]
        public void PlaceRobot()
        {
            if (!liveUpdate || !robotRoot || !table) return;
            Vector3 local = new Vector3(posX * ToM, 0f, posY * ToM);
            robotRoot.position = table.transform.TransformPoint(local);
            robotRoot.rotation = table.transform.rotation * Quaternion.Euler(0f, headingDeg, 0f);
        }
    }
}