using UnityEditor;
using UnityEngine;

public class PrintBounds
{
    [MenuItem("Tools/Print Selected Bounds")]
    static void Print()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.Log("Nothing selected."); return; }

        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { Debug.Log("No renderers under selection."); return; }

        Bounds b = rends[0].bounds;
        foreach (Renderer r in rends) b.Encapsulate(r.bounds);

        Debug.Log($"{go.name} world bounds:\n" +
                  $"  center = {b.center}\n" +
                  $"  size   = {b.size}\n" +
                  $"  min Y  = {b.min.y:F4}\n" +
                  $"  max Y (TOP SURFACE) = {b.max.y:F4}");
    }
}