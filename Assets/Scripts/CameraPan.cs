// 7/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPanZoom : MonoBehaviour
{
    public float panSpeed = 20f; // Speed of camera panning
    public float zoomSpeed = 500f; // Speed of zooming
    public float mousePanSensitivity = 1f; // Base multiplier for mouse panning sensitivity
    public float velocitySensitivityMultiplier = 0.1f; // How much velocity affects sensitivity
    public float maxVelocityBoost = 3f; // Maximum sensitivity boost from velocity
    public float minZoom = 5f; // Minimum zoom distance
    public float maxZoom = 50f; // Maximum zoom distance
    public Vector2 panLimitX = new Vector2(-50f, 50f); // Horizontal pan limits
    public Vector2 panLimitZ = new Vector2(-50f, 50f); // Vertical pan limits

    private CameraControls controls; // Reference to the generated Input Actions class
    private Vector2 panInput; // Stores input for panning
    private Vector2 mouseDelta; // Stores mouse movement for middle mouse panning
    private float zoomInput; // Stores input for zooming
    private bool isMiddleMouseHeld = false; // Tracks if the middle mouse button is held

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

        mainCamera = Camera.main;
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