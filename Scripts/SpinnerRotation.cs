using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    public float rotationSpeed = -150f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}