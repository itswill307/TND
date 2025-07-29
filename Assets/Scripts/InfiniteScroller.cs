using UnityEngine;
using System.Collections.Generic;

public class InfiniteScroller : MonoBehaviour
{
    [Tooltip("Your N chunk Transforms, initially laid out side‑by‑side along +X.")]
    public List<Transform> chunks;

    [Tooltip("Width of each chunk in world units along X.")]
    public float chunkWidth = 12.4f;

    [Tooltip("The object whose X‑position drives the scroll (e.g. your camera).")]
    public Transform driver;
    
    [Tooltip("Camera's horizontal view distance (buffer for fast panning)")]
    public float cameraViewWidth = 62f;

    // derived values
    float totalWidth;
    float halfRingWidth;
    bool isSetupComplete = false;

    // single ghost copy per chunk
    List<Transform> ghosts = new List<Transform>();

    void Start()
    {
        if (chunks == null || chunks.Count == 0 || driver == null)
            return;

        totalWidth   = chunks.Count * chunkWidth;
        halfRingWidth = totalWidth * 0.5f;

        // 1) Lay out the original chunks in a row centered on driver.x
        float startX = driver.position.x - (totalWidth - chunkWidth) * 0.5f;
        for (int i = 0; i < chunks.Count; i++)
        {
            Vector3 p = chunks[i].position;
            p.x = startX + i * chunkWidth;
            chunks[i].position = p;
        }

        // 2) Create one ghost copy for each chunk
        foreach (var c in chunks)
        {
            var ghost = Instantiate(c.gameObject, c.position + Vector3.right * totalWidth, c.rotation, transform).transform;
            // optional: disable scripts/colliders on ghosts so only visuals remain
            foreach (var comp in ghost.GetComponents<MonoBehaviour>()) comp.enabled = false;
            ghosts.Add(ghost);
        }
        
        isSetupComplete = true;
    }

    void Update()
    {
        if (chunks == null || driver == null || !isSetupComplete) return;
        
        // Additional safety check - ensure all chunks and ghosts exist
        if (chunks.Count == 0 || ghosts.Count != chunks.Count) return;
        
        float dx = driver.position.x;
        
        // Expand boundaries by camera view width for fast panning
        float bufferZone = cameraViewWidth * 0.5f;
        float leftBound  = dx - halfRingWidth - bufferZone;
        float rightBound = dx + halfRingWidth + bufferZone;

        // 3) Wrap chunks when they move past the ring boundary
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var ghost = ghosts[i];
            
            float chunkLeftEdge  = chunk.position.x - (chunkWidth * 0.5f);
            float chunkRightEdge = chunk.position.x + (chunkWidth * 0.5f);

            // if original chunk is entirely to the left of the ring, send it right
            if (chunkRightEdge < leftBound)
            {
                chunk.position += Vector3.right * totalWidth;
            }
            // if original chunk is entirely to the right of the ring, send it left  
            else if (chunkLeftEdge > rightBound)
            {
                chunk.position += Vector3.left * totalWidth;
            }

            // 4) Keep ghost positioned one totalWidth ahead of original
            ghost.position = chunk.position + Vector3.right * totalWidth;
        }
    }
}
