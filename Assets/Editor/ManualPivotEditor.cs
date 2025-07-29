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
    Vector3 newPivotGlobal = Vector3.zero;
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
        EditorGUILayout.LabelField("Current Global Position:", selected.position.ToString("F3"));

        GUILayout.Space(5);
        EditorGUILayout.LabelField("New Pivot (Global Space)", EditorStyles.miniBoldLabel);
        changeX = EditorGUILayout.Toggle("Change X", changeX);
        if (changeX)
            newPivotGlobal.x = EditorGUILayout.FloatField("X", newPivotGlobal.x);
        changeY = EditorGUILayout.Toggle("Change Y", changeY);
        if (changeY)
            newPivotGlobal.y = EditorGUILayout.FloatField("Y", newPivotGlobal.y);
        changeZ = EditorGUILayout.Toggle("Change Z", changeZ);
        if (changeZ)
            newPivotGlobal.z = EditorGUILayout.FloatField("Z", newPivotGlobal.z);

        GUILayout.Space(10);
        if (GUILayout.Button("Apply Pivot Change"))
        {
            ChangePivot(selected, newPivotGlobal, changeX, changeY, changeZ);
        }
    }

    static void ChangePivot(Transform target, Vector3 newPivotGlobal, bool changeX, bool changeY, bool changeZ)
    {
        Undo.RegisterCompleteObjectUndo(target, "Change Pivot");
        
        // Start with current global position
        Vector3 desiredGlobal = target.position;
        
        // Only change the axes that are enabled
        if (changeX) desiredGlobal.x = newPivotGlobal.x;
        if (changeY) desiredGlobal.y = newPivotGlobal.y;
        if (changeZ) desiredGlobal.z = newPivotGlobal.z;

        // Calculate how much to move in world space
        Vector3 worldDelta = desiredGlobal - target.position;

        // Move the parent to the new global position
        target.position += worldDelta;

        // Move all children back by the same amount to keep them in place
        foreach (Transform child in target)
        {
            Undo.RegisterCompleteObjectUndo(child, "Change Pivot");
            child.position -= worldDelta;
        }
    }
}
