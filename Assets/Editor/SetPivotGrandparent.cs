// Place this file in Assets/Editor/SetPivotGrandparent.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class SetPivotGrandparent
{
    [MenuItem("Tools/Pivot/Recenter From Grandparent")]
    private static void SetPivotGrandparentMethod()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Transform parent = go.transform;

            // gather only direct children
            var directChildren = new List<Transform>();
            foreach (Transform child in parent)
                directChildren.Add(child);

            if (directChildren.Count == 0)
            {
                Debug.LogWarning($"‘{go.name}’ has no direct children to center upon.");
                continue;
            }

            // compute average of children local positions
            Vector3 avgLocal = Vector3.zero;
            foreach (var child in directChildren)
                avgLocal += child.localPosition;
            avgLocal /= directChildren.Count;

            // convert to world space
            Vector3 newPivotWorld = parent.TransformPoint(avgLocal);
            Vector3 oldPivotWorld = parent.position;
            Vector3 delta = newPivotWorld - oldPivotWorld;

            // record for undo
            Undo.RecordObject(parent, "Set Pivot Grandparent");
            foreach (var child in directChildren)
                Undo.RecordObject(child, "Set Pivot Grandparent");

            // move parent pivot
            parent.position = newPivotWorld;

            // offset children so they stay in place
            foreach (var child in directChildren)
                child.position -= delta;
        }
    }
}
