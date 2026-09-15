using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkspaceMapper.Scripts
{
    public class InteractiveRobotDrag : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform robotRoot;
        [SerializeField] WorkspaceTable table;
        [SerializeField] Camera cam;

        [Header("Controls")]
        [SerializeField] Key dragKey = Key.G;
        [SerializeField] bool snapToGrid;
        [SerializeField] float rotateSpeed = 60f;

        void Update()
        {
            Mouse m = Mouse.current;
            Keyboard kb = Keyboard.current;
            if (m == null || kb == null || !robotRoot || !table) return;

            if (kb.leftBracketKey.isPressed) robotRoot.Rotate(0f, -rotateSpeed * Time.deltaTime, 0f, Space.World);
            if (kb.rightBracketKey.isPressed) robotRoot.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

            if (!kb[dragKey].isPressed || !m.leftButton.isPressed) return;
            Camera c = cam ? cam : Camera.main;
            if (!c) return;
            Ray r = c.ScreenPointToRay(m.position.ReadValue());
            Plane plane = new Plane(table.transform.up, table.transform.position);
            if (!plane.Raycast(r, out float d)) return;
            Vector3 local = table.transform.InverseTransformPoint(r.GetPoint(d));
            if (snapToGrid)
            {
                float cM = table.CellMeters;
                local.x = Mathf.Round(local.x / cM) * cM;
                local.z = Mathf.Round(local.z / cM) * cM;
            }
            local.y = 0f;
            robotRoot.position = table.transform.TransformPoint(local);
        }
    }
}