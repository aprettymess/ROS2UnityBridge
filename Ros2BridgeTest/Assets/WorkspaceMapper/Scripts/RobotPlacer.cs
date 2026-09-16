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

        [Header("Placement (selected unit, from grid corner 0,0)")]
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

        public float PosX { get => posX; set { posX = value; PlaceRobot(); } }
        public float PosY { get => posY; set { posY = value; PlaceRobot(); } }
        public float HeadingDeg { get => headingDeg; set { headingDeg = value; PlaceRobot(); } }
        public void SetPlacement(float x, float y, float heading) { posX = x; posY = y; headingDeg = heading; PlaceRobot(); }
        public float TableLenU => table ? table.LengthMeters / ToM : 0f;
        public float TableWidU => table ? table.WidthMeters / ToM : 0f;
        public void PlaceAtFraction(float fx, float fy, float heading)
        {
            if (!table) return;
            posX = fx * TableLenU;
            posY = fy * TableWidU;
            headingDeg = heading;
            PlaceRobot();
        }

        [Button(ButtonSizes.Large)]
        public void PlaceRobot()
        {
            if (!liveUpdate || !robotRoot || !table) return;
            Vector3 local = new Vector3(-table.LengthMeters * 0.5f + posX * ToM, 0f, -table.WidthMeters * 0.5f + posY * ToM);
            Vector3 world = table.transform.TransformPoint(local);
            Quaternion rot = table.transform.rotation * Quaternion.Euler(0f, headingDeg, 0f);
            RobotMover.Move(robotRoot, world, rot);
        }
    }
}