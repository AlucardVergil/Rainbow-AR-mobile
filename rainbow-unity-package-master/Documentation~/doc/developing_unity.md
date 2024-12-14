# Developping with the **Unity Rainbow SDK WebRTC**

This page presents specificities of using Rainbow WebRTC in unity. 


## Threading

When running in Unity, most events and callbacks of the **Rainbow SDK** will not be triggered in the Unity thread.

As a result if you to trigger some code which modifies or use the unity objects as a result from this events, 
you will **must** use some mechanism to delegate the execution of this code to the Unity thread.

The package contains a (**UnityExecutor**)[] class to help with it.
Feel free to use it or some other third party mechanism of your choice.

Example: running some code in the unity Thread

```

// This code is bound to a rainbow event, so it won't be triggered in the Unity thread
private void RainbowContacts_ContactAggregatedPresenceChanged(object sender, PresenceEventArgs e)
    {
        if (e.Presence != null && rbApplication.GetContacts().GetCurrentContactJid().StartsWith(e.Presence.BasicNodeJid))
        {
            UnityExecutor.Execute( () => {
                // this call will be run in the Unity Thread
                myPresenceLabel.text = $"my Presence is now {e.Presence.PresenceLevel}";
            });
        }
    }
```


Also, some of the SDK method are blocking calls, and some of them even defer treatments to the Unity thread, so as a rule of thumb you **musn't**
call the Rainbow SDK from the unity thread. 

The package comes with a (**RainbowThreadExecutor** class)[] to help with it. 

Feel free to use it or some other third party mechanism of your choice.

Example: Calling a SDk method from the Unity thread

```
    class SomeClass : MonoBehaviour {
        private RainbowThreadExecutor executor;
        ...

        private void Awake() {
            executor = new RainbowThreadExecutor();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Z)) {
                // Call a contact 
                executor.Execute(()=>{
                    // This will not block the unity thread
                    webRTCCommunications.MakeCall(... etc... )
                });
            }
        }
    }

```

## Initializing the **Unity Rainbow SDK WebRTC package**

For the **Unity Rainbow SDK WebRTC** to work, some intializations must be performed from the unity thread.

+ Initialization of Unity.WebRTC dependency

If your application is sending or receiving video, the [**Unity.WebRTC.Update**](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/api/Unity.WebRTC.WebRTC.html) Coroutine needs to be started. 

+ Initialization of the UnityExecutor class

Make sure you call the static method **Rainbow.WebRTC.Unity.UnityExecutor.Initialize()** from the unity thread.

```
    private void Awake() {
        UnityThreadExecutor.Initialize();
        StartCoRoutine(Unity.WebRTC.Update());
    }
```

## Instantiating a WebRTCCommunications object

The Rainbow.WebRTC sdk is a generic package which to work with multiple implementations of webrtc stacks and multiple implementations media libraries etc, and manipulates interfaces.

The entry class to use its feature is WebRTCCommunications, but to get an instance of this class, you need to provide him with an instance of a class implementing 
IWebRTCFactory. In the **Unity Rainbow SDK WebRTC** package this service is provided by *UnityWebRTCFactory*.

To instantiate a WebRTCCommunications object use an instance of Rainbow.WebRTC.Unity.UnityWebRTCFactory

Example: 

```
    // ... you already have a Rainbow.Application in myApplication

    var unityWebRTCFactory = new UnityWebRTCFactory();
    webRTCCommunication WebRTCCommunications.GetOrCreateInstance(myApplication, unityWebRTCFactory);

    // you're good to use webRTCCommunication to make calls etc..

```

## Creating a IAudioStreamTrack

To make a call or join a conference, you need an object implementing IAudioStreamTrack which will produce the audio sent in the call.

