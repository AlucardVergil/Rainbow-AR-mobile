using UnityEngine;

/// <summary>
/// Simple script to rotate an object about an axis
/// </summary>
public class RotateObject : MonoBehaviour
{
    public float SecondsPerRotation = 5.0f;

    public Vector3 Axis = Vector3.up;

    void Update()
    {
        if (SecondsPerRotation == 0.0f)
        {
            return;
        }
        transform.Rotate(Axis, 360.0f * Time.deltaTime / SecondsPerRotation);
    }
}
