using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class TouchSteering : MonoBehaviour
{
    [Header("Steering Settings")] [Tooltip("Multiplier for steering sensitivity.")]
    public float steeringSensitivity = 1f;

    [Tooltip("Maximum absolute value for steering.")]
    public float maxSteering = 1f;

    // The current steering value (typically between -maxSteering and maxSteering)
    private float steering = 0f;

    // Current brake value (negative of touch pressure)
    private float brake = 0f;

    // InputActions for touch input
    private InputAction touchActionMove;
    private InputAction touchActionRelease;

    // To track the previous touch position for delta calculation (steering)
    private Vector2 previousTouchPos;

    // Separate flags for steering (left side) and braking (right side)
    private bool isSteering = false;
    public bool isBraking = false;

    // Serialized field for the brake image (assign this in the Inspector)
    [SerializeField] private Image brakeImage;

    private void Awake()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        // Get the PlayerInput component and bind the actions
        PlayerInput playerInput = GetComponent<PlayerInput>();
        touchActionMove = playerInput.actions["SteeringMove"];
        touchActionRelease = playerInput.actions["SteeringMoveFinished"];
        touchActionMove.performed += OnTouchPerformed;
        touchActionRelease.canceled += OnTouchCanceled;
    }

    private void OnEnable()
    {
        touchActionMove.Enable();
        touchActionRelease.Enable();
    }

    private void OnDisable()
    {
        touchActionMove.Disable();
        touchActionRelease.Disable();
    }

    // Called whenever a touch update is received
    public void OnTouchPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Rouch revieved");
        // Read the touch position from the context
        // Touch touch = context.ReadValue<Vector2>();
        // Debug.Log($"exports {Touchscreen.current.touches}");
        // ReadOnlyArray<TouchControl> touches = Touchscreen.current.touches;
        // Debug.Log($"counts {touches.Count}");
        // Debug.Log(touches[0].phase.ReadValue());
        // Debug.Log(touches[0].position.ReadValue());
        // // Vector2Control t = touches[0].position;
        //
        // Debug.Log(touches[1].phase.ReadValue());
        // Debug.Log(touches[1].position.ReadValue());

        // Vector2 touchPos  = touch.position;
        Vector2 touchPos = context.ReadValue<Vector2>();
        
        // Touch touch = context.ReadValue<Touch>();
        // Vector2 touchPos = touch.position;
        
        
        // TouchControl touchControl2 = context.ReadValue<TouchControl>();
        
        
        
        // Vector2 touchPos = touch.position;
        // Debug.Log($"touches {touch.fingerId}");
        
        
        // Check which half of the screen is touched
        if (touchPos.x < Screen.width / 2)
        {
            // Process steering on left side
            if (!isSteering)
            {
                previousTouchPos = touchPos;
                isSteering = true;
            }
            else
            {
                float deltaX = touchPos.x - previousTouchPos.x;
                steering += deltaX * steeringSensitivity * 0.01f;
                steering = Mathf.Clamp(steering, -maxSteering, maxSteering);
                previousTouchPos = touchPos;
                Debug.Log("Steering value: " + steering);
                // Pass the steering value to your vehicle controller as needed
            }
        }
        else
        {
            // Process brake on right side
            // Position and show the brake image at the touch location
            // if (brakeImage.gameObject.activeSelf == false)
            // {
                brakeImage.transform.position = touchPos;
                if (!brakeImage.gameObject.activeSelf)
                    brakeImage.gameObject.SetActive(true);
            // }

            // Retrieve touch pressure; if not available, default to 1.
            float pressure = 1f;

            brake = -pressure;
            isBraking = true;
            Debug.Log("Brake value: " + brake);
            // Apply the brake value to your vehicle controller as needed
        }
    }

    // Called when a touch is released
    public void OnTouchCanceled(InputAction.CallbackContext context)
    {
        Touch touch = context.ReadValue<Touch>(); 
        Vector2 touchPos = touch.position;

        Debug.Log("Touch canceled");
        Debug.Log(touchPos);
        Debug.Log($"touches {touch.fingerId}");
        // Reset steering if it was active
        if (isSteering && touchPos.x < Screen.width / 2)
        {
            isSteering = false;
            steering = 0f;
            Debug.Log("Steering reset to: " + steering);
            // Optionally notify your vehicle controller to reset steering here
        }

        // Reset brake if it was active
        if (isBraking && touchPos.x > Screen.width / 2)
        {
            Debug.Log("here");
            isBraking = false;
            brake = 0f;
            if (brakeImage != null && brakeImage.gameObject.activeSelf)
            {
                brakeImage.gameObject.SetActive(false);
                Debug.Log("--*--");
            }

            Debug.Log("Brake reset to: " + brake);
            // Optionally notify your vehicle controller to reset braking here
        }
    }

    private void Update()
    {
        // You can update your vehicle's behavior here with the current steering and brake values.
        // For example:
        // vehicleController.UpdateSteering(steering);
        // vehicleController.ApplyBrake(brake);
    }
}