**UnityWebRTCFacory** lets you create an AudioMediaDevice from an [**AudioSource**](https://docs.unity3d.com/ScriptReference/AudioSource.html), and then retrieve an IAudioStreamTrack.

Example:

```
    AudioMediaDevice myDevice = CreateAudioMediaDevice(myAudioSource);
    IAudioStreamTrack? myAudioTrack = CreateAudioTrack(myDevice);

    // Then call joinConference from the Rainbow thread
    RainbowExecutor.Execute(() =>
    {
        rbWebRTCCommunications.JoinConference(Id, myAudioTrack, cb =>
        {
            currentCallId = cb.Data;
        });
    });
```

Such an [**AudioSource**](https://docs.unity3d.com/ScriptReference/AudioSource.html) could be gotten from enumerating available Microphones for example, or to play an Audio Clip.

For example to select the first microphone device available as an [**AudioSource**](https://docs.unity3d.com/ScriptReference/AudioSource.html):

```
    AudioSource myMicro;
    foreach (string microName in Microphone.devices)
    {             
            int maxFreq = 48000;
            AudioClip clip;
            clip = Microphone.Start(microName, true, 1, maxFreq);
            if (clip == null)
            {
                Debug.LogError(" Microphone.Start failed, clip is null");
                break;
            }

            while (!(Microphone.GetPosition(microName) > 0)) { }
            var myMicro = new GameObject().AddComponent<AudioSource>();
            myMicro.clip = clip;
            myMicro.loop = true;
            break;
    }

```
## Creating a IVideoStreamTrack

To publish a video stream, [**WebRTCCommunications.AddVideo**](https://developers.openrainbow.com/doc/sdk/csharp/webrtc/sts/api/Rainbow.WebRTC.WebRTCCommunications#Rainbow.WebRTC.WebRTCCommunications.AddVideo(String-_Rainbow.Medias.IMedia)) and [**WebRTCCommunications.AddSharing**](https://developers.openrainbow.com/doc/sdk/csharp/webrtc/sts/api/Rainbow.WebRTC.WebRTCCommunications#Rainbow.WebRTC.WebRTCCommunications.AddSharing(String-_Rainbow.Medias.IMedia)) methods require a IVideoStreamTrack.

**UnityWebRTC** lets you create an VideoMediaDevice either from a [**Camera**](https://docs.unity3d.com/ScriptReference/Camera.html) or from a [**WebCamDevice**](https://docs.unity3d.com/ScriptReference/WebCamDevice.html), and then retrieve an IAudioStreamTrack.

Example - publish your webcam in the video Channel

```
    WebCamDevice myWebCam = WebCamTexture.devices.First();
    WebCamMediaDevice myWebcamMediaDevice = unityWebRTCFactory.CreateWebCamDevice(myWebCam, 800, 450 );
    IVideoStreamTrack? myVideoTrack = unityWebRTCFactory.CreateVideoTrack(myWebcamMediaDevice);

    myWebRTCCommunications.AddVideo( callId, VideoTrack)
```


Example - publish a [**Unity Camera**](https://docs.unity3d.com/ScriptReference/Camera.html)  in the sharing Channel

```
    CamMediaDevice myCamMediaDevice = unityWebRTCFactory.CreateCameraDevice(camera, 1280, 960 );
    IVideoStreamTrack? myVideoTrack = unityWebRTCFactory.CreateVideoTrack(myCamMediaDevice);

    myWebRTCCommunications.AddSharing( callId, VideoTrack)
```

## Playing incoming audio track

When a call is established if SubscribeToMediaPublications have been called, WebRTCCommunications sends an OnTrack event passing a MediaStreamTrackDescriptor.
This object contains a MediaStreamTrack which can be bound to an [**AudioSource**](https://docs.unity3d.com/ScriptReference/AudioSource.html) to play the track in Unity.

Example
```
    private void WebRTCCommunications_OnTrack(string callId, MediaStreamTrackDescriptor mediaStreamTrackDescriptor) {
        IMediaStreamTrack mediaStreamTrack = mediaStreamTrackDescriptor.MediaStreamTrack;
        
        if (mediaStreamTrack is IAudioStreamTrack audioStreamTrack)
        {
            unityWebRTCFactory.OutputAudio(audioStreamTrack, outputAudioSource);
        }
    }
```

## Displaying incoming video

When a remote participant of an establish call starts his sharing or camera, WebRTCCommunications will send a [**MediaPublicationUpdated**](https://developers.openrainbow.com/doc/sdk/csharp/webrtc/sts/api/Rainbow.WebRTC.WebRTCCommunications#Rainbow.WebRTC.WebRTCCommunications.OnMediaPublicationUpdated) event. 

If the Application calls [**SubscribeToMediaPublication**](https://developers.openrainbow.com/doc/sdk/csharp/webrtc/sts/api/Rainbow.WebRTC.WebRTCCommunications#Rainbow.WebRTC.WebRTCCommunications.SubscribeToMediaPublication(Rainbow.Model.MediaPublication-_Action-Rainbow.SdkResult-Boolean--)), the sdk will then send an OnTrack event passing a MediaStreamTrackDescriptor containing the video Track. 

This videoTrack can be rendered in a RawImage using a RemoteVideoDisplay object.

Example 
```
 private void WebRTCCommunications_OnTrack(string callId, MediaStreamTrackDescriptor mediaStreamTrackDescriptor) {
        IMediaStreamTrack mediaStreamTrack = mediaStreamTrackDescriptor.MediaStreamTrack;
        
        if (mediaStreamTrack is VideoStreamTrack videoStreamTrack)
        {
            RemoteVideoDisplay remoteVideoDisplay = new RemoteVideoDisplay();
            remoteVideoDisplay.Track = videoStreamTrack;
            remoteVideoDisplay.image = myRawImage;
        }
    }
    
```


| |
| ----------- |  
|[Back to `Index`](../index.md)|
|[Back to `Install`](install.md)|
|[Back to `Sample`](sample.md)|
|[Back to `Develop with Rainbow WebRTC`](developing_general.md)|
