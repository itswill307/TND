// ManualPivotEditor.cs
// Place this file in an Editor folder (e.g., Assets/Editor/ManualPivotEditor.cs)
// Usage:
// 1. Select the parent GameObject in the Hierarchy.
// 2. Go to Tools -> Manual Pivot Editor.
// 3. In the window, enable the axis(es) you want to change and enter the new local pivot coordinates.
// 4. Click 'Apply Pivot Change'. The pivot will update without moving child objects in world space.

using UnityEngine;
using UnityEditor;

public class ManualPivotEditor : EditorWindow
{
    Vector3 newPivotLocal = Vector3.zero;
    bool changeX = false, changeY = false, changeZ = false;

    [MenuItem("Tools/Pivot/Manual Pivot Editor")]
    public static void ShowWindow()
    {
        GetWindow<ManualPivotEditor>("Manual Pivot Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("Manual Pivot Editor", EditorStyles.boldLabel);
        Transform selected = Selection.activeTransform;
        if (selected == null)
        {
            EditorGUILayout.HelpBox("Select a GameObject in the Hierarchy to edit its pivot.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Selected Object:", selected.name);

        GUILayout.Space(5);
        EditorGUILayout.LabelField("New Pivot (Local Space)", EditorStyles.miniBoldLabel);
        changeX = EditorGUILayout.Toggle("Change X", changeX);
        if (changeX)
            newPivotLocal.x = EditorGUILayout.FloatField("X", newPivotLocal.x);
        changeY = EditorGUILayout.Toggle("Change Y", changeY);
        if (changeY)
            newPivotLocal.y = EditorGUILayout.FloatField("Y", newPivotLocal.y);
        changeZ = EditorGUILayout.Toggle("Change Z", changeZ);
        if (changeZ)
            newPivotLocal.z = EditorGUILayout.FloatField("Z", newPivotLocal.z);

        GUILayout.Space(10);
        if (GUILayout.Button("Apply Pivot Change"))
        {
            ChangePivot(selected, newPivotLocal, changeX, changeY, changeZ);
        }
    }

    static void ChangePivot(Transform target, Vector3 newPivotLocal, bool changeX, bool changeY, bool changeZ)
    {
        Undo.RegisterCompleteObjectUndo(target, "Change Pivot");
        Vector3 desiredLocal = Vector3.zero;
        if (changeX) desiredLocal.x = newPivotLocal.x;
        if (changeY) desiredLocal.y = newPivotLocal.y;
        if (changeZ) desiredLocal.z = newPivotLocal.z;

        Vector3 worldDelta = target.TransformPoint(desiredLocal) - target.position;

        target.position += worldDelta;

        foreach (Transform child in target)
        {
            Undo.RegisterCompleteObjectUndo(child, "Change Pivot");
            child.position -= worldDelta;
        }
    }
}
