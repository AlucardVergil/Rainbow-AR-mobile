using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField]
    private RainbowController rainbow;
    private TMP_Dropdown DropdownVideo;
    private TMP_Dropdown DropdownSharing;
    private List<Camera> cameras;
    private List<string> webCamNames;
    private TMP_Dropdown dropdown;
    private const string USE_CLIP_LABEL = "Use audio clip";
    private AudioSource audioSource;
    public AudioClip audioClip;

    // Start is called before the first frame update
    void Awake()
    {
        if (rainbow == null)
        {
            var rainbows = FindObjectsOfType<RainbowController>();
            if (rainbows.Length == 1)
            {
                rainbow = rainbows[0];
            }
            else
            {
                Debug.LogError("SettingsUI is missing a reference to a RainbowController");
                return;
            }
        }

        DropdownVideo = transform.Find("SelectedVideoCamDevice").GetComponent<TMP_Dropdown>();
        List<TMP_Dropdown.OptionData> options = new();
        DropdownVideo.ClearOptions();
        webCamNames = new List<string>();
        cameras = new List<Camera>();
        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.transform.parent == null)
            {
                cameras.Add(cam);
                options.Add(new(cam.gameObject.name));
            }
        }
        int firstWebCamIndex = options.Count;
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            options.Add(new(device.name));
            webCamNames.Add(device.name);
        }
        DropdownVideo.AddOptions(options);
        DropdownVideo.value = firstWebCamIndex;
        DropdownVideo.onValueChanged.AddListener( delegate { DropdownVideo_onValueChanged(DropdownVideo); });
        
        // initialize with first item
        DropdownVideo_onValueChanged(DropdownVideo);


        DropdownSharing = transform.Find("SelectedSharingCamDevice").GetComponent<TMP_Dropdown>();
        options = new();
        DropdownSharing.ClearOptions();

        foreach (Camera cam in cameras)
        {
            if (cam.transform.parent == null)
            {
                options.Add(new(cam.gameObject.name));
            }
        }

        foreach (string devicename in webCamNames)
        {
            options.Add(new(devicename));           
        }
        DropdownSharing.AddOptions(options);
        DropdownSharing.value = 0;
        DropdownSharing.onValueChanged.AddListener(delegate { DropdownVideo_onValueChanged(DropdownSharing); });

        // initialize with first item
        DropdownVideo_onValueChanged(DropdownSharing);

        dropdown = transform.Find("SelectedAudioDevice").GetComponent<TMP_Dropdown>();
        options = new();

        dropdown.ClearOptions();
        if (audioClip != null)
        {
            options.Add(new(USE_CLIP_LABEL));
        }

        foreach (string microName in Microphone.devices)
        {
            options.Add(new(microName));
        }
        dropdown.AddOptions(options);

        dropdown.onValueChanged.AddListener(delegate
        {
            DropdownAudio_onValueChanged(dropdown);
        });

        // initialize with first item
        DropdownAudio_onValueChanged(dropdown);
    }
    private void DropdownAudio_onValueChanged(TMP_Dropdown sender)
    {
        int value = sender.value;
        string optionText = sender.options[value].text;
        Debug.Log($"Audio micro selected: {value} {optionText}");
        if (audioSource != null)
        {
            Destroy(this.audioSource.gameObject);
        }
        switch (optionText)
        {
            case USE_CLIP_LABEL:
                audioSource = AudioSourceFromClip();
                break;
            default:
                audioSource = AudioSourceFromDevice(optionText);
                break;
        }
        rainbow.AudioSourceToPublish = audioSource;
    }

    private void DropdownVideo_onValueChanged(TMP_Dropdown sender)
    {
        bool isForVideo = (sender == DropdownVideo);

        int value = sender.value;
        string optionText = sender.options[value].text;
        if( isForVideo) { 
            Debug.Log($"Video selected: {value} {optionText}");
        } else
            Debug.Log($"Sharing selected: {value} {optionText}");

        if (sender.value < cameras.Count)
        {
            Camera cam = cameras[sender.value];
            if( !isForVideo)
            {
                rainbow.CameraSharing = cam;
                rainbow.WebCamSharingDevice = null;
            } else
            {
                rainbow.CameraVideo = cam;
                rainbow.WebCamVideoDevice = null;
            }
            return;
        }

        value = sender.value - cameras.Count;
        string deviceName = webCamNames[value];

        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            if( device.name == deviceName)
            {
                if (!isForVideo)
                {
                    rainbow.WebCamSharingDevice = device;
                    rainbow.CameraSharing = null;
                }
                else
                {
                    rainbow.WebCamVideoDevice = device;
                    rainbow.CameraVideo = null;
                }
            }
        }
    }

    AudioSource AudioSourceFromDevice(string deviceName)
    {
        foreach (string microName in Microphone.devices)
        {
            // Debug.Log($"possible Micro: {microName}");
            if (microName == deviceName)
            {
                int maxFreq = 48000;
                AudioClip clip;
                clip = Microphone.Start(microName, true, 1, maxFreq);
                if (clip == null)
                {
                    Debug.LogError(" Microphone.Start failed, clip is null");
                }
                // Microphone.GetDeviceCaps(microName, out int minFreq, out int maxFreq);
                while (!(Microphone.GetPosition(microName) > 0)) { }
                var inputAudioSource = new GameObject("InputAudioSourceDevice").AddComponent<AudioSource>();
                inputAudioSource.clip = clip;
                inputAudioSource.loop = true;
                // inputAudioSource.Play();
                return inputAudioSource;
            }
        }
        return null;
    }
    AudioSource AudioSourceFromClip()
    {
        var inputAudioSource = new GameObject("InputAudioSourceClip").AddComponent<AudioSource>();
        inputAudioSource.clip = audioClip;
        inputAudioSource.loop = true;
        inputAudioSource.volume = 0.3f;
        // inputAudioSource.Play();
        return inputAudioSource;
    }
}
