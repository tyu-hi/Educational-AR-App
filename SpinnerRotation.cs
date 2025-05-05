using UnityEngine;

public class SpinnerRotation : MonoBehaviour
{
    // Negative rotationSpeed makes the image turn to the right
    public float rotationSpeed = -150f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}