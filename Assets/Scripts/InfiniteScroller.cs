using UnityEngine;

public class InfiniteScroller : MonoBehaviour
{
    [Tooltip("Drag your 5 chunk Transforms here, in left‑to‑right order")]
    public Transform[] chunks;

    [Tooltip("Width of each chunk in world units")]
    public float chunkWidth = 12.4f;

    private int leftIndex, rightIndex;
    private Transform cam;

    void Start()
    {
        if (chunks.Length == 0)
        {
            Debug.LogError("No chunks assigned!");
            enabled = false;
            return;
        }

        cam = Camera.main.transform;

        // Debug: Show all chunk positions
        Debug.Log("=== CHUNK POSITIONS DEBUG ===");
        for (int i = 0; i < chunks.Length; i++)
        {
            Debug.Log($"Chunk {i} ({chunks[i].name}): Local=({chunks[i].localPosition.x:F2}, {chunks[i].localPosition.y:F2}, {chunks[i].localPosition.z:F2}), World=({chunks[i].position.x:F2}, {chunks[i].position.y:F2}, {chunks[i].position.z:F2})");
        }
        Debug.Log($"Parent object position: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");

        leftIndex  = 0;
        rightIndex = chunks.Length - 1;
        
        // Debug.Log($"Initialized chunks. Left: {chunks[leftIndex].name} at X={chunks[leftIndex].position.x}, Right: {chunks[rightIndex].name} at X={chunks[rightIndex].position.x}");
    }

    void Update()
    {
        float camX = cam.position.x;

        // If camera has moved far enough right, recycle the leftmost chunk to the right
        if (camX > chunks[rightIndex].position.x - chunkWidth * 0.5f)
            RepositionLeftToRight();

        // If camera has moved far enough left, recycle the rightmost chunk to the left
        if (camX < chunks[leftIndex].position.x + chunkWidth * 0.5f)
            RepositionRightToLeft();
    }

    void RepositionLeftToRight()
    {
        // grab the leftmost chunk…
        Transform t = chunks[leftIndex];
        // …and move it exactly one chunkWidth to the right of the current rightmost
        Vector3 newPos = t.position;
        newPos.x = chunks[rightIndex].position.x + chunkWidth;
        t.position = newPos;

        // Debug.Log($"Moved {t.name} from left to right. New position: X={newPos.x}");

        // update our indices
        rightIndex = leftIndex;
        leftIndex  = (leftIndex + 1) % chunks.Length;
        
        // Debug.Log($"New indices - Left: {leftIndex} ({chunks[leftIndex].name}), Right: {rightIndex} ({chunks[rightIndex].name})");
    }

    void RepositionRightToLeft()
    {
        Transform t = chunks[rightIndex];
        Vector3 newPos = t.position;
        newPos.x = chunks[leftIndex].position.x - chunkWidth;
        t.position = newPos;

        // Debug.Log($"Moved {t.name} from right to left. New position: X={newPos.x}");

        leftIndex  = rightIndex;
        rightIndex = (rightIndex - 1 + chunks.Length) % chunks.Length;
        
        // Debug.Log($"New indices - Left: {leftIndex} ({chunks[leftIndex].name}), Right: {rightIndex} ({chunks[rightIndex].name})");
    }
}
