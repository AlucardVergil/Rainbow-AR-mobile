using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        float t = Time.deltaTime;
        transform.Rotate(100 * t, 200 * t, 300 * t);        

    }
}
