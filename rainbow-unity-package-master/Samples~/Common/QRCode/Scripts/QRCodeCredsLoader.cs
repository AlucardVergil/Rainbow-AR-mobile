using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using ZXing;

public class QRCodeCredsLoader : MonoBehaviour
{
    [SerializeField]
    private bool logAvailableWebcams;
    [SerializeField]
    private int selectedWebcamIndex;

    private WebCamTexture camTexture;
    private Color32[] cameraColorData;
    private int width, height;
    private Rect screenRect;

    public delegate void CredentialsLoadedDelegate(string login, string password, string platform, string appId, string appSecret );
    public event CredentialsLoadedDelegate CredentialsLoaded;

    private IBarcodeReader barcodeReader = new BarcodeReader
    {
        AutoRotate = false,
        Options = new ZXing.Common.DecodingOptions
        {
            TryHarder = false
        }
    };

    private Result result;
     

    private void Start()
    {       
        LogWebcamDevices();

        cameraColorData = new Color32[width * height];
        screenRect = new Rect(0, 0, Screen.width, Screen.height);
    }

    private void OnEnable()
    {
        PlayWebcamTexture();
    }

    private void OnDisable()
    {
        if (camTexture != null)
        {
            camTexture.Stop();
            camTexture = null;
        }
    } 
    public void StartScanning()
    {
        if( camTexture == null )
        {
            SetupWebcamTexture();
        }
        Debug.Log("Starting to Scan");
        PlayWebcamTexture();        
    }

    public void StopScanning()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // try once every 20 frames to lower performances impact
        if (camTexture != null && camTexture.isPlaying && Time.frameCount % 20 == 19)
        {        
            camTexture.GetPixels32(cameraColorData); 
            result = barcodeReader.Decode(cameraColorData, width, height); 
            if (result != null)
            {
                Dictionary<string, string> decodedValues = null;
                try
                {
                    decodedValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Text);

                    if (decodedValues != null && decodedValues.Count != 0)
                    {
                        CredentialsLoaded?.Invoke(decodedValues["login"], decodedValues["password"], decodedValues["hostId"], decodedValues["appId"], decodedValues["appsecret"]);
                    }
                } 
                catch { }
            }
        }
    }
     

    private void OnGUI()
    {
        if (camTexture == null || !camTexture.isPlaying)
            return;
        // show camera image on screen
        GUI.DrawTexture(screenRect, camTexture, ScaleMode.ScaleToFit);
        
    }

    private void OnDestroy()
    {
        if (camTexture != null)
        {
            camTexture.Stop();
            camTexture = null;
        }
    }

    private void LogWebcamDevices()
    {
        if (logAvailableWebcams)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log(devices[i].name);
            }
        }
    }

    private void SetupWebcamTexture()
    {
        string selectedWebcamDeviceName = WebCamTexture.devices[selectedWebcamIndex].name;
        camTexture = new WebCamTexture(selectedWebcamDeviceName);
        camTexture.requestedHeight = Screen.height;
        camTexture.requestedWidth = Screen.width;
    }

    private void PlayWebcamTexture()
    {
        if (camTexture != null)
        {            
            camTexture.Play();
            width = camTexture.width;
            height = camTexture.height;
        }
    }

    
} 
