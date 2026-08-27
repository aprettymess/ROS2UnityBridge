using UnityEditor;
using UnityEngine;

public class RecursiveMaterialAssigner : EditorWindow
{
    [SerializeField] Material material;
    [SerializeField] bool includeInactive = true;
    [SerializeField] bool onlyIfPink;

    [MenuItem("Tools/Recursive Material Assigner")]
    static void Open()
    {
        GetWindow<RecursiveMaterialAssigner>("Material Assigner");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Assign a material to the selected GameObject and all its children.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        onlyIfPink = EditorGUILayout.Toggle("Only Replace Missing/Pink", onlyIfPink);

        EditorGUILayout.Space();

        GameObject target = Selection.activeGameObject;
        EditorGUILayout.LabelField("Selected", target != null ? target.name : "(nothing selected)");

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(target == null || material == null))
        {
            if (GUILayout.Button("Assign Recursively"))
                Assign(target);
        }
    }

    void Assign(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
        int changed = 0;

        Undo.RecordObjects(renderers, "Assign Material Recursively");

        foreach (Renderer r in renderers)
        {
            if (onlyIfPink && !HasMissingMaterial(r))
                continue;

            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = material;
            r.sharedMaterials = mats;

            EditorUtility.SetDirty(r);
            changed++;
        }

        Debug.Log($"RecursiveMaterialAssigner: assigned '{material.name}' to {changed} renderer(s) under '{root.name}'.");
    }

    bool HasMissingMaterial(Renderer r)
    {
        foreach (Material m in r.sharedMaterials)
            if (m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                return true;
        return false;
    }
}