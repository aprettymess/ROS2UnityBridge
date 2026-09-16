using RuntimeSceneGizmo;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkspaceMapper.Scripts
{
    // Put this on the same GameObject as the SceneGizmoRenderer (the gizmo UI image).
    public class GizmoCameraLink : MonoBehaviour, IDragHandler, IScrollHandler
    {
        [SerializeField] SceneGizmoRenderer gizmo;
        [SerializeField] WorkspaceCamera cam;
        [SerializeField] bool dragOrbits = true;
        [SerializeField] bool scrollZooms = true;

        void Awake()
        {
            if (!gizmo) gizmo = GetComponent<SceneGizmoRenderer>();
            if (!cam) cam = FindFirstObjectByType<WorkspaceCamera>();
        }

        void Start()
        {
            if (cam && gizmo) gizmo.ReferenceTransform = cam.transform;
            if (gizmo) gizmo.OnComponentClicked.AddListener(OnGizmoClicked);
        }

        void OnDestroy()
        {
            if (gizmo) gizmo.OnComponentClicked.RemoveListener(OnGizmoClicked);
        }

        void OnGizmoClicked(GizmoComponent c)
        {
            if (!cam) return;
            switch (c)
            {
                case GizmoComponent.XPositive: cam.LookFromDirection(Vector3.right); break;
                case GizmoComponent.XNegative: cam.LookFromDirection(Vector3.left); break;
                case GizmoComponent.YPositive: cam.LookFromDirection(Vector3.up); break;
                case GizmoComponent.YNegative: cam.LookFromDirection(Vector3.down); break;
                case GizmoComponent.ZPositive: cam.LookFromDirection(Vector3.forward); break;
                case GizmoComponent.ZNegative: cam.LookFromDirection(Vector3.back); break;
                case GizmoComponent.Center: cam.ToggleProjection(); break;
            }
        }

        public void OnDrag(PointerEventData e)
        {
            if (!cam || !dragOrbits) return;
            if (e.button == PointerEventData.InputButton.Right) cam.PanBy(e.delta.x, e.delta.y);
            else cam.OrbitBy(e.delta.x, e.delta.y);
        }

        public void OnScroll(PointerEventData e)
        {
            if (cam && scrollZooms) cam.ZoomBy(e.scrollDelta.y);
        }
    }
}