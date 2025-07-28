using UnityEngine;
using UnityEditor;
using EzySlice;
using System.Linq;
using System.Collections.Generic;

public class CustomXMeshChunkerWindow : EditorWindow
{
    [Tooltip("Comma‑separated list of local X coordinates at which to slice.")]
    private string xCoordsInput = "0, 12.4, 24.8";

    private List<float> slicePositions = new List<float>();

    [MenuItem("Tools/Custom X‑Coordinate Mesh Chunker")]
    public static void ShowWindow() =>
        GetWindow<CustomXMeshChunkerWindow>("X‑Coord Chunker");

    void OnGUI()
    {
        GUILayout.Label("Slice by Custom X‑Coordinates", EditorStyles.boldLabel);

        xCoordsInput = EditorGUILayout.TextField(
            new GUIContent("Slice X Positions",
                           "Enter local X positions (object‑space) separated by commas"),
            xCoordsInput);

        if (GUILayout.Button("Parse & Chunk"))
        {
            if (TryParsePositions(xCoordsInput, out slicePositions))
                ChunkByXCoordinates();
            else
                Debug.LogError(
                  "Invalid input! Use comma‑separated floats, e.g. \"0, 12.4, 24.8\".");
        }
    }

    private bool TryParsePositions(string input, out List<float> positions)
    {
        positions = input
            .Split(new[]{','}, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => float.TryParse(s, out _))
            .Select(float.Parse)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        return positions.Count > 0;
    }

    private void ChunkByXCoordinates()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogWarning("Select a parent GameObject first.");
            return;
        }

        // 1. Gather all mesh‑bearing children
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        var currentPieces = filters.Select(f => f.gameObject).ToList();

        // 2. Compute combined X‑bounds in root‑local space
        var combinedBounds = new Bounds();
        bool first = true;
        foreach (var mf in filters)
        {
            var b = mf.sharedMesh.bounds;
            // transform each of the 8 corners into root‑local
            foreach (var corner in new Vector3[]{
                b.center + new Vector3( b.extents.x,  b.extents.y,  b.extents.z),
                b.center + new Vector3(-b.extents.x,  b.extents.y,  b.extents.z),
                b.center + new Vector3( b.extents.x, -b.extents.y,  b.extents.z),
                b.center + new Vector3(-b.extents.x, -b.extents.y,  b.extents.z),
                b.center + new Vector3( b.extents.x,  b.extents.y, -b.extents.z),
                b.center + new Vector3(-b.extents.x,  b.extents.y, -b.extents.z),
                b.center + new Vector3( b.extents.x, -b.extents.y, -b.extents.z),
                b.center + new Vector3(-b.extents.x, -b.extents.y, -b.extents.z)
            })
            {
                var worldPt = mf.transform.TransformPoint(corner);
                var localPt = root.transform.InverseTransformPoint(worldPt);
                if (first)
                {
                    combinedBounds = new Bounds(localPt, Vector3.zero);
                    first = false;
                }
                else
                {
                    combinedBounds.Encapsulate(localPt);
                }
            }
        }

        float minX = combinedBounds.min.x;
        float maxX = combinedBounds.max.x;

        // Build full list of boundaries: [minX, ...slicePositions..., maxX]
        var boundaries = new List<float> { minX };
        boundaries.AddRange(slicePositions);
        boundaries.Add(maxX);

        // 3. Iteratively slice at each user‑specified X plane
        foreach (float localX in slicePositions)
        {
            Vector3 planePos  = root.transform.TransformPoint(new Vector3(localX, 0, 0));
            Vector3 planeNorm = root.transform.right;

            var nextPieces = new List<GameObject>();
            foreach (var piece in currentPieces)
            {
                // use original material for cap
                var renderer = piece.GetComponent<Renderer>();
                var originalMat = (renderer != null && renderer.sharedMaterials.Length > 0)
                                  ? renderer.sharedMaterials[0]
                                  : null;

                var hull = piece.Slice(planePos, planeNorm, originalMat);
                if (hull != null)
                {
                    var lower = hull.CreateLowerHull(piece, originalMat);
                    var upper = hull.CreateUpperHull(piece, originalMat);

                    // parent new pieces to root temporarily
                    lower.transform.parent = root.transform;
                    upper.transform.parent = root.transform;

                    nextPieces.Add(lower);
                    nextPieces.Add(upper);
                    Object.DestroyImmediate(piece);
                }
                else
                {
                    nextPieces.Add(piece);
                }
            }
            currentPieces = nextPieces;
        }

        // 4. Create one parent GameObject per chunk interval
        var chunkParents = new List<GameObject>();
        for (int i = 0; i < boundaries.Count - 1; i++)
        {
            var chunkParent = new GameObject($"Chunk_{i}");
            chunkParent.transform.parent = root.transform;
            chunkParent.transform.localPosition = Vector3.zero;
            chunkParent.transform.localRotation = Quaternion.identity;
            chunkParent.transform.localScale = Vector3.one;
            chunkParents.Add(chunkParent);
        }

        // 5. Reparent each final piece under its chunk
        foreach (var piece in currentPieces)
        {
            // get mesh center in root‑local X
            var mf = piece.GetComponent<MeshFilter>();
            var b  = mf.sharedMesh.bounds;
            var worldCenter = piece.transform.TransformPoint(b.center);
            float xLocal = root.transform.InverseTransformPoint(worldCenter).x;

            // find which interval it belongs to
            int idx = 0;
            for (int j = 0; j < boundaries.Count - 1; j++)
            {
                if (xLocal >= boundaries[j] && xLocal <= boundaries[j + 1])
                {
                    idx = j;
                    break;
                }
            }

            piece.transform.parent = chunkParents[idx].transform;
        }

        Debug.Log($"Sliced into {currentPieces.Count} pieces and grouped into {chunkParents.Count} chunks.");
    }
}
