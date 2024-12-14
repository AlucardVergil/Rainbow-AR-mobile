using Rainbow.WebRTC.Abstractions;
using System;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Unity.WebRTC;

namespace Rainbow.WebRTC.Unity
{
    internal delegate void RemoteTrackDelegate(RainbowMediaStreamTrack streamTrack);

    internal class RainbowMediaStream : IMediaStream
    {
        private string id;
        private string? loggerPrefix;
        private ILogger log;
        private RainbowMediaStreamTrack localTrack;
        private RTCRtpTransceiver transceiver;
        private MediaStream remoteStream;
        private TriState isSending;
        private TriState isReceiving;
        internal event RemoteTrackDelegate OnRemoteTrack;
        internal RainbowMediaStreamTrack remoteTrack;

        internal TriState IsSending
        {
            get { return isSending; }
            set { if (value != TriState.Unknown) isSending = value; }
        }

        internal TriState IsReceiving
        {
            get { return isReceiving; }
            set { if (value != TriState.Unknown) isReceiving = value; }
        }

        internal RainbowMediaStream(RTCRtpTransceiver transceiver, String? loggerPrefix)
        {
            id = Guid.NewGuid().ToString();
            this.transceiver = transceiver;
            isSending = TriState.Unknown;
            isReceiving = TriState.Unknown;
            this.loggerPrefix = loggerPrefix;
            log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
        }

        internal RTCRtpTransceiver Instance => transceiver;

        public string Id => id;

        public string Mid => Instance.Mid;

        internal void RemoveLocalTrack()
        {
            log.LogDebug("RemoveLocalTrack RemoveTrack");
            // Instance.Sender.Track.Stop();
            // Instance.Sender.ReplaceTrack(null);

            LocalTrack = null;
            IsSending = TriState.No;
        }


        public Direction Direction
        {
            get
            {
                if (isSending == TriState.Yes)
                {
                    if (isReceiving == TriState.Yes)
                    {
                        return Direction.SendRecv;
                    }
                    return Direction.SendOnly;
                }
                if (isReceiving == TriState.Yes)
                {
                    return Direction.RecvOnly;
                }
                return Direction.Inactive;
            }
        }

        public IMediaStreamTrack LocalTrack
        {
            get => localTrack;
            set
            {
                try
                {
                    RainbowMediaStreamTrack track = value as RainbowMediaStreamTrack;
                    localTrack = track;

                    if (track != null)
                    {
                        log.LogDebug($"IMediaStreamTrack SetLocalTrack Kind {value.Kind} called Mid={transceiver.Mid}");
                        MediaStreamTrack mediaStreamTrack = track.Instance;

                        if (transceiver.Sender != null)
                        {
                            if (true || mediaStreamTrack != transceiver.Sender.Track)
                            {
                                if (!transceiver.Sender.ReplaceTrack(mediaStreamTrack))
                                {
                                    log.LogError($"ReplaceTrack failed Mid={transceiver.Mid} kind={value.Kind} id={value.Id}");
                                } else
                                {
                                    log.LogDebug($"Replaced local track on transceiver: Mid={transceiver.Mid} kind={value.Kind} id={value.Id}");
                                }
                            }
                            // log.LogDebug($"UnityWebRTCVideoStream SetLocalTrack mid {transceiver.Mid} Direction {transceiver.Direction} calls ReplaceTrack {mediaStreamTrack}{transceiver.Sender.Track} on transceiver mid={transceiver.Mid}");
                            if (transceiver.Direction == RTCRtpTransceiverDirection.Inactive)
                            {
                                transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
                            }
                            else if (transceiver.Direction == RTCRtpTransceiverDirection.RecvOnly)
                            {
                                transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
                            }
                        }
                    }
                    else
                    {
                        log.LogDebug("Set MediaStreamTrack to a null track");
                    }

                }
                catch (Exception ex)
                {
                    log.LogError($"{ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public IMediaStreamTrack RemoteTrack
        {
            get => remoteTrack;
        }

        internal void SetRemoteTrack(MediaStreamTrack remoteTrack)
        {
            // Debug.Log("SetRemoteTrack");
            try
            {
                if (remoteStream != null)
                {
                    var tracks = remoteStream.GetTracks();
                    foreach (var track in tracks)
                    {
                        remoteStream.RemoveTrack(track);
                    }
                    remoteStream.OnAddTrack = null;
                    remoteStream.Dispose();
                    remoteStream = null;
                }
                if (remoteTrack == null && this.remoteTrack != null)
                {
                    try
                    {
                        RainbowMediaStreamTrack track = this.remoteTrack as RainbowMediaStreamTrack;
                        track.RaiseTrackRemoved();
                        this.remoteTrack = null;
                        return;

                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Exception removing remote track {ex.Message} {ex.StackTrace}");
                    }
                }

                remoteStream = new MediaStream();
                remoteStream.OnAddTrack = OnRemoteTrackAddedToMediaStream;
                remoteStream.AddTrack(remoteTrack);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }


        }
        internal void OnRemoteTrackAddedToMediaStream(MediaStreamTrackEvent e)
        {
            log.LogDebug("OnRemoteTrackAddedToMediaStream (Ontrack)== we added a track into the stream ");
            IsReceiving = TriState.Yes;

            if (e.Track is AudioStreamTrack)
            {
                remoteTrack = RainbowAudioStreamTrack.Wrap(e.Track, loggerPrefix);
            }
            else if (e.Track is VideoStreamTrack videoStreamTrack)
            {
                remoteTrack = RainbowVideoStreamTrack.Wrap(e.Track, loggerPrefix);
            }


            try
            {
                log.LogDebug($"The remote track is started: {remoteTrack.Kind}");
                this.OnRemoteTrack?.Invoke(remoteTrack);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }

        }

        public void Dispose()
        {
            try
            {
                // log.LogDebug("RainbowMediaStream Dispose"); 
                if (remoteStream != null)
                {
                    foreach (var track in remoteStream.GetTracks())
                    {
                        remoteStream.RemoveTrack(track);
                    }
                    remoteStream.Dispose();
                    remoteStream = null;
                }
                if (localTrack != null)
                {
                    this.localTrack.Dispose(); 
                    this.localTrack = null;
                }
                if (remoteTrack != null)
                {
                    this.remoteTrack.RaiseTrackRemoved();
                    this.remoteTrack = null;
                }
                if (transceiver != null)
                {
                    this.transceiver = null;
                }
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    internal class RainbowAudioStream : RainbowMediaStream, IAudioStream
    {
        internal RainbowAudioStream(RTCRtpTransceiver transceiver, string? loggerPrefix) : base(transceiver, loggerPrefix) { }
    }

    internal class RainbowVideoStream : RainbowMediaStream, IVideoStream
    {
        internal RainbowVideoStream(RTCRtpTransceiver transceiver, string? loggerPrefix) : base(transceiver, loggerPrefix) { }
    }
}
