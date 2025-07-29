using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 20f; // Speed of camera panning
    public float mousePanSensitivity = 1f; // Base multiplier for mouse panning sensitivity
    public float velocitySensitivityMultiplier = 0.1f; // How much velocity affects sensitivity
    public float maxVelocityBoost = 3f; // Maximum sensitivity boost from velocity
    public Vector2 panLimitZ = new Vector2(-50f, 50f); // Vertical pan limits

    [Header("Zoom Settings")]
    public float zoomSpeed = 500f; // Speed of zooming
    public float minZoom = 5f; // Minimum zoom distance
    public float maxZoom = 60f; // Maximum zoom distance
    
    [Header("View Limits")]
    public float maxViewWidth = 62f; // Maximum view width in world units

    [Header("Rotation Settings")]
    public LayerMask groundLayerMask = -1; // What layers to consider as "ground" for orbit point
    public float defaultOrbitDistance = 10f; // Default distance if no ground is hit
    public float rotationSensitivity = 0.2f; // Speed of camera rotation
    
    // Input System
    private CameraControls controls; // Reference to the generated Input Actions class
    
    // Input Values
    private Vector2 panInput; // Stores input for panning
    private Vector2 mouseDelta; // Stores mouse movement for middle mouse panning and rotation
    private float zoomInput; // Stores input for zooming
    
    // Input States
    private bool isMiddleMouseHeld = false; // Tracks if the middle mouse button is held
    private bool isRightMouseHeld = false; // Tracks if the right mouse button is held
    
    // Rotation State
    private bool isRightMouseRotating = false; // Tracks if we're in rotation mode
    private Vector3 rotationCenter; // Point to rotate around
    private float orbitDistance; // Distance from rotation center
    
    // Camera Reference
    private Camera mainCamera;
    
    // Height calculation cache
    private float cachedAspectRatio;
    private float cachedMaxHeight;

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

        // Bind the Mouse Delta action for rotation and panning
        controls.Camera.MouseDelta.performed += ctx => mouseDelta = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.canceled += ctx => mouseDelta = Vector2.zero;

        // Bind the Middle Mouse Drag action
        controls.Camera.MiddleMouseDrag.started += ctx => isMiddleMouseHeld = true;
        controls.Camera.MiddleMouseDrag.canceled += ctx => isMiddleMouseHeld = false;

        // Bind the Right Mouse Hold action
        controls.Camera.RightMouseHold.started += ctx => isRightMouseHeld = true;
        controls.Camera.RightMouseHold.canceled += ctx => isRightMouseHeld = false;

        mainCamera = Camera.main;

        transform.rotation = Quaternion.Euler(85f, 0f, 0f);
        
        // Initialize height calculation cache
        cachedAspectRatio = mainCamera.aspect;
        cachedMaxHeight = GetMaxHeightForViewWidth(maxViewWidth, 60f);
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
        // Handle right mouse rotation using Input Actions
        // Start rotation
        if (isRightMouseHeld && !isRightMouseRotating)
        {
            StartRotation();
        }
        // Stop rotation
        else if (!isRightMouseHeld && isRightMouseRotating)
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
        LimitCameraHeight();
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
        position.z = Mathf.Clamp(position.z, panLimitZ.x, panLimitZ.y);

        // Apply the new position to the camera
        transform.position = position;
    }

    private void HandleZooming()
    {
        // Handle zooming by adjusting field of view
        float zoom = mainCamera.fieldOfView;
        zoom -= zoomInput * zoomSpeed * Time.deltaTime;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        
        mainCamera.fieldOfView = zoom;
    }

    private void LimitCameraHeight()
    {
        // Only recalculate if aspect ratio has changed
        if (Mathf.Abs(mainCamera.aspect - cachedAspectRatio) > 0.001f)
        {
            cachedAspectRatio = mainCamera.aspect;
            cachedMaxHeight = GetMaxHeightForViewWidth(maxViewWidth, 60f);
        }
        
        // Always set camera height to cached max height
        Vector3 pos = transform.position;
        pos.y = cachedMaxHeight;
        transform.position = pos;
    }

    private float GetMaxHeightForViewWidth(float desiredWidth, float fixedFov)
    {
        // For perspective camera: viewWidth = 2 * height * tan(FOV/2) * aspectRatio
        // Solving for height: height = viewWidth / (2 * tan(FOV/2) * aspectRatio)
        // Using fixed FOV of 60 degrees instead of current camera FOV
        float halfFovRad = fixedFov * 0.5f * Mathf.Deg2Rad;
        float aspectRatio = mainCamera.aspect;
        return desiredWidth / (2f * Mathf.Tan(halfFovRad) * aspectRatio);
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
        // Early exit if no mouse movement
        if (mouseDelta.magnitude < 0.01f)
        {
            return;
        }
        
        // Store current position to check rotation limits
        Vector3 currentOffset = transform.position - rotationCenter;
        float currentDistance = currentOffset.magnitude;
        
        // Horizontal rotation (yaw) around world Y-axis - always allow this
        if (Mathf.Abs(mouseDelta.x) > 0.01f)
        {
            transform.RotateAround(rotationCenter, Vector3.up, mouseDelta.x * rotationSensitivity);
        }
        
        // Vertical rotation (pitch) with limits to prevent gimbal lock
        if (Mathf.Abs(mouseDelta.y) > 0.01f)
        {
            // Store the original position in case we need to revert
            Vector3 originalPosition = transform.position;
            Quaternion originalRotation = transform.rotation;
            
            // Try the rotation
            Vector3 right = transform.right;
            transform.RotateAround(rotationCenter, right, -mouseDelta.y * rotationSensitivity);
            
            // Check the new angle after rotation
            Vector3 newOffset = transform.position - rotationCenter;
            float newDistance = newOffset.magnitude;
            float newPitch = Mathf.Asin(Mathf.Clamp(newOffset.y / newDistance, -1f, 1f)) * Mathf.Rad2Deg;
            
            // If the new angle is unsafe, revert the rotation
            if (newPitch < 20f || newPitch > 85f)
            {
                transform.position = originalPosition;
                transform.rotation = originalRotation;
            }
        }
    }
    
    private void StopRotation()
    {
        isRightMouseRotating = false;
        
        // Calculate position for 85-degree angle facing the rotation center
        // Convert 85 degrees to radians for calculation
        float angleInRadians = 85f * Mathf.Deg2Rad;
        
        // Calculate offset from rotation center for 85-degree view
        // Y component based on sine of angle, horizontal distance based on cosine
        float horizontalDistance = orbitDistance * Mathf.Cos(angleInRadians);
        float verticalHeight = orbitDistance * Mathf.Sin(angleInRadians);
        
        // Position camera at calculated offset (facing south, so offset is on negative Z-axis)
        Vector3 targetPosition = rotationCenter + new Vector3(0f, verticalHeight, -horizontalDistance);
        transform.position = targetPosition;
        
        // Make camera look at the rotation center
        transform.LookAt(rotationCenter, Vector3.up);
    }
}