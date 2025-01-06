
using System.Collections.Generic;
using UnityEngine;
using Rainbow.Model;
using Rainbow.WebRTC;
using Rainbow;
using Rainbow.Events;
using System;
using Unity.WebRTC;
using Rainbow.WebRTC.Unity;
using Rainbow.WebRTC.Abstractions;
using static Rainbow.Model.Call;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Cortex
{

    [SelectionBase]
    public class RainbowInterface : MonoBehaviour
    {

        [Header("Audio/Video settings")]
        [Tooltip("Specify the Unity camera to when publishing video")]
        public Camera CameraVideo;       /* it is the camera we will publish in rainbow's video media  */

        [Tooltip("Specify the Unity camera to when publishing sharing")]
        public Camera CameraSharing;    /* it is the camera we will publish in webrtc's sharing media */

        [Tooltip("Audio source on which the remote audio will be played")]
        public AudioSource outputAudioSource; /* Audio we use to play received audio */

        [Tooltip("Resolution to use when publishing video: width")]
        public int PublishVideoWidth = 800;
        [Tooltip("Resolution to use when publishing video: height")]
        public int PublishVideoHeight = 600;

        [Tooltip("Resolution to use when publishing sharing: width")]
        public int PublishSharingWidth = 800;
        [Tooltip("Resolution to use when publishing sharing: height")]
        public int PublishSharingHeight = 600;

        public bool IsLocalVideoFlipped = false;
        public bool IsLocalSharingFlipped = false;
        public AvatarCache AvatarCache = null;

        [Header("Remote Video streams in conference")]
        [Tooltip("Ignore remote videos published by a regular rainbow client in conference")]
        public bool IgnoreNonUnityVideos = false;

        [Tooltip("Ignore remote sharing published by a regular rainbow client in conference")]
        public bool IgnoreNonUnitySharing = false;
        [Header("Deprecated")]

        [Tooltip("Handle up to two remote videoTracks - deprecated")]
        public bool HandleRemoteVideoStreams = false;

        private RemoteVideoDisplay SharingRemoteVideoDisplay;
        private RemoteVideoDisplay VideoRemoteVideoDisplay;

        [Tooltip("Destination RawImage for remote video.")]
        public RawImage VideoDisplay;   /* We display remote video media in this RawImage */

        [Tooltip("Destination RawImage for remote sharing.")]
        public RawImage SharingDisplay; /* We display remote sharing media in this RawImage */

        public MediaStreamTrackCache RemoteTracksCache;

        private Dictionary<string, MonoBehaviour> Services = new Dictionary<string, MonoBehaviour>();
        public void RegisterService(string name, MonoBehaviour service)
        {
            Services[name] = service;
        }

        public T GetService<T>(string name) where T : MonoBehaviour
        {
            return (T)Services[name];
        }

        [Header("Misc")]
        public bool NoKeyboard = true;

        [Header("FrameRate and bitrate")]
        [Tooltip("Set Max Frame rate")]
        public bool SetMaxFramerate = false;
        [Tooltip("Max Frame rate")]
        public uint MaxFramerate = 30;
        [Tooltip("Set Max Bitrate")]
        public bool SetMaxBitrate = false;
        [Tooltip("Max Bitrate")]
        public uint MaxBitrate = 2000000;
        [Tooltip("Set Min Bitrate")]
        public bool SetMinBitrate = false;
        [Tooltip("Min Bitrate")]
        public uint MinBitrate = 1000000;
        [Tooltip("Set Scale Resolution Down By")]
        public bool SetScaleResolutionDownBy = false;
        [Tooltip("Scale Resolution Down By")]
        public double ScaleResolutionDownBy = 1;

        [Header("Logging")]
        [Tooltip("Collect logs from the SDK")]
        public bool LogRainbow = false;
        [Tooltip("Remove stack trace from logs")]
        public bool HideStackInLog = false;
        [Tooltip("Log successfull Rest requests")]
        public bool LogRestSuccess = false;
        [Tooltip("Log Failed Rest requests")]
        public bool LogRestError = true;

        public delegate void LocalVideoStreamUpdatedDelegate(bool isSharing, IMediaStreamTrack videoTrack);
        public event LocalVideoStreamUpdatedDelegate LocalVideoStreamUpdated;

        public delegate void RemoteSharingChangedDelegate(bool hasSharing);
        public event RemoteSharingChangedDelegate RemoteSharingChanged;
        public delegate void RemoteVideoChangedDelegate(bool hasVideo);
        public event RemoteVideoChangedDelegate RemoteVideoChanged;

        public delegate void ReadyDelegate(bool isReadyAndConnected);
        public event ReadyDelegate Ready;
        public delegate void ConnectionChangedDelegate(string state);
        public event ConnectionChangedDelegate ConnectionChanged;

        public delegate void ApplicationDelegate(Rainbow.Application application);
        public event ApplicationDelegate OnApplicationInit;
        public event ApplicationDelegate OnApplicationDispose;

        public Rainbow.Application RainbowApplication { get { return rbApplication; } }
        public WebRTCCommunications RainbowWebRTC { get { return rbWebRTCCommunications; } }

        private Rainbow.Application rbApplication;
        private AudioSource audiosourceToPublish;
        private WebRTCCommunications rbWebRTCCommunications;
        private WebCamDevice? webCamVideoDevice = null;
        private WebCamDevice? webCamSharingDevice = null;
        private string currentCallId = "";

        public RainbowThreadExecutor RainbowExecutor;
        public UnityWebRTCFactory UnityWebRTCFactory;
        private VideoDevice videoDevice;
        private VideoDevice sharingDevice;
        private QRCodeCredsLoader qrCodeScanner;

        private Coroutine webRTCUpdate = null;

        public Call CurrentCall
        {
            get
            {
                if (string.IsNullOrEmpty(currentCallId))
                {
                    return null;
                }
                if (rbWebRTCCommunications == null)
                {
                    return null;
                }
                return rbWebRTCCommunications.GetCall(currentCallId);
            }
        }
        public AudioSource AudioSourceToPublish
        {
            get
            {
                if (audiosourceToPublish == null)
                {
                    audiosourceToPublish = FindFirstObjectByType<AudioSource>();
                }
                return audiosourceToPublish;
            }
            set
            {
                audiosourceToPublish = value;
            }
        }

        public WebCamDevice? WebCamSharingDevice
        {
            get
            {
                return webCamSharingDevice;
            }
            set
            {
                webCamSharingDevice = value;
            }
        }
        public WebCamDevice? WebCamVideoDevice
        {
            get
            {
                return webCamVideoDevice;
            }
            set
            {
                webCamVideoDevice = value;
            }
        }

        private bool createdDefaultAudio = false;


        // vagelis
        public GameObject incomingCallPanel;


        #region Unity entry points
        private void Awake()
        {
            RainbowExecutor = InitializeUnityWebRTC();

            UnityWebRTCFactory = new UnityWebRTCFactory();
            initAudioSourceToPublish();

            InitRainbowApp();

            // We might do this differently
            DontDestroyOnLoad(gameObject);

            // TODO remove
            // if (AreCredentialsComplete())
            // {
            //     Debug.Log("Creds are complete");
            //     StartRainbowWithCredentials(UserLogin, UserPassword, RainbowHostName, ApplicationID, ApplicationSecretKey);
            // }
            // else
            // {
            //     Debug.Log("Creds not complete");
            //     qrCodeScanner = FindObjectOfType<QRCodeCredsLoader>();
            //     qrCodeScanner.CredentialsLoaded += QrCodeScanner_CredentialsLoaded;
            //     qrCodeScanner.StartScanning();
            // }

        }

        void Start()
        {

        }

        // Just Handle keyboard here:
        // Z = call contact
        // H = hangup
        // S,V = add sharing or video to current p2pcall or conference
        // D,B = remove sharing or video to current p2pcall or conference
        // C = join first active conference found.
        void Update()
        {

        }

        private void OnDestroy()
        {

            if (currentCallId != "")
            {
                if (RainbowExecutor != null)
                {
                    RainbowExecutor.Execute(() => rbWebRTCCommunications.HangUpCall(currentCallId));
                }
            }

            if (rbApplication != null)
            {
                // Unset events - Rainbow.Application
                rbApplication.ConnectionStateChanged -= RainbowApplication_ConnectionStateChanged;
                rbApplication.GetContacts().ContactAggregatedPresenceChanged -= RainbowContacts_ContactAggregatedPresenceChanged;

            }

            if (rbWebRTCCommunications != null)
            {
                // Unset events - WebRTCCommunications
                rbWebRTCCommunications.CallUpdated -= WebRTCCommunications_CallUpdated;
                rbWebRTCCommunications.OnMediaPublicationUpdated -= WebRTCCommunications_OnMediaPublicationUpdated;
                rbWebRTCCommunications.OnTrack -= WebRTCCommunications_OnTrack;
                rbWebRTCCommunications = null;
            }

            if (rbApplication != null)
            {
                // Log out
                rbApplication.Logout();
                OnApplicationDispose?.Invoke(rbApplication);
                rbApplication.Dispose();
                rbApplication = null;
            }

            if (AvatarCache != null)
            {
                AvatarCache.Terminate();
                AvatarCache = null;
            }

            // Stop unity webrtc library
            DeInitializeUnityWebRTC();

            if (createdDefaultAudio && audiosourceToPublish != null)
            {
                Destroy(audiosourceToPublish);
            }

        }

        #endregion Unity entry points

        private void InitRainbowApp()
        {
            //vagelis
            UnityMainThreadDispatcher.Instance();

            // Subscribe to SDK Logs                
            var logFactory = new BasicLoggerFactory();
            logFactory.OnLogEntry += LogFactory_OnLogEntry;
            Rainbow.LogFactory.Set(logFactory);

            string[] s = UnityEngine.Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            string iniFileName;
#if UNITY_EDITOR
            iniFileName = projectName + "_editor" + ".ini";
#else
            iniFileName = projectName + ".ini";
#endif
            rbApplication = new Rainbow.Application(iniFileName: iniFileName);
            // Handle events
            rbApplication.ConnectionStateChanged += RainbowApplication_ConnectionStateChanged;
            rbApplication.GetContacts().ContactAggregatedPresenceChanged += RainbowContacts_ContactAggregatedPresenceChanged;
            rbApplication.InitializationPerformed += RainbowApplication_InitializationPerformed;

            rbApplication.GetBubbles().BubbleInvitationReceived += RainbowController_BubbleInvitationReceived;

            RemoteTracksCache = new MediaStreamTrackCache(this);
            AvatarCache = new AvatarCache(this);

            Debug.Log("Init Rainbow App");
            OnApplicationInit?.Invoke(rbApplication);
        }

        #region public interface

        public bool Connect(string login, string password, string hostname, string appId, string appSecret)
        {

            if (AreCredentialsComplete(login, password, hostname, appId, appSecret))
            {
                Debug.Log("Creds are complete");
                StartRainbowWithCredentials(login, password, hostname, appId, appSecret);
                return true;
            }
            else
            {
                Debug.Log("Creds not complete");
                return false;
            }

        }

        public void Disconnect()
        {
            Disconnect(v => { });
        }
        public void Disconnect(Action<bool> callback)
        {
            if (CurrentCall != null)
            {
                HangupCall();
            }
            rbApplication.Logout((v) =>
                {
                    callback(v.Data);
                });
            // RemoteTracksCache = null;
            OnApplicationDispose?.Invoke(rbApplication);

            rbApplication.Dispose();
            rbApplication = null;
        }

        #endregion

        private void QrCodeScanner_CredentialsLoaded(string login, string password, string platform, string appId, string appSecret)
        {
            StartRainbowWithCredentials(login, password, platform, appId, appSecret);
            qrCodeScanner.StopScanning();
        }

        private bool AreCredentialsComplete(string login, string password, string hostname, string appId, string appSecret)
        {
            return (login.Length > 0 && password.Length > 0 && appId.Length > 0 && hostname.Length > 0 && appSecret.Length > 0);
        }

        // Provide the first microphone device found as a default for Audio.
        private void initAudioSourceToPublish()
        {
            Debug.Log("Init default AudioSource");
            if (audiosourceToPublish != null)
            {
                return;
            }

            if (Microphone.devices.Length == 0)
            {
                return;
            }

            string microName = Microphone.devices.First();

            int maxFreq = 48000;
            AudioClip clip;
            clip = Microphone.Start(microName, true, 1, maxFreq);
            if (clip == null)
            {
                Debug.LogError("Microphone.Start failed, clip is null");
            }
            while (!(Microphone.GetPosition(microName) > 0)) { }
            audiosourceToPublish = new GameObject("DefaultInputAudioSourceDevice").AddComponent<AudioSource>();
            audiosourceToPublish.clip = clip;
            audiosourceToPublish.loop = true;
            createdDefaultAudio = true;
            DontDestroyOnLoad(audiosourceToPublish);
        }
        private void StartRainbowWithCredentials(string login, string password, string hostname, string appId, string appSecret)
        {

            // TODO this probably needs some additional handling

            // Subscribe to SDK Logs         
            if (rbApplication == null)
            {
                InitRainbowApp();
            }

            if (rbApplication.IsConnected())
            {
                rbApplication.Logout((v) =>
                {
                    Debug.Log($"Logout {v}");
                });
            }

            rbApplication.Restrictions.UseWebRTC = true;
            rbApplication.Restrictions.UseConferences = true;

            // Set Rainbow.Application main values
            rbApplication.SetApplicationInfo(appId, appSecret);
            rbApplication.SetHostInfo(hostname);
            rbApplication.SetTimeout(90000);
            rbApplication.Restrictions.LogRestRequest = LogRestSuccess;
            rbApplication.Restrictions.LogRestRequestOnError = LogRestError;

            // Initialize the Remote display objects once and for all
            if (HandleRemoteVideoStreams)
            {
                VideoRemoteVideoDisplay = new RemoteVideoDisplay(null, VideoDisplay);
                SharingRemoteVideoDisplay = new RemoteVideoDisplay(null, SharingDisplay);
            }

            // Login
            rbApplication.Login(login, password, result =>
            {
                // TODO add callback or something for this
                if (result.Result.Success)
                {
                    Debug.Log($"Login successfull {login}");
                }
                else
                {
                    Debug.Log("Error: " + result.Result.ToString());
                }
            });
        }

        private void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
         UnityEngine.Application.OpenURL(webplayerQuitURL);
#else
            UnityEngine.Application.Quit();
#endif
        }

        public bool P2PCallContact(string Id)
        {
            Assert.IsTrue(UnityExecutor.IsUnityThread(), "Method P2PCallContact was called from a non unity thread");
            if (currentCallId != "")
            {
                return false;
            }
            if (rbApplication.ConnectionState() != "connected")
            {
                Debug.Log("You need to be connected first");
                return false;
            }

            // AudioSource inputAudio = getAudioInputDevice();
            if (AudioSourceToPublish == null)
            {
                Debug.LogError("No audio source");
                return false;
            }

            audiosourceToPublish.Play();
            RainbowExecutor.Execute(() =>
            {
                Dictionary<int, IMediaStreamTrack> mediaStreams = new();
                var mediaAudio = UnityWebRTCFactory.CreateAudioTrack(UnityWebRTCFactory.CreateAudioMediaDevice(AudioSourceToPublish));
                mediaStreams.Add(Media.AUDIO, mediaAudio);
                rbWebRTCCommunications.MakeCall(Id, mediaStreams, "P2P Call from Unity");
            });
            return true;
        }

        public bool HangupCall()
        {
            if (string.IsNullOrEmpty(currentCallId))
            {
                return false;
            }
            if (videoDevice != null)
            {
                videoDevice.Dispose();
                videoDevice = null;
            }
            if (sharingDevice != null)
            {
                sharingDevice.Dispose();
                sharingDevice = null;
            }

            return rbWebRTCCommunications.HangUpCall(currentCallId);
        }

        private bool JoinFirstActiveConference()
        {
            var conferences = rbApplication.GetConferences().ConferenceGetListFromCache();
            if (rbApplication.ConnectionState() != "connected")
            {
                Debug.Log("You need to be connected first");
                return false;
            }

            if (currentCallId != "")
            {
                Debug.LogError("already in a call");
                return false;
            }
            foreach (var conference in conferences)
            {
                JoinConference(conference);
                return true;
            }

            Debug.LogError("No conference found");
            return false;
        }

        public void JoinConference(string Id)
        {
            UnityExecutor.Execute(() =>
            {
                // Create or reuse the audio source we want to "publish"
                audiosourceToPublish = AudioSourceToPublish;
                audiosourceToPublish.mute = false;
                audiosourceToPublish.Play();

                var mediaAudio = UnityWebRTCFactory.CreateAudioTrack(UnityWebRTCFactory.CreateAudioMediaDevice(audiosourceToPublish));

                // Then call joinConference from the Rainbow thread
                RainbowExecutor.Execute(() =>
                {
                    rbWebRTCCommunications.JoinConference(Id, mediaAudio, cb =>
                    {
                        currentCallId = cb.Data;
                    });
                });
            });
        }

        public void StartAndJoinConference(string Id)
        {
            Assert.IsTrue(UnityExecutor.IsUnityThread(), "Method StartAndJoinConference was called from a non unity thread");
            UnityExecutor.Execute(() =>
            {
                audiosourceToPublish.volume = 0.3f;
                audiosourceToPublish.Play();

                var mediaAudio = UnityWebRTCFactory.CreateAudioTrack(UnityWebRTCFactory.CreateAudioMediaDevice(audiosourceToPublish));

                // Then call StartAndJoinConference from the Rainbow thread
                RainbowExecutor.Execute(() =>
                {
                    rbWebRTCCommunications.StartAndJoinConference(Id, mediaAudio, cb =>
                    {
                        currentCallId = cb.Data;
                    });
                });
            });
        }

        private void JoinConference(Conference conference)
        {
            JoinConference(conference.Id);
        }

        private void RaiseLocalStreamUpdated(bool isSharing, IMediaStreamTrack mediaStreamTrack)
        {
            if (LocalVideoStreamUpdated == null)
                return;
            UnityExecutor.Execute(() =>
            {
                LocalVideoStreamUpdated.Invoke(isSharing, mediaStreamTrack);
            });
        }

        // Uses the sdk to remove a media from the current call.
        public void RemoveMediaFromCurrentCall(int mediaToDelete)
        {
            if (string.IsNullOrEmpty(currentCallId))
            {
                Debug.LogWarning("No current callID won't remove media");
                return;
            }

            Call call = rbWebRTCCommunications.GetCall(currentCallId);
            if (call == null)
            {
                Debug.LogWarning("remove media: abort: Call not found");
                return;
            }

            if ((call.LocalMedias & mediaToDelete) != 0)
            {
                RainbowExecutor.Execute(() =>
                {
                    if (mediaToDelete == Media.VIDEO)
                    {
                        UnityExecutor.Execute(() =>
                        {
                            if (videoDevice != null)
                            {
                                videoDevice.Dispose();
                                videoDevice = null;
                            }

                            RaiseLocalStreamUpdated(false, null);

                        });
                        rbWebRTCCommunications.RemoveVideo(currentCallId);
                    }
                    else if (mediaToDelete == Media.SHARING)
                    {
                        UnityExecutor.Execute(() =>
                        {
                            if (sharingDevice != null)
                            {
                                sharingDevice.Dispose();
                                sharingDevice = null;
                            }
                            RaiseLocalStreamUpdated(true, null);
                        });
                        rbWebRTCCommunications.RemoveSharing(currentCallId);
                    }
                });
            }

        }

        // Uses the sdk to mute/unmute auio.
        public void ToggleAudioMuteCurrentCall(Action<SdkResult<bool>>? cb = null)
        {
            if (string.IsNullOrEmpty(currentCallId))
            {
                Debug.LogWarning("No current callID won't mute");
                return;
            }

            Call call = rbWebRTCCommunications.GetCall(currentCallId);
            if (call == null)
            {
                Debug.LogWarning("mute: abort: Call not found");
                return;
            }

            bool mustMute = true;
            if (call.IsLocalAudioMuted)
            {
                mustMute = false;
            }
            RainbowExecutor.Execute(() => rbWebRTCCommunications.MuteAudio(currentCallId, mustMute, cb));

        }

        public void UpdateMediaFromCurrentCall(int mediaToAdd, Texture texture)
        {
            RainbowExecutor.Execute(() =>
            {
                if (string.IsNullOrEmpty(currentCallId))
                {
                    Debug.LogWarning("No current callID won't update media");
                    return;
                }

                Debug.Log($"Will Update Media {mediaToAdd} in {currentCallId}");

                Call call = rbWebRTCCommunications.GetCall(currentCallId);
                if (call == null)
                {
                    Debug.LogWarning($"update media: abort: Call not found:[{currentCallId}]");
                    return;
                }
                if ((call.LocalMedias & mediaToAdd) == mediaToAdd)
                {
                    VideoDevice temporaryDevice;
                    if (mediaToAdd == Media.VIDEO)
                    {

                        if (texture != null)
                        {
                            Debug.Log("[UpdateMediaFromCurrentCall] VIDEO - Using Unity Texture");
                            temporaryDevice = UnityWebRTCFactory.CreateTextureDevice(texture);
                        }
                        else
                        {
                            Debug.LogWarning("[UpdateMediaFromCurrentCall] VIDEO - Texture object is null");
                            return;
                        }

                        IVideoStreamTrack videoTrack = UnityWebRTCFactory.CreateVideoTrack(temporaryDevice);
                        bool willBeFlippedX = temporaryDevice.isFlippedX;

                        if (videoTrack == null)
                        {
                            Debug.LogWarning("[UpdateMediaFromCurrentCall] VIDEO - Cannot create videoTrack");
                            temporaryDevice.Dispose();
                            temporaryDevice = null;
                            return;
                        }

                        if (rbWebRTCCommunications.ChangeVideo(currentCallId, videoTrack))
                        {
                            IsLocalVideoFlipped = willBeFlippedX;

                            if (videoDevice != null)
                            {
                                videoDevice.Dispose();
                                videoDevice = null;
                            }
                            videoDevice = temporaryDevice;
                            UnityExecutor.Execute(() =>
                            {
                                RaiseLocalStreamUpdated(false, videoTrack);
                            });
                        }
                        else
                        {
                            Debug.LogError("Change video failed");
                        }
                    }
                    else if (mediaToAdd == Media.SHARING)
                    {
                        if (texture != null)
                        {
                            Debug.Log("[UpdateMediaFromCurrentCall] SHARING - Using Unity Texture");
                            temporaryDevice = UnityWebRTCFactory.CreateTextureDevice(texture);
                        }
                        else
                        {
                            Debug.LogWarning("[UpdateMediaFromCurrentCall] SHARING - Texture object is null");
                            return;
                        }

                        IVideoStreamTrack sharingVideoTrack = UnityWebRTCFactory.CreateVideoTrack(temporaryDevice);
                        bool willBeFlippedX = temporaryDevice.isFlippedX;

                        if (sharingVideoTrack == null)
                        {
                            Debug.LogWarning("[UpdateMediaFromCurrentCall] SHARING - Cannot create videoTrack");
                            temporaryDevice.Dispose();
                            temporaryDevice = null;
                            return;
                        }
                        if (rbWebRTCCommunications.ChangeSharing(currentCallId, sharingVideoTrack))
                        {
                            IsLocalSharingFlipped = willBeFlippedX;
                            if (sharingDevice != null)
                            {
                                sharingDevice.Dispose();
                                sharingDevice = null;
                            }
                            sharingDevice = temporaryDevice;
                            UnityExecutor.Execute(() =>
                            {
                                RaiseLocalStreamUpdated(true, sharingVideoTrack);
                            });
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("update media: media not used");
                }
            });
        }
        public void AddMediaToCurrentCall(int mediaToAdd, VideoDevice device = null)
        {
            if (string.IsNullOrEmpty(currentCallId))
            {
                Debug.LogWarning("No current callID won't add media");
                return;
            }

            Debug.LogWarning($"Will Add Media {mediaToAdd} to {currentCallId}");

            Call call = rbWebRTCCommunications.GetCall(currentCallId);
            if (call == null)
            {
                Debug.LogWarning("add media: abort: Call not found :(");
                return;
            }
            if ((call.LocalMedias & mediaToAdd) == 0)
            {
                if (mediaToAdd == Media.VIDEO)
                {
                    if (device != null)
                    {
                        videoDevice = device;
                    }
                    else if (webCamVideoDevice == null)
                    {
                        Debug.Log("Using Unity camera " + CameraVideo.name);
                        videoDevice = UnityWebRTCFactory.CreateCameraDevice(this.CameraVideo, PublishVideoWidth, PublishVideoHeight);
                    }
                    else
                    {
                        Debug.Log("Using real camera");
                        videoDevice = UnityWebRTCFactory.CreateWebCamDevice(this.webCamVideoDevice, PublishVideoWidth, PublishVideoHeight);
                    }
                    videoDevice.SetOptions(GetVideoOptions());
                    IVideoStreamTrack videoTrack = UnityWebRTCFactory.CreateVideoTrack(videoDevice);
                    if (videoTrack == null)
                    {
                        videoDevice.Dispose();
                        videoDevice = null;
                        return;
                    }
                    IsLocalVideoFlipped = videoDevice.isFlippedX;
                    RainbowExecutor.Execute(() =>
                    {
                        rbWebRTCCommunications.AddVideo(
                            currentCallId,
                            videoTrack);
                        UnityExecutor.Execute(() =>
                        {
                            RaiseLocalStreamUpdated(false, videoTrack);
                        });
                    });
                }
                else if (mediaToAdd == Media.SHARING)
                {
                    if (device != null)
                    {
                        sharingDevice = device;
                    }
                    else if (webCamSharingDevice == null)
                    {
                        Debug.Log("Using Unity camera " + CameraSharing.name);
                        sharingDevice = UnityWebRTCFactory.CreateCameraDevice(this.CameraSharing, PublishSharingWidth, PublishSharingHeight);

                    }
                    else
                    {
                        Debug.Log("Using real camera " + webCamSharingDevice?.name);
                        sharingDevice = UnityWebRTCFactory.CreateWebCamDevice(this.webCamSharingDevice, PublishSharingWidth, PublishSharingHeight);
                    }
                    sharingDevice.SetOptions(GetVideoOptions());
                    IVideoStreamTrack sharingVideoTrack = UnityWebRTCFactory.CreateVideoTrack(sharingDevice);
                    if (sharingVideoTrack == null)
                    {
                        sharingDevice.Dispose();
                        sharingDevice = null;
                        return;
                    }
                    IsLocalSharingFlipped = sharingDevice.isFlippedX;
                    RainbowExecutor.Execute(() =>
                    {
                        rbWebRTCCommunications.AddSharing(currentCallId, sharingVideoTrack);
                        UnityExecutor.Execute(() =>
                        {
                            RaiseLocalStreamUpdated(true, sharingVideoTrack);
                        });
                    });
                }
            }
        }

        #region Core SDK events
        private void RainbowContacts_ContactAggregatedPresenceChanged(object sender, PresenceEventArgs e)
        {
            if (e.Presence != null && rbApplication.GetContacts().GetCurrentContactJid().StartsWith(e.Presence.BasicNodeJid))
            {
                Debug.Log($"Presence is now {e.Presence.PresenceLevel}");
            }
        }

        private void RainbowApplication_InitializationPerformed(object sender, EventArgs e)
        {
            try
            {
                rbWebRTCCommunications = WebRTCCommunications.GetOrCreateInstance(rbApplication, UnityWebRTCFactory);
                rbWebRTCCommunications.CallUpdated += WebRTCCommunications_CallUpdated;
                rbWebRTCCommunications.OnMediaPublicationUpdated += WebRTCCommunications_OnMediaPublicationUpdated;
                rbWebRTCCommunications.OnTrack += WebRTCCommunications_OnTrack;

                // Bootstrap conferencing and multi chat
                rbApplication.GetBubbles().GetAllBubbles(result =>
                {
                    if (!result.Result.Success)
                    {
                        Debug.Log($"GetAllBubbles returned {result.Result.ExceptionError} {result.Result.IncorrectUseError}");
                    }

                    Conversations conversations = rbApplication.GetConversations();

                    conversations.GetAllConversations(callback =>
                    {

                        UnityExecutor.Execute(() =>
                        {
                            Ready?.Invoke(true);
                        });
                    });
                });

            }
            catch (Exception ex)
            {
                Debug.Log($"Exception {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
            }
        }
        private void RainbowController_BubbleInvitationReceived(object sender, BubbleInvitationEventArgs e)
        {
            // Auto accept invitations to new bubbles:
            rbApplication.GetBubbles().AcceptInvitation(e.BubbleId, result =>
            {
                if (!result.Result.Success)
                {
                    Debug.LogError($"failed to accept invitation to bubble {e.BubbleName}: {result.Result.ExceptionError}");
                }
            });
        }

        private void RainbowApplication_ConnectionStateChanged(object sender, ConnectionStateEventArgs e)
        {
            Debug.Log($"Connection State changed {e.ConnectionState.State}");
            UnityExecutor.Execute(() =>
            {
                ConnectionChanged?.Invoke(e.ConnectionState.State);

                //vagelis
                if (e.ConnectionState.State == ConnectionState.Connected)
                {                    
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        GetComponent<ConversationsManager>().InitializeConversationsAndContacts();
                        GetComponent<BubbleManager>().InitializeBubblesManager();
                        GetComponent<FileManager>().InitializeFileManager();
                    });
                }
            });
        }

        #endregion Core SDK events

        #region RetryUtils
        private void SubscribeToMediaPublicationWithRetry(MediaPublication mediaPublication, Action<SdkResult<bool>> cb, int nbTry = 0, int maxTry = 3)
        {
            rbWebRTCCommunications.SubscribeToMediaPublication(mediaPublication, Conference.SubStreamLevel.MIDDLE, result =>
        {
            if (result.Result.Success)
            {
                cb(result);
            }
            else
            {
                int statusCode = (int)result.Result.HttpStatusCode;
                Debug.Log($"SubscribeToMediaPublication -> {result.Result.ResponseStatus}");
                if (nbTry < maxTry &&
                    (result.Result.ResponseStatus != "None") && (
                    (result.Result.HttpStatusCode < (int)System.Net.HttpStatusCode.OK) ||
                    ((int)System.Net.HttpStatusCode.Ambiguous <= result.Result.HttpStatusCode)))
                {
                    Debug.LogError($"SubscribeToMediaPublicationWithRetry callId={mediaPublication.CallId} media={mediaPublication.Media} failed. (try {nbTry + 1}/{maxTry})");

                    SubscribeToMediaPublicationWithRetry(mediaPublication, cb, nbTry + 1, maxTry);
                }
                else
                {
                    cb(result);
                }
            }
        });
        }
        #endregion

        #region WEBRTCCommunication events

        public void SubscribeToPendingMediaPublications()
        {
            Contacts ContactService = rbApplication.GetContacts();
            Call call = rbWebRTCCommunications.GetCall(currentCallId);
            if (call == null)
            {
                return;
            }

            var mediaPublications = RainbowWebRTC.GetMediaPublicationsAvailable(currentCallId);
            foreach (var mediaPublication in mediaPublications)
            {
                if (mediaPublication.PublisherJid_im == ContactService.GetCurrentContactJid())
                {
                    continue;
                }

                if (call.IsConference && NotInterestedInThisMediaPublication(mediaPublication))
                {
                    Debug.Log($"Ignore the remote stream stream  Media {mediaPublication.Media} from {mediaPublication.PublisherId}: the publisher isn't a c# client. ");
                    return;
                }

                SubscribeToMediaPublicationWithRetry(mediaPublication, result =>
                {
                    if (!result.Data)
                        Debug.LogError($"SubscribeToMediaPublication {mediaPublication.Media} failed ");
                    else
                        Debug.Log($"SubscribeToMediaPublication {mediaPublication.Media} ok ");
                    return;
                });
            }
        }

        private bool NotInterestedInThisMediaPublication(MediaPublication mediaPublication)
        {
            if ((IgnoreNonUnityVideos && mediaPublication.Media == Media.VIDEO) || (IgnoreNonUnitySharing && mediaPublication.Media == Media.SHARING))
            {
                Contact publisher = rbApplication.GetContacts().GetContactFromContactId(mediaPublication.PublisherId);
                string resource = FindUsableResource(publisher);
                if (resource == string.Empty)
                {
                    return true;
                }
            }
            return false;
        }

        private void WebRTCCommunications_OnMediaPublicationUpdated(object sender, MediaPublicationEventArgs e)
        {

            Contacts ContactService = rbApplication.GetContacts();

            // If there is no call / no callId, do nothing
            Call call = rbWebRTCCommunications.GetCall(e.MediaPublication.CallId);
            if (call == null)
            {
                return;
            }

            // Ignore events related to our own publications
            if (e.MediaPublication.PublisherJid_im == ContactService.GetCurrentContactJid())
            {
                return;
            }

            Debug.Log($"Received mediapublication event status = {e.Status} jid {e.MediaPublication.PublisherJid_im} media {e.MediaPublication.Media} me {rbApplication.GetContacts().GetCurrentContactJid()} isMe: {e.MediaPublication.PublisherJid_im == rbApplication.GetContacts().GetCurrentContactJid()}");

            // If a new media publication is started, subscribe to it. (i.e. accept any remote media )
            if (e.Status == MediaPublicationStatus.PEER_STARTED)
            {
                // If it's audio, we want to subscribe no matter what the state is
                if (e.MediaPublication.Media != Media.AUDIO)
                {
                    // we want to wait for the call to be active or connecting before we accept the media publication
                    // missed publications of existing medias in a pre existing confenrence received during the dialing state
                    // will be retrieved by the com manager when the call is established, and the participants retrieved.
                    if (call.CallStatus != Status.ACTIVE && call.CallStatus != Status.CONNECTING)
                    {
                        Debug.Log($"Don't subscribe to the video stream Media {{e.MediaPublication.Media}} from {{e.MediaPublication.PublisherId}}: call status is {call.CallStatus}.");
                        return;
                    }
                }

                if (call.IsConference && NotInterestedInThisMediaPublication(e.MediaPublication))
                {
                    Debug.Log($"Ignore the remote stream stream  Media {e.MediaPublication.Media} from {e.MediaPublication.PublisherId}: the publisher isn't a c# client. ");
                    return;
                }

                Debug.Log($"SubscribeToMediaPublication");
                // Ok: want to subscribe to this audio, video or sharing stream
                SubscribeToMediaPublicationWithRetry(e.MediaPublication, result =>
                {
                    if (!result.Data)
                    {
                        Debug.LogError($"SubscribeToMediaPublication failed ");
                        return;
                    }
                    if (HandleRemoteVideoStreams)
                    {
                        if (e.MediaPublication.Media == Media.SHARING)
                            UnityExecutor.Execute(() => { this.RemoteSharingChanged?.Invoke(true); });
                        if (e.MediaPublication.Media == Media.VIDEO)
                            UnityExecutor.Execute(() => { this.RemoteVideoChanged?.Invoke(true); });
                    }
                });

            }
            else if (e.Status == MediaPublicationStatus.PEER_STOPPED)
            {
                if (HandleRemoteVideoStreams)
                {
                    if (e.MediaPublication.Media == Media.SHARING)
                        UnityExecutor.Execute(() => { this.RemoteSharingChanged?.Invoke(false); });
                    if (e.MediaPublication.Media == Media.VIDEO)
                        UnityExecutor.Execute(() => { this.RemoteVideoChanged?.Invoke(false); });
                }
            }
        }

        private String FindUsableResource(Contact contact)
        {
            string resource = string.Empty;
            var ContactService = rbApplication.GetContacts();

            if (contact == null || contact.Equals(ContactService.GetCurrentContact()))
            {
                return resource;
            }

            Dictionary<String, Presence> presences = ContactService.GetPresencesFromContact(contact);

            if (presences == null || presences.Count <= 0)
            {
                return resource;
            }

            foreach (var presence in presences)
            {
                if (presence.Key.StartsWith("sdk_net_"))
                {
                    if (PresenceLevel.Busy == presence.Value.PresenceLevel)
                    {
                        resource = presence.Key;
                        break;
                    }
                }
            }

            return resource;
        }
        // Track call changes
        private void WebRTCCommunications_CallUpdated(object? sender, CallEventArgs e)
        {

            Debug.Log($"call updated id {e.Call?.Id} status {e.Call?.CallStatus} cuurentCallId = {currentCallId}");
            if (e.Call == null)
                return;

            // Incoming call: accept it 
            if (e.Call.CallStatus == Status.RINGING_INCOMING)
            {
                currentCallId = e.Call?.Id;

                UnityExecutor.Execute(() =>
                {
                    // Show the Accept/Reject panel
                    ShowCallPanel(() =>
                    {
                        AcceptCall(currentCallId);
                    },
                    () =>
                    {
                        RejectCall(currentCallId);
                    });

                    //if (AudioSourceToPublish == null)
                    //{
                    //    Debug.LogError("Couldn't find an audio source");
                    //    return;
                    //}
                    //Dictionary<int, IMediaStreamTrack> mediaStreams = new();
                    //AudioSourceToPublish.Play();
                    //var mediaAudio = UnityWebRTCFactory.CreateAudioTrack(UnityWebRTCFactory.CreateAudioMediaDevice(AudioSourceToPublish));
                    //mediaStreams.Add(Media.AUDIO, mediaAudio);
                    //RainbowExecutor.Execute(() =>
                    //{
                    //    rbWebRTCCommunications.AnswerCall(currentCallId, mediaStreams);
                    //    Debug.Log("ANSWER CALL");
                    //});

                });
            }

            if (e.Call.CallStatus == Status.RELEASING || e.Call.CallStatus == Status.ERROR || e.Call.CallStatus == Status.UNKNOWN)
            {
                // put presence back to Inactive
                Presence presence = rbApplication.GetContacts().CreatePresence(false, PresenceLevel.Online, PresenceDetails.Inactive);
                this.rbApplication.GetContacts().SetPresenceLevel(presence);

                UnityExecutor.Execute(() =>
                {
                    if (videoDevice != null)
                    {
                        videoDevice.Dispose();
                        videoDevice = null;
                    }

                    if (sharingDevice != null)
                    {
                        sharingDevice.Dispose();
                        sharingDevice = null;
                    }
                    // the audio track will be released: pause it
                    // audiosourceToPublish.volume = 0;
                    audiosourceToPublish.Pause();
                    if (HandleRemoteVideoStreams)
                    {
                        this.RemoteSharingChanged?.Invoke(false);
                        this.RemoteVideoChanged?.Invoke(false);
                    }
                });

                // lose the callId
                currentCallId = "";
            }

            if (e.Call?.CallStatus == Status.ACTIVE)
            {
                Presence presence = rbApplication.GetContacts().CreatePresence(false, PresenceLevel.Busy, PresenceDetails.Audio);
                this.rbApplication.GetContacts().SetPresenceLevel(presence);
                currentCallId = e.Call?.Id;
            }

        }


        // vagelis start

        void ShowCallPanel(Action onAccept, Action onReject)
        {
            incomingCallPanel.SetActive(true);
                        
            Button acceptButton = incomingCallPanel.transform.Find("AcceptButton").GetComponent<Button>();
            Button rejectButton = incomingCallPanel.transform.Find("RejectButton").GetComponent<Button>();

            // Assign button actions
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() =>
            {
                onAccept?.Invoke();
                incomingCallPanel.SetActive(false); // Hide the panel
            });

            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() =>
            {
                onReject?.Invoke();
                incomingCallPanel.SetActive(false); // Hide the panel
            });
        }



        void AcceptCall(string callId)
        {
            if (AudioSourceToPublish == null)
            {
                Debug.LogError("Couldn't find an audio source");
                return;
            }

            Dictionary<int, IMediaStreamTrack> mediaStreams = new();
            AudioSourceToPublish.Play();
            var mediaAudio = UnityWebRTCFactory.CreateAudioTrack(UnityWebRTCFactory.CreateAudioMediaDevice(AudioSourceToPublish));
            mediaStreams.Add(Media.AUDIO, mediaAudio);

            RainbowExecutor.Execute(() =>
            {
                rbWebRTCCommunications.AnswerCall(callId, mediaStreams);
                Debug.Log("ANSWER CALL");
            });
        }



        void RejectCall(string callId)
        {
            RainbowExecutor.Execute(() =>
            {
                rbWebRTCCommunications.RejectCall(callId);
                Debug.Log("REJECT CALL");
            });
        }



        // vagelis end






        private void WebRTCCommunications_OnTrack(string callId, MediaStreamTrackDescriptor mediaStreamTrackDescriptor)
        {
            IMediaStreamTrack mediaStreamTrack = mediaStreamTrackDescriptor.MediaStreamTrack;
            int media = mediaStreamTrackDescriptor.Media;

            // In any case we won't support OnTrack from local medias in Unity
            if (mediaStreamTrackDescriptor.PublisherId == rbApplication.GetContacts().GetCurrentContactId())
            {
                return;
            }

            Debug.Log($"Received OnTrack. CallId={callId} Media={mediaStreamTrackDescriptor.Media} PublisherID={mediaStreamTrackDescriptor.PublisherId} selfId={rbApplication.GetContacts().GetCurrentContactId()}");

            if (mediaStreamTrack is IAudioStreamTrack audioStreamTrack)
            {
                Debug.Log($"Received an audio track callId={callId} media={mediaStreamTrackDescriptor.Media}");
                try
                {
                    UnityWebRTCFactory.OutputAudio(audioStreamTrack, outputAudioSource);
                }
                catch (Exception e)
                {
                    Debug.LogError($"EXCEPTION: {e.Message}\n{e.StackTrace}");
                }
                return;
            }

            if (!HandleRemoteVideoStreams)
                return;

            IVideoStreamTrack videoTrack = mediaStreamTrack as IVideoStreamTrack;
            {
                Debug.Log($"Received a video track callId={callId}  media={mediaStreamTrackDescriptor.Media}");
                RemoteVideoDisplay destinationForTrack = VideoRemoteVideoDisplay;

                switch (media)
                {
                    case Media.SHARING:
                        destinationForTrack = SharingRemoteVideoDisplay;
                        break;
                    case Media.VIDEO:
                        destinationForTrack = VideoRemoteVideoDisplay;
                        break;
                }

                try
                {
                    destinationForTrack.Track = videoTrack;
                    destinationForTrack.Active = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"EXCEPTION: {e.Message}\n{e.StackTrace}");
                }
            }

        }

        #endregion WEBRTCCommunication events

        private Dictionary<string, System.Object> GetVideoOptions()
        {
            if (!SetMaxBitrate && !SetMinBitrate && !SetScaleResolutionDownBy && !SetMaxFramerate) return null;

            Dictionary<string, System.Object> result = new();

            if (SetMaxBitrate)
                result[VideoDeviceOptionsNames.MaxBitrate] = MaxBitrate;
            if (SetMinBitrate)
                result[VideoDeviceOptionsNames.MinBitrate] = MinBitrate;
            if (SetMaxFramerate)
                result[VideoDeviceOptionsNames.MaxFramerate] = MaxFramerate;
            if (SetScaleResolutionDownBy)
                result[VideoDeviceOptionsNames.ScaleResolutionDownBy] = ScaleResolutionDownBy;
            return result;
        }

        private void LogFactory_OnLogEntry(DateTime dateTime, string categoryName, Microsoft.Extensions.Logging.LogLevel logLevel, int eventId, string message)
        {
            if (!LogRainbow)
                return;

            if (logLevel == Microsoft.Extensions.Logging.LogLevel.None)
                return;
            String date = DateTime.Now.ToString("0:MM/dd/yy H:mm:ss:ffff");
            String logEntry = $"[{date}] [{categoryName}] {message}";
            LogOption option = LogOption.NoStacktrace;
            if (!HideStackInLog)
            {
                option = LogOption.None;
            }
            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    Debug.LogFormat(LogType.Log, option, null, "{0}", logEntry);
                    break;

                case Microsoft.Extensions.Logging.LogLevel.Information:
                    Debug.LogFormat(LogType.Log, option, null, "{0}", logEntry);
                    break;

                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    Debug.LogFormat(LogType.Warning, option, null, "{0}", logEntry);
                    break;

                case Microsoft.Extensions.Logging.LogLevel.Error:
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    Debug.LogError(logEntry);
                    break;
            }
        }

        // This will be part of the namespace Rainbow.WebRTC.Unity
        private RainbowThreadExecutor InitializeUnityWebRTC()
        {
            // Start WebRTC update coroutine 
            webRTCUpdate = StartCoroutine(WebRTC.Update());

            // Create the unity executor from main thread
            UnityExecutor.Initialize();
            return new RainbowThreadExecutor();
        }

        private void DeInitializeUnityWebRTC()
        {
            if (webRTCUpdate != null)
            {
                StopCoroutine(webRTCUpdate);
                webRTCUpdate = null;
            }
            RainbowExecutor.Stop();
            RainbowExecutor = null;

            //  UnityExecutor creates an object but does not clean it up
            var exec = GameObject.Find("executor");
            if (exec != null)
            {
                Destroy(exec);
            }
        }

    }

} // End namespace cortex