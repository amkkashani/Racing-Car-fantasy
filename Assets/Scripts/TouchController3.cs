using UnityEngine;
using UnityEngine.UI;

public class TouchController3 : MonoBehaviour
{
    [SerializeField] private Image brakeImage;
    [SerializeField] private Image stearImage;
    [SerializeField] private float maxStearingAngle = 45f;
    [SerializeField] private float maxDeltaRangeLeftTouch = 50f;
    [SerializeField] private float steeringReturnSpeed = 100f; // degrees per second

    private float horizontalDelta = 0f;
    private bool IsControlling = false;

    // Touch tracking
    private int leftTouchId = -1;
    [SerializeField] private bool leftTouchIsActive = true;

    // Mouse tracking
    private bool isMouseControlling = false;
    private Vector2 lastMousePos;

    void Update()
    {
        HandleTouchInput();
        HandleMouseInput();
        ReturnSteeringToCenter();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        foreach (Touch touch in Input.touches)
        {
            // Start or continue steering on left half
            if (leftTouchIsActive
                && touch.position.x < Screen.width / 2
                && touch.fingerId != leftTouchId)
            {
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
                {
                    IsControlling = true;
                    leftTouchId = touch.fingerId;
                }
            }

            if (touch.fingerId == leftTouchId && touch.phase == TouchPhase.Moved)
            {
                ApplyHorizontalDelta(touch.deltaPosition.x);
            }

            // End touch
            if (touch.fingerId == leftTouchId && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                ResetControl();
            }
        }
    }

    private void HandleMouseInput()
    {
        // Only when no touch is active
        if (Input.touchCount > 0) return;

        Vector2 mousePos = Input.mousePosition;

        // Start mouse control
        if (Input.GetMouseButtonDown(0) && mousePos.x < Screen.width / 2)
        {
            isMouseControlling = true;
            IsControlling = true;
            lastMousePos = mousePos;
        }

        // Continue dragging
        if (isMouseControlling && Input.GetMouseButton(0))
        {
            float deltaX = mousePos.x - lastMousePos.x;
            ApplyHorizontalDelta(deltaX);
            lastMousePos = mousePos;
        }

        // Release mouse
        if (isMouseControlling && Input.GetMouseButtonUp(0))
        {
            ResetControl();
        }
    }

    private void ApplyHorizontalDelta(float deltaX)
    {
        horizontalDelta += deltaX;
        horizontalDelta = Mathf.Clamp(horizontalDelta, -maxDeltaRangeLeftTouch, maxDeltaRangeLeftTouch);

        float angle = horizontalDelta * maxStearingAngle / maxDeltaRangeLeftTouch;
        stearImage.transform.localRotation = Quaternion.Euler(0, 0, -angle);
    }

    private void ReturnSteeringToCenter()
    {
        if (IsControlling) return;

        // Smoothly rotate back to zero
        stearImage.transform.localRotation = Quaternion.RotateTowards(
            stearImage.transform.localRotation,
            Quaternion.identity,
            steeringReturnSpeed * Time.deltaTime
        );

        // Ease horizontalDelta back to zero as well
        horizontalDelta = Mathf.MoveTowards(
            horizontalDelta,
            0f,
            (maxDeltaRangeLeftTouch / maxStearingAngle) * steeringReturnSpeed * Time.deltaTime
        );
    }

    private void ResetControl()
    {
        leftTouchId = -1;
        isMouseControlling = false;
        IsControlling = false;
    }
}
