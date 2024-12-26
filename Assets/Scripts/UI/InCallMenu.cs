using System;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Simple UI script to show a Call/Mute/Share Video/Share interface.
    /// </summary>
    public class InCallMenu : BaseStartOrEnabled
    {
        #region public events

        /// <summary>
        /// Event fired when the user clicks on the call button. Since the specifics of calling depends on the circumstances, like P2P or Conferences, the actual call action is delegated
        /// </summary>
        public event Action OnRequestCall;

        /// <summary>
        /// Event fired when the user clicks on the share video button, while no video is currently shared. Since the specifics of sharing depends on the circumstances, like a Webcam or custom video, the actual share video action is delegated
        /// </summary>
        public event Action OnRequestShareVideo;

        /// <summary>
        /// Event fired when the user clicks on the share video button, while a video is being shared. Sharing will be stopped and afterwards this callback will be called to allow for cleanup
        /// </summary>
        public event Action OnStopVideo;
        /// <summary>
        /// Event fired when the user clicks on the share  button, while nothing is currently being shared. Since the specifics of sharing depends on the circumstances, like a Webcam or custom video, the actual share action is delegated
        /// </summary>
        public event Action OnRequestShare;

        /// <summary>
        /// Event fired when the user clicks on the share button, while nothing is being shared. Sharing will be stopped and afterwards this callback will be called to allow for cleanup
        /// </summary>
        public event Action OnStopShare;

        #endregion // public events

        #region inspector fields

        [Header("Buttons")]
        // Backing field for property ButtonMute
        [SerializeField]
        private Button m_buttonMute;

        public Button ButtonHangUp
        {
            get => m_buttonHangUp;
            private set => m_buttonHangUp = value;
        }
        // Backing field for property ButtonHangUp
        [SerializeField]
        private Button m_buttonHangUp;

        public Button ButtonShareVideo
        {
            get => m_buttonShareVideo;
            private set => m_buttonShareVideo = value;
        }
        // Backing field for property ButtonShareVideo
        [SerializeField]
        private Button m_buttonShareVideo;

        public Button ButtonShare
        {
            get => m_buttonShare;
            private set => m_buttonShare = value;
        }
        // Backing field for property ButtonShare
        [SerializeField]
        private Button m_buttonShare;

        public Sprite MicStart
        {
            get => m_micStart;
            private set => m_micStart = value;
        }
        [Header("Sprites")]
        // Backing field for property MicStart
        [SerializeField]
        private Sprite m_micStart;

        public Sprite MicStop
        {
            get => m_micStop;
            private set => m_micStop = value;
        }
        // Backing field for property MicStop
        [SerializeField]
        private Sprite m_micStop;

        public Sprite CamStart
        {
            get => m_camStart;
            private set => m_camStart = value;
        }
        // Backing field for property CamStart
        [SerializeField]
        private Sprite m_camStart;

        public Sprite CamStop
        {
            get => m_camStop;
            private set => m_camStop = value;
        }
        // Backing field for property CamStop
        [SerializeField]
        private Sprite m_camStop;

        public Sprite ShareStart
        {
            get => m_shareStart;
            private set => m_shareStart = value;
        }
        // Backing field for property ShareStart
        [SerializeField]
        private Sprite m_shareStart;

        public Sprite ShareStop
        {
            get => m_shareStop;
            private set => m_shareStop = value;
        }
        // Backing field for property ShareStop
        [SerializeField]
        private Sprite m_shareStop;

        public Sprite CallStart
        {
            get => m_callStart;
            private set => m_callStart = value;
        }
        // Backing field for property CallStart
        [SerializeField]
        private Sprite m_callStart;

        public Sprite CallHangUp
        {
            get => m_callHangUp;
            private set => m_callHangUp = value;
        }
        // Backing field for property CallHangUp
        [SerializeField]
        private Sprite m_callHangUp;

        public Button ButtonMute
        {
            get => m_buttonMute;
            private set => m_buttonMute = value;
        }

        #endregion // inspector fields

        #region public properties

        public bool Muted
        {
            get
            {
                return _muted;
            }

            private set
            {
                _muted = value;
                UpdateMuteButton();
            }
        }
        private bool _muted = false;

        private bool _connected = true;
        public bool Connected
        {
            get => _connected;
            private set
            {
                _connected = value;
                UpdateConnectedButton();
            }
        }

        private bool _allowVideoShare = true;
        public bool AllowVideoShare
        {
            get
            {
                return _allowVideoShare;
            }
            set
            {
                _allowVideoShare = value;
                // set container to false
                ButtonShareVideo.transform.parent.gameObject.SetActive(_allowVideoShare);
            }
        }

        private bool _allowShare = true;

        public bool AllowSharing
        {
            get
            {
                return _allowShare;
            }
            set
            {
                _allowShare = value;

                // set container to false
                ButtonShare.transform.parent.gameObject.SetActive(_allowShare);
            }
        }

        private bool _currentlySharingVideo = false;
        public bool CurrentlySharingVideo
        {
            get { return _currentlySharingVideo; }
            private set
            {
                _currentlySharingVideo = value;
                UpdateCurrentlySharingVideo();
            }
        }

        private bool _currentlySharing = false;
        public bool CurrentlySharing
        {
            get { return _currentlySharing; }
            private set
            {
                _currentlySharing = value;
                UpdateCurrentlySharing();
            }
        }

        #endregion // public properties

        #region internal

        void Awake()
        {
            if (ButtonMute == null)
            {
                ButtonMute = GameObjectUtils.FindGameObjectByName(transform, "ButtonMute", true).GetComponent<Button>();
            }
            if (ButtonHangUp == null)
            {
                ButtonHangUp = GameObjectUtils.FindGameObjectByName(transform, "ButtonHangUp", true).GetComponent<Button>();
            }
            if (ButtonShareVideo == null)
            {
                ButtonShareVideo = GameObjectUtils.FindGameObjectByName(transform, "ButtonShareVideo", true).GetComponent<Button>();
            }
            if (ButtonShare == null)
            {
                ButtonShare = GameObjectUtils.FindGameObjectByName(transform, "ButtonShare", true).GetComponent<Button>();
            }
        }

        protected override void OnStartOrEnable()
        {
            ButtonMute.onClick.AddListener(OnMuteClick);
            ButtonHangUp.onClick.AddListener(OnHangUpClick);
            ButtonShareVideo.onClick.AddListener(OnShareVideoClick);
            ButtonShare.onClick.AddListener(OnShareClick);

            var m = ConnectionModel.Instance;

            // default values
            Muted = true;
            Connected = false;
            CurrentlySharingVideo = false;
            CurrentlySharing = false;

            // get current state, if available
            if (m != null)
            {
                var ri = m.RainbowInterface;

                var currentCall = ri != null ? ri.CurrentCall : null;

                Muted = currentCall?.IsLocalAudioMuted ?? true;

                if (currentCall != null)
                {
                    CurrentlySharingVideo = (currentCall.LocalMedias & Call.Media.VIDEO) == Call.Media.VIDEO;
                    CurrentlySharing = (currentCall.LocalMedias & Call.Media.SHARING) == Call.Media.SHARING;

                }

                Connected = currentCall != null;

            }

            ConnectionModel.OnCallConnected += OnCallConnected;
            ConnectionModel.OnCallDisconnected += OnCallDisconnected;
            ConnectionModel.OnCallStatusUpdate += OnCallStatusUpdated;

            ButtonHangUp.interactable = true;
            ButtonMute.interactable = true;
            ButtonShare.interactable = true;
            ButtonShareVideo.interactable = true;

            UpdateConnectedButton();
            UpdateMuteButton();
            UpdateCurrentlySharing();
            UpdateCurrentlySharingVideo();

            ButtonShare.transform.parent.gameObject.SetActive(AllowSharing);
            ButtonShareVideo.transform.parent.gameObject.SetActive(AllowVideoShare);
        }

        void OnDisable()
        {
            ButtonMute.onClick.RemoveListener(OnMuteClick);
            ButtonHangUp.onClick.RemoveListener(OnHangUpClick);
            ButtonShareVideo.onClick.RemoveListener(OnShareVideoClick);
            ButtonShare.onClick.RemoveListener(OnShareClick);

            ConnectionModel.OnCallConnected -= OnCallConnected;
            ConnectionModel.OnCallDisconnected -= OnCallDisconnected;
            ConnectionModel.OnCallStatusUpdate -= OnCallStatusUpdated;
        }

        private void OnCallStatusUpdated(ConnectionModel model, CallEventArgs args)
        {
            var call = args.Call;
            Muted = call.IsLocalAudioMuted;
            CurrentlySharingVideo = (call.LocalMedias & Call.Media.VIDEO) == Call.Media.VIDEO;
            CurrentlySharing = (call.LocalMedias & Call.Media.SHARING) == Call.Media.SHARING;
        }

        private void OnCallDisconnected(ConnectionModel model, Call call)
        {
            Muted = false; ;
            CurrentlySharingVideo = false;
            CurrentlySharing = false;
            Connected = false;
        }

        private void OnCallConnected(ConnectionModel model, Call call)
        {
            Muted = call.IsLocalAudioMuted;
            CurrentlySharingVideo = (call.LocalMedias & Call.Media.VIDEO) == Call.Media.VIDEO;
            CurrentlySharing = (call.LocalMedias & Call.Media.SHARING) == Call.Media.SHARING;
            Connected = true;
        }

        private void OnShareClick()
        {
            if (CurrentlySharing)
            {
                var model = ConnectionModel.Instance;
                if (model == null)
                {
                    return;
                }
                model.RainbowInterface.RemoveMediaFromCurrentCall(Call.Media.SHARING);
                model.RainbowInterface.CameraSharing = null;
                model.RainbowInterface.WebCamSharingDevice = null;
                OnStopShare?.Invoke();
            }
            else
            {
                OnRequestShare?.Invoke();
            }
        }

        private void OnShareVideoClick()
        {
            if (CurrentlySharingVideo)
            {
                var model = ConnectionModel.Instance;
                if (model == null)
                {
                    return;
                }
                model.RainbowInterface.RemoveMediaFromCurrentCall(Call.Media.VIDEO);
                model.RainbowInterface.CameraVideo = null;
                model.RainbowInterface.WebCamVideoDevice = null;
                OnStopVideo?.Invoke();
            }
            else
            {
                OnRequestShareVideo?.Invoke();
            }
        }

        private async void OnHangUpClick()
        {
            if (!Connected)
            {
                OnRequestCall?.Invoke();
            }
            else
            {
                ButtonHangUp.interactable = false;
                try
                {
                    await ConnectionModel.Instance.HangupCall();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    // call in the executor since the await might end up in a different thread
                    UnityExecutor.Execute(() =>
                    {
                        ButtonHangUp.interactable = true;
                    });
                }
            }
        }

        private void OnMuteClick()
        {
            var m = ConnectionModel.Instance;
            if (m == null)
            {
                return;
            }
            var ri = m.RainbowInterface;
            if (ri == null)
            {
                return;
            }

            ri.ToggleAudioMuteCurrentCall();
        }

        private void UpdateButton(Button button, bool toggle, Sprite toggleTrue, Sprite toggleFalse)
        {
            if (toggle)
            {
                button.GetComponent<Image>().sprite = toggleTrue;
            }
            else
            {
                button.GetComponent<Image>().sprite = toggleFalse;
            }
        }

        private void UpdateMuteButton()
        {
            UpdateButton(ButtonMute, Muted, MicStart, MicStop);
        }

        private void UpdateConnectedButton()
        {
            UpdateButton(ButtonHangUp, Connected, CallHangUp, CallStart);
        }

        private void UpdateCurrentlySharing()
        {
            UpdateButton(ButtonShare, CurrentlySharing, ShareStop, ShareStart);
        }
        private void UpdateCurrentlySharingVideo()
        {
            UpdateButton(ButtonShareVideo, CurrentlySharingVideo, CamStop, CamStart);
        }

        #endregion // internal
    }

} // end namespace Cortex