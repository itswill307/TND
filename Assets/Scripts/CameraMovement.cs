// 7/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPanZoom : MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 20f; // Speed of camera panning
    public float mousePanSensitivity = 1f; // Base multiplier for mouse panning sensitivity
    public float velocitySensitivityMultiplier = 0.1f; // How much velocity affects sensitivity
    public float maxVelocityBoost = 3f; // Maximum sensitivity boost from velocity
    public Vector2 panLimitX = new Vector2(-50f, 50f); // Horizontal pan limits
    public Vector2 panLimitZ = new Vector2(-50f, 50f); // Vertical pan limits

    [Header("Zoom Settings")]
    public float zoomSpeed = 500f; // Speed of zooming
    public float minZoom = 5f; // Minimum zoom distance
    public float maxZoom = 50f; // Maximum zoom distance

    [Header("Rotation Settings")]
    public Vector3 topDownRotation = new Vector3(90f, 0f, 0f); // Top-down view rotation (Euler angles)
    public LayerMask groundLayerMask = -1; // What layers to consider as "ground" for orbit point
    public float defaultOrbitDistance = 10f; // Default distance if no ground is hit

    private CameraControls controls; // Reference to the generated Input Actions class
    private Vector2 panInput; // Stores input for panning
    private Vector2 mouseDelta; // Stores mouse movement for middle mouse panning
    private float zoomInput; // Stores input for zooming
    private bool isMiddleMouseHeld = false; // Tracks if the middle mouse button is held
    private bool isRightMouseRotating = false; // Tracks if we're in rotation mode
    private Vector3 rotationCenter; // Point to rotate around
    private float orbitDistance = 10f; // Distance from rotation center

    private Camera mainCamera;

    private void Awake()
    {
        // Initialize the Input Actions
        controls = new CameraControls();

        // Bind the Pan action (for keyboard panning)
        controls.Camera.Pan.performed += ctx => panInput = ctx.ReadValue<Vector2>();
        controls.Camera.Pan.canceled += ctx => panInput = Vector2.zero;

        // Bind the Zoom action (for scroll wheel zooming)
        controls.Camera.Zoom.performed += ctx => zoomInput = ctx.ReadValue<float>();
        controls.Camera.Zoom.canceled += ctx => zoomInput = 0f;

        // Bind the Middle Mouse Drag action
        controls.Camera.MiddleMouseDrag.started += ctx => isMiddleMouseHeld = true;
        controls.Camera.MiddleMouseDrag.canceled += ctx => isMiddleMouseHeld = false;

        // Mouse delta for rotation will be read directly in RotateCamera() method

        mainCamera = Camera.main;
        transform.rotation = Quaternion.Euler(85f, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }

    private void OnEnable()
    {
        controls.Enable(); // Enable the Input Actions
    }

    private void OnDisable()
    {
        controls.Disable(); // Disable the Input Actions
    }

    private void Update()
    {
        // Handle right mouse rotation with direct input (simple and reliable)
        bool rightMousePressed = Mouse.current.rightButton.isPressed;
        
        // Start rotation
        if (rightMousePressed && !isRightMouseRotating)
        {
            StartRotation();
        }
        // Stop rotation
        else if (!rightMousePressed && isRightMouseRotating)
        {
            StopRotation();
        }
        
        // Handle rotation
        if (isRightMouseRotating)
        {
            UpdateRotation();
        }
        
        HandlePanning();
        HandleZooming();
    }
    
    private void StartRotation()
    {
        isRightMouseRotating = true;
        
        // Set rotation center from screen center
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
        {
            rotationCenter = hit.point;
        }
        else
        {
            rotationCenter = transform.position + transform.forward * defaultOrbitDistance;
        }
        
        // Store the distance for returning to top-down view
        orbitDistance = Vector3.Distance(transform.position, rotationCenter);
        
        // Ensure minimum distance
        if (orbitDistance < 1f)
        {
            orbitDistance = defaultOrbitDistance;
        }
    }
    
    private void UpdateRotation()
    {
        // Get mouse movement
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        // Early exit if no mouse movement
        if (mouseDelta.magnitude < 0.01f)
        {
            return;
        }
        
        // Store current position to check rotation limits
        Vector3 currentOffset = transform.position - rotationCenter;
        float currentDistance = currentOffset.magnitude;
        
        // Debug: Show offset and distance calculation
        Debug.Log($"Camera pos: {transform.position}, Rotation center: {rotationCenter}");
        Debug.Log($"Current offset: {currentOffset}, Distance: {currentDistance}");
        Debug.Log($"Offset.y / Distance ratio: {currentOffset.y / currentDistance}");
        
        // Calculate current pitch angle (0 = horizontal, 90 = straight down)
        float currentPitch = Mathf.Asin(Mathf.Clamp(currentOffset.y / currentDistance, -1f, 1f)) * Mathf.Rad2Deg;
        Debug.Log($"Current pitch: {currentPitch} degrees");
        
        // Horizontal rotation (yaw) around world Y-axis - always allow this
        if (Mathf.Abs(mouseDelta.x) > 0.01f)
        {
            transform.RotateAround(rotationCenter, Vector3.up, mouseDelta.x * 0.2f);
            Debug.Log($"Applied horizontal rotation: {mouseDelta.x * 0.2f}");
        }
        
        // Vertical rotation (pitch) with limits to prevent gimbal lock
        if (Mathf.Abs(mouseDelta.y) > 0.01f)
        {
            // Store the original position in case we need to revert
            Vector3 originalPosition = transform.position;
            Quaternion originalRotation = transform.rotation;
            
            // Try the rotation
            Vector3 right = transform.right;
            transform.RotateAround(rotationCenter, right, -mouseDelta.y * 0.2f);
            
            // Check the new angle after rotation
            Vector3 newOffset = transform.position - rotationCenter;
            float newDistance = newOffset.magnitude;
            float newPitch = Mathf.Asin(Mathf.Clamp(newOffset.y / newDistance, -1f, 1f)) * Mathf.Rad2Deg;
            
            Debug.Log($"After rotation - New pitch: {newPitch} degrees");
            
            // If the new angle is unsafe, revert the rotation
            if (newPitch < 20f || newPitch > 85f)
            {
                Debug.Log($"Unsafe angle {newPitch}, reverting rotation");
                transform.position = originalPosition;
                transform.rotation = originalRotation;
            }
            else
            {
                Debug.Log($"Applied vertical rotation - new pitch: {newPitch}");
            }
        }
        
        // Safer LookAt that avoids gimbal lock
        Vector3 directionToCenter = (rotationCenter - transform.position).normalized;
        if (Vector3.Dot(directionToCenter, Vector3.up) < 0.99f) // Avoid looking straight up/down
        {
            transform.LookAt(rotationCenter, Vector3.up);
        }
    }
    
    private void StopRotation()
    {
        isRightMouseRotating = false;
        
        // Return to 85-degree view instead of 90 to stay well under limit
        // Simple approach: start with pure top-down, then add horizontal offset for 85°
        Vector3 pureTopDown = rotationCenter + Vector3.up * orbitDistance;
        
        // Add backward offset to make it 85° instead of 90°
        // This moves the camera backward from pure vertical (tilting back 5 degrees)
        Vector3 smallOffset = Vector3.back * (orbitDistance * 0.087f); // ~5 degree offset for 85°
        Vector3 nearTopDownPosition = pureTopDown + smallOffset;
        
        transform.position = nearTopDownPosition;
        
        // Set rotation to look at the center
        transform.LookAt(rotationCenter, Vector3.up);
    }

    private void HandlePanning()
    {
        // Handle keyboard panning
        Vector3 position = transform.position;
        position.x += panInput.x * panSpeed * Time.deltaTime;
        position.z += panInput.y * panSpeed * Time.deltaTime;

        // Handle middle mouse panning with velocity-based sensitivity
        if (isMiddleMouseHeld)
        {
            mouseDelta = Mouse.current.delta.ReadValue();

            // Calculate mouse velocity (magnitude of delta)
            float mouseVelocity = mouseDelta.magnitude;

            // Calculate dynamic sensitivity based on velocity
            float velocityBoost = Mathf.Clamp(mouseVelocity * velocitySensitivityMultiplier, 1f, maxVelocityBoost);
            float dynamicSensitivity = mousePanSensitivity * velocityBoost;

            // Convert mouse delta to world space movement
            Vector3 worldDelta = Vector3.zero;

            // For perspective camera, calculate world units per pixel at camera distance
            float distance = transform.position.y; // Assuming camera looks down at y=0 plane
            float worldHeight = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float worldUnitsPerPixel = worldHeight / Screen.height;

            // Apply dynamic sensitivity
            worldDelta.x = -mouseDelta.x * worldUnitsPerPixel * dynamicSensitivity;
            worldDelta.z = -mouseDelta.y * worldUnitsPerPixel * dynamicSensitivity;

            position += worldDelta;
        }

        // Clamp the camera position to stay within the pan limits
        position.x = Mathf.Clamp(position.x, panLimitX.x, panLimitX.y);
        position.z = Mathf.Clamp(position.z, panLimitZ.x, panLimitZ.y);

        // Apply the new position to the camera
        transform.position = position;
    }

    private void HandleZooming()
    {
        // Handle zooming with increased speed
        float zoom = mainCamera.orthographic ? mainCamera.orthographicSize : mainCamera.fieldOfView;
        zoom -= zoomInput * zoomSpeed * Time.deltaTime;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = zoom;
        }
        else
        {
            mainCamera.fieldOfView = zoom;
        }
    }
}