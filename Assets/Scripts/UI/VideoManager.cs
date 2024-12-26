using System;
using Rainbow.Model;
using Rainbow.WebRTC;
using Rainbow.WebRTC.Abstractions;
using Rainbow.WebRTC.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Class that handles camera and share videos.
    /// It is configurable to either attach to the first incoming contact (and release when that contact leaves) or use a given contact id and only accept data from that contact.
    /// </summary>
    public class VideoManager : BaseStartOrEnabled
    {
        // It would be nice to expose textures instead, but RemoveVideoDisplay only allows for RawImage
        #region inspector modifiable 

        [Tooltip("The output for a remote video. Can be null, if not used")]
        public RawImage VideoImage;

        [Tooltip("The output for a remote share. Can be null, if not used")]
        public RawImage SharingImage;

        [Tooltip("If set to true and no user ID is currently set, the video manager will associate with the first media publisher. Otherwise, the current ID is compared")]
        public bool AllowFirstConnection = false;

        public bool AutoHideImage = true;

        #endregion inspector modifiable 

        #region public

        /// <summary>
        /// Get the currently set user id or null, if it doesn't exist
        /// </summary>
        public string UserId
        {
            get => m_userId; private set => m_userId = value;
        }
        [SerializeField]
        private string m_userId = null;

        /// <summary>
        /// Sets the user id to the given one. This will retrieve existing video streams, if available and update elements accordingly
        /// </summary>
        /// <param name="id"></param>
        public void SetUserId(string id)
        {
            // if the user changed, reset tracks
            if (UserId != id)
            {
                SetTrack(remoteVideo, null);
                SetTrack(remoteSharing, null);
            }
            UserId = id;

            // Id was set from the outside, so it does not come from a publication
            gotIdFromPublication = false;

            // if the user already published media, this will take it

            SetTracksFromModel();
        }

        #endregion // public

        #region events
        /// <summary>
        /// Handler for remote video active changed events
        /// </summary>
        /// <param name="manager">The video manager that spawned this event</param>
        /// <param name="active">True, if the remote video is active, false otherwise</param>
        public delegate void RemoteVideoActiveChangedHandler(VideoManager manager, bool active);

        /// <summary>
        /// Event called when a remote video changes its activity status
        /// </summary>
        public event RemoteVideoActiveChangedHandler OnRemoteVideoActiveChanged;

        /// <summary>
        /// Handler for remote share active changed events
        /// </summary>
        /// <param name="manager">The video manager that spawned this event</param>
        /// <param name="active">True, if the remote share is active, false otherwise</param>
        public delegate void RemoteSharingActiveChangedHandler(VideoManager manager, bool active);

        /// <summary>
        /// Event called when a remote share changes its activity status
        /// </summary>
        public event RemoteSharingActiveChangedHandler OnRemoteSharingActiveChanged;

        // TODO more events
        // Maybe let callbacks return a RawImage on Track initialization so you can more easily hook up data dynamically
        #endregion // events

        /// <summary>
        /// Whether or not the remote video is active
        /// </summary>
        public bool RemoteVideoActive
        {
            get; private set;
        }

        /// <summary>
        /// Whether or not the remote share is active
        /// </summary>
        public bool RemoteShareActive
        {
            get; private set;
        }

        private RemoteVideoDisplay remoteVideo;
        private RemoteVideoDisplay remoteSharing;

        private bool gotIdFromPublication = false;

        #region  unity lifecycle

        protected override void OnStartOrEnable()
        {
            if (remoteVideo == null)
            {
                CreateRemoteVideo();
            }

            if (remoteSharing == null)
            {
                CreateRemoteSharing();
            }

            ConnectionModel.OnRemoteTrack += OnRemoteTrack;
            ConnectionModel.OnRemoteMediaPublicationUpdated += OnRemoteMediaPublicationUpdated;

            SetTracksFromModel();
        }

        void OnDisable()
        {
            ConnectionModel.OnRemoteTrack -= OnRemoteTrack;
            ConnectionModel.OnRemoteMediaPublicationUpdated -= OnRemoteMediaPublicationUpdated;
        }

        #endregion  // unity lifecycle

        #region internal

        private void SetTracksFromModel()
        {
            if (UserId == null)
            {
                return;
            }

            // this could be called before the connection model exists, so we guard against that
            ConnectionModel model = ConnectionModel.Instance;

            if (model != null)
            {

                SetTrack(remoteVideo, model.GetStreamVideoTrack(UserId));

                SetTrack(remoteSharing, model.GetStreamSharingTrack(UserId));
            }
            else
            {
                SetTrack(remoteVideo, null);

                SetTrack(remoteSharing, null);
            }
        }

        private void CreateRemoteVideo()
        {
            if (VideoImage == null || remoteVideo != null)
            {
                return;
            }
            remoteVideo = new(null, VideoImage);
            remoteVideo.ActiveChanged += (active) =>
            {
                RemoteVideoActive = active;
                OnRemoteVideoActiveChanged?.Invoke(this, active);
            };
            var currentImage = VideoImage;
            remoteVideo.TextureChanged += (tex) =>
            {
                if (currentImage.TryGetComponent(out AspectRatioFitter fitter))
                {
                    fitter.aspectRatio = (float)tex.width / (float)tex.height;
                }
            };
        }

        private void CreateRemoteSharing()
        {
            if (SharingImage == null || remoteSharing != null)
            {
                return;
            }
            remoteSharing = new(null, SharingImage);
            remoteSharing.ActiveChanged += (active) =>
            {
                RemoteShareActive = active;
                OnRemoteSharingActiveChanged?.Invoke(this, active);
            };
            var currentImage = SharingImage;
            remoteSharing.TextureChanged += (tex) =>
            {
                if (currentImage.TryGetComponent(out AspectRatioFitter fitter))
                {
                    fitter.aspectRatio = (float)tex.width / (float)tex.height;
                }
            };
        }

        private void OnRemoteMediaPublicationUpdated(ConnectionModel model, MediaPublicationEventArgs e)
        {
            if (e.MediaPublication == null) { return; }

            // ignore publications from other sources
            if (string.IsNullOrEmpty(UserId) || UserId != e.MediaPublication.PublisherId)
            {
                return;
            }
            if (e.Status == MediaPublicationStatus.PEER_STOPPED)
            {
                try
                {

                    if (e.MediaPublication.Media == Call.Media.VIDEO)
                    {
                        SetTrack(remoteVideo, null);

                    }
                    else if (e.MediaPublication.Media == Call.Media.SHARING)
                    {
                        SetTrack(remoteSharing, null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                // removed all video from a publication that we got via first event -> reset
                if (gotIdFromPublication && remoteVideo?.Track == null && remoteSharing?.Track == null)
                {
                    // don't use public accessor, we already 
                    UserId = null;
                    gotIdFromPublication = false;
                }
            }
        }

        private void OnRemoteTrack(ConnectionModel model, string callId, MediaStreamTrackDescriptor descriptor)
        {
            if (!string.IsNullOrEmpty(UserId))
            {
                if (UserId != descriptor.PublisherId)
                {
                    return;
                }
            }
            else
            {
                // if the user id is empty, we take the one of the published media, if this operation is allowed
                // otherwise nothing is done
                if (AllowFirstConnection)
                {
                    UserId = descriptor.PublisherId;
                    gotIdFromPublication = true;
                }
                else
                {
                    return;
                }

            }

            if (descriptor.Media == Call.Media.VIDEO)
            {

                if (remoteVideo == null)
                {
                    CreateRemoteVideo();
                }

                SetTrack(remoteVideo, ConnectionModel.Instance.GetStreamVideoTrack(UserId));
            }
            if (descriptor.Media == Call.Media.SHARING)
            {

                if (remoteSharing == null)
                {
                    CreateRemoteSharing();
                }
                SetTrack(remoteSharing, ConnectionModel.Instance.GetStreamSharingTrack(UserId));

            }
        }

        private void SetTrack(RemoteVideoDisplay display, IMediaStreamTrack track)
        {
            if (display == null)
            {
                return;
            }
            display.Track = (IVideoStreamTrack)track;
            display.Active = track != null;

            // Hide image
            if (display.Track == null && AutoHideImage)
            {
                var col = display.Image.color;
                display.Image.texture = null;
                col.a = 0.0f;
                display.Image.color = col;

            }
            else
            {

                var col = display.Image.color;
                col.a = 1.0f;
                display.Image.color = col;
            }
        }

        #endregion // internal

    }
} // end namespace Cortex