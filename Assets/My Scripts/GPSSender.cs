using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;

public class GPSSender : MonoBehaviour
{
    private UdpClient udpClient;
    private const int port = 11000; // Use the same port as your listener
    [SerializeField] private TMP_InputField ipInputField; // Input field for IP address
    private string serverIp = ""; // HoloLens IP address

    public TMP_Text debugText;

    void Start()
    {
        udpClient = new UdpClient();

        // Assign a listener to detect changes in the input field
        ipInputField.onEndEdit.AddListener(UpdateServerIp);

        InvokeRepeating("SendGPSData", 1.0f, 1.0f); // Send GPS data every second

        
        // Start the location service
        if (!Input.location.isEnabledByUser)
        {
            debugText.text = "Location services are not enabled by the user.";
            return;
        }
        Input.location.Start(1f, 1f); // Start location service with desired accuracy and update frequency
    }

    void UpdateServerIp(string newIp)
    {
        serverIp = newIp;
        debugText.text = $"Server IP updated to: {serverIp}";
    }

    void SendGPSData()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            string gpsData = $"{Input.location.lastData.latitude},{Input.location.lastData.longitude}";
            byte[] data = Encoding.UTF8.GetBytes(gpsData);

            debugText.text = $"Current Location: Latitude: {Input.location.lastData.latitude} \nLongitude: {Input.location.lastData.longitude}";

            try
            {
                udpClient.Send(data, data.Length, serverIp, port);
                debugText.text += $"\nSent GPS data: {gpsData}";
            }
            catch (Exception e)
            {
                debugText.text += $"\nError sending data: {e.Message}";
            }
        }
        else if (Input.location.status == LocationServiceStatus.Stopped)
        {
            debugText.text += "\nLocation services are stopped.";
        }
        else if (Input.location.status == LocationServiceStatus.Failed)
        {
            debugText.text += "\nLocation services failed to start.";
        }
    }


    void OnApplicationQuit()
    {
        Input.location.Stop();
        udpClient.Close();
    }
}
