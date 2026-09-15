using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    [RequireComponent(typeof(Camera))]
    public class OrientationGizmo : MonoBehaviour
    {
        [SerializeField] WorkspaceCamera controller;
        [SerializeField] float radius = 34f;
        [SerializeField] Vector2 corner = new Vector2(64, 64);

        Camera _cam;
        static readonly Vector3[] Axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        static readonly string[] Names = { "X", "-X", "Y", "-Y", "Z", "-Z" };

        void Awake() { _cam = GetComponent<Camera>(); if (!controller) controller = GetComponent<WorkspaceCamera>(); }

        void OnGUI()
        {
            Vector2 c = new Vector2(corner.x, corner.y);
            Quaternion inv = Quaternion.Inverse(transform.rotation);
            for (int i = 0; i < 6; i++)
            {
                Vector3 v = inv * Axes[i];
                Vector2 p = c + new Vector2(v.x, -v.y) * radius;
                float size = Mathf.Lerp(10f, 20f, (v.z + 1f) * 0.5f);
                Rect r = new Rect(p.x - size / 2f, p.y - size / 2f, size, size);
                Color col = i < 2 ? new Color(0.9f, 0.3f, 0.3f) : i < 4 ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.4f, 0.6f, 0.95f);
                GUI.color = col;
                if (GUI.Button(r, Names[i]) && controller) controller.LookFromDirection(Axes[i]);
            }
            GUI.color = Color.white;
        }
    }
}