using UnityEngine;
using UnityEngine.UI;

public class TouchController3 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Image brakeImage;
    [SerializeField] private Image stearImage;
    [SerializeField] private float maxStearingAngle = 45f;
    [SerializeField] private float maxDeltaRangeLeftTouch = 50f;
    [SerializeField] private float steeringReturnSpeed = 100f ; //degree per second
    private float brake = 0;

    [SerializeField] private int leftTouchId = -1;
    [SerializeField] private int rightTouchId = -1;

    [SerializeField] private bool leftTouchIsActive = true;

    private float horizontalDelta = 0;

    private bool IsControlling = false;

    // Update is called once per frame
    void Update()
    {
        // Process touches if there is at least one on the screen
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (leftTouchIsActive && touch.position.x < Screen.width / 2 && touch.fingerId != rightTouchId)
                {
                    IsControlling = true;
                    leftTouchId = touch.fingerId;
                    //left side
                    // Only process movement when the touch is moving
                    if (touch.phase == TouchPhase.Moved)
                    {
                        // Get the horizontal movement (deltaPosition.x)
                        horizontalDelta += touch.deltaPosition.x;

                        horizontalDelta = Mathf.Clamp(horizontalDelta, -maxDeltaRangeLeftTouch, maxDeltaRangeLeftTouch);

                        float degree = horizontalDelta * maxStearingAngle / maxDeltaRangeLeftTouch;
                        // Apply steering based on the horizontal delta and sensitivity
                        Debug.Log(horizontalDelta);
                        stearImage.transform.localRotation = Quaternion.Euler(0, 0, -degree);
                    }
                }


                // clear the finger
                if (touch.phase == TouchPhase.Ended)
                {
                    if (leftTouchId == touch.fingerId)
                    {
                        leftTouchId = -1;
                        IsControlling = false;
                    }
                }
            }
        }

        if (!IsControlling && stearImage.transform.localEulerAngles.z != 0)
        {
            // todo smoothly rotate until reach zero at Z axis 
            // Rotate the steering wheel image back to identity over time
            stearImage.transform.localRotation = Quaternion.RotateTowards(
                stearImage.transform.localRotation,
                Quaternion.identity,
                steeringReturnSpeed * Time.deltaTime
            );

            // Also ease horizontalDelta back to zero so it lines up next time you touch
            horizontalDelta = Mathf.MoveTowards(
                horizontalDelta,
                0f,
                (maxDeltaRangeLeftTouch / maxStearingAngle) * steeringReturnSpeed * Time.deltaTime
            );
        }
    }
}