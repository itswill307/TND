// Save this as Assets/Editor/LocalRecenterPivot.cs

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class RecenterParentPivot
{
    [MenuItem("Tools/Pivot/Recenter From Parent")]
    static void RecenterLocal()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Transform parent = go.transform;
            var renderers = parent.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[{go.name}] has no renderers—skipping.");
                continue;
            }

            // 1) Build a Bounds in *local* space
            Bounds localBounds = new Bounds();
            bool firstCorner = true;

            foreach (var r in renderers)
            {
                // get the 8 corners of the world-space AABB
                var wb = r.bounds;
                Vector3 min = wb.min, max = wb.max;
                Vector3[] corners = {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z)
                };

                // transform each corner into the parent’s local space
                foreach (var c in corners)
                {
                    Vector3 localCorner = parent.InverseTransformPoint(c);
                    if (firstCorner)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        firstCorner = false;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            // 2) Compute where that local‐center is in world space
            Vector3 localCenter = localBounds.center;
            Vector3 worldCenter = parent.TransformPoint(localCenter);
            Vector3 delta = worldCenter - parent.position;

            // 3) Record Undo for parent + all descendants
            Undo.RecordObject(parent, "Recenter Pivot Locally");
            foreach (var t in parent.GetComponentsInChildren<Transform>())
                Undo.RecordObject(t, "Recenter Pivot Locally");

            // 4) Move the parent pivot, then offset every child so nothing shifts visually
            parent.position = worldCenter;
            foreach (var t in parent.GetComponentsInChildren<Transform>())
                if (t != parent)
                    t.position -= delta;

            Debug.Log($"Recentered pivot of “{go.name}” to local center {localCenter:F3} (world {worldCenter:F3}).");
        }
    }

    [MenuItem("Tools/Pivot/Recenter To Children Local", true)]
    static bool Validate() => Selection.gameObjects.Length > 0;
}
