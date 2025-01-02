using TMPro;
using UnityEngine;

public class PhoneCameraManager : MonoBehaviour
{
    public Camera CameraVideo;
    public Camera CameraSharing;

    private WebCamTexture webCamTexture;

    public TMP_Text debugText;

    void Start()
    {
        debugText.text = "Start:\n";
        // Check if the device has a camera
        if (WebCamTexture.devices.Length > 0)
        {
            // Get the default camera
            webCamTexture = new WebCamTexture();
            webCamTexture.deviceName = WebCamTexture.devices[0].name;

            for (int i = 0; i < WebCamTexture.devices.Length; i++)
            {
                debugText.text += i + " = " + WebCamTexture.devices[i].name + "\n";
            }



            // Assign the WebCamTexture to a material (e.g., for display on a quad)
            Renderer renderer = CameraVideo.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.mainTexture = webCamTexture;
            }

            // Start the camera
            webCamTexture.Play();

            // Create a RenderTexture for the Unity camera
            RenderTexture renderTexture = new RenderTexture(webCamTexture.width, webCamTexture.height, 24);
            CameraVideo.targetTexture = renderTexture;
            CameraSharing.targetTexture = renderTexture;
        }
        else
        {
            Debug.LogError("No camera detected on this device.");
        }
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
}
