using UnityEngine;

public class TouchSteering2 : MonoBehaviour
{
    // Adjust this sensitivity to control the steering responsiveness
    public float steeringSensitivity = 0.1f;

    void Update()
    {
        // Process touches if there is at least one on the screen
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // Check if the touch is on the left half of the screen
                if (touch.position.x < Screen.width / 2)
                {
                    // Only process movement when the touch is moving
                    if (touch.phase == TouchPhase.Moved)
                    {
                        // Get the horizontal movement (deltaPosition.x)
                        float horizontalDelta = touch.deltaPosition.x;
                        
                        // Apply steering based on the horizontal delta and sensitivity
                        Steer(horizontalDelta * steeringSensitivity);
                    }
                }
            }
        }
    }

    // A simple example of a steering method.
    // This could be modified to rotate a car or adjust a character's movement.
    void Steer(float amount)
    {
        // Rotate the object around the Y-axis.
        // For a vehicle, you might modify this to suit your steering mechanics.
        Debug.Log($"Amount {amount}");
        transform.Rotate(Vector3.up, amount);
    }
}