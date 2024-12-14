using Rainbow.WebRTC.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityWebRTC = Unity.WebRTC;
using Unity.WebRTC;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Rainbow.WebRTC.Unity
{
    internal class PeerConnection : IPeerConnection
    {
        RTCPeerConnection wrapped;
        ILogger log;
        private String? logPrefix;
        List<IAudioStream> audioStreams;
        List<IVideoStream> videoStreams;
        private static VideoStreamComparer comparer = new VideoStreamComparer();
        private SignalingState signalingstate;
        private bool mustForceRaiseSignalingState = false;

        internal PeerConnection(Configuration configuration, String? loggerPrefix)
        {
            RTCConfiguration config = Helpers.RTCConfiguration(configuration);
            wrapped = new RTCPeerConnection(ref config);
            this.logPrefix = loggerPrefix;
            log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
            subscribeToEvents();
            audioStreams = new();
            videoStreams = new();
            mustForceRaiseSignalingState = false;
        }

        private SignalingState signalingState
        {
            set
            {
                try
                {
                    // If signaling is stable but we're not in connected state: don't raise signaligstatechanged
                    // but we want to do it as soon as we are connected.
                    // This is because the sdk relies on signalingstate == stable to change a call status from connecting to active
                    // (instead of relying on ConnectionState = connected)
                    if ((value == SignalingState.stable) && (wrapped.ConnectionState != RTCPeerConnectionState.Connected))
                    {
                        mustForceRaiseSignalingState = true;
                        return;
                    }
                        
                    if (value != signalingstate || mustForceRaiseSignalingState )
                    {
                        mustForceRaiseSignalingState = false;
                        signalingstate = value;                      
                        this.OnSignalingStateChange?.Invoke(signalingstate);
                    } 
                }
                catch (Exception ex)
                {
                    log.LogError($"{ex.Message}\n{ex.StackTrace}");
                }

            }
        }

        #region EVENTS

        public event NegotiationNeededDelegate OnNegotiationNeeded;
        public event IceCandidateDelegate OnIceCandidate;
        public event IceCandidateErrorDelegate OnIceCandidateError; // not supported
        public event IceConnectionStateDelegate OnIceConnectionStateChange;
        public event IceGatheringStateDelegate OnIceGatheringstateChange;
        public event SignalingStateChangeDelegate OnSignalingStateChange;
        public event PeerConnectionStateDelegate OnConnectionStateChange;
        public event TrackDelegate OnTrack;
        public event DataChannelDelegate OnDataChannel;

        private void subscribeToEvents()
        {
            if (wrapped != null)
            {
                wrapped.OnIceCandidate = onIceCandidate;
                wrapped.OnConnectionStateChange = onConnectionStateChange;
                wrapped.OnIceConnectionChange = onIceConnectionStateChange;
                wrapped.OnNegotiationNeeded = onNegotiationNeeded;
                wrapped.OnIceGatheringStateChange = onIceGatheringStateChange;
                wrapped.OnTrack = onTrack;
            }
        }

        private void unsubscribeToEvents()
        {
            if (wrapped != null)
            {
                wrapped.OnIceCandidate -= onIceCandidate;
                wrapped.OnConnectionStateChange -= onConnectionStateChange;
                wrapped.OnIceConnectionChange -= onIceConnectionStateChange;
                wrapped.OnNegotiationNeeded -= onNegotiationNeeded;
                wrapped.OnIceGatheringStateChange -= onIceGatheringStateChange;
                wrapped.OnTrack -= onTrack;
            }
        }

        private void onIceCandidate(RTCIceCandidate candidate)
        {
            try
            {
                log.LogDebug($"onIceCandidate({candidate.Candidate})");
                this.OnIceCandidate?.Invoke(Helpers.IceCandidate(candidate));

            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void onConnectionStateChange(RTCPeerConnectionState state)
        {
            try
            {
                log.LogDebug($"onConnectionStateChange({state})");
                this.OnConnectionStateChange?.Invoke(Helpers.PeerConnectionState(state));
                if (wrapped != null)
                {
                    this.signalingState = Helpers.SignalingState(wrapped.SignalingState);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void onIceConnectionStateChange(RTCIceConnectionState state)
        {
            try
            {
                log.LogDebug($"onIceConnectionStateChange({state})");
                this.OnIceConnectionStateChange?.Invoke(Helpers.IceConnectionState(state));
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void onIceGatheringStateChange(RTCIceGatheringState state)
        {
            try
            {
                log.LogDebug($"onIceGatheringStateChange({state})");
                this.OnIceGatheringstateChange?.Invoke(Helpers.IceGatheringState(state));
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void onNegotiationNeeded()
        {
            try
            {
                // log.LogDebug($"onNegotiationNeeded()");
                this.OnNegotiationNeeded?.Invoke();
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }
        private void onRemoteTrack(IMediaStreamTrack track)
        {
            try
            {
                if( track == null )
                {
                    return;
                }                
                log.LogDebug($"will invoke OnTrack({track} {track.Kind} {track.Id}) on {OnTrack}");
                this.OnTrack?.Invoke(track);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        // onTrack is received on peer connection after a set RemoteDescrpiption is applied
        // this is processed internally by adding the trackevent's track in a stream, and wait for the OnAddTrack signal
        // on this stream, which returns the track in a state the sdk and application can use it.
        private void onTrack(RTCTrackEvent e)
        {
            log.LogDebug($"Received onTrack of a track: {e.Track.Kind} {e.Track.Id}");
            try
            {
                //if (e.Transceiver.Direction == RTCRtpTransceiverDirection.RecvOnly)
                //{
                //    e.Transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
                //}
                StreamInfos($"RemoteTrack Received kind {e.Track.Kind} {e.Track.Id}");
                if (e.Track is AudioStreamTrack audiotrack)
                {
                    RainbowAudioStream audioStream = addAudioStream(new RainbowAudioStream(e.Transceiver, logPrefix), TriState.Unknown, TriState.Yes);
                    audioStream.SetRemoteTrack(e.Track);
                }
                else if (e.Track is VideoStreamTrack videotrack)
                {
                    // addVideoStream(new RainbowVideoStream(e.Transceiver, logPrefix), TriState.Unknown, TriState.Yes);
                    RainbowVideoStream videoStream = addVideoStream(new RainbowVideoStream(e.Transceiver, logPrefix), TriState.Unknown, TriState.Yes);
                    videoStream.SetRemoteTrack(e.Track);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }
        #endregion EVENTS

        public PeerConnectionState ConnectionState { get { return Helpers.PeerConnectionState(wrapped.ConnectionState); } }

        public IceConnectionState IceConnectionState { get { return Helpers.IceConnectionState(wrapped.IceConnectionState); } }

        public List<IAudioStream> AudioStreams => audioStreams;

        public List<IVideoStream> VideoStreams
        {
            get
            {
                StreamInfos("Returned by VideoStreams:");
                return videoStreams;
            }
        }

        // This is only for SipSorcery.
        public bool AddRemoteTrack(int media)
        {
            return true;
        }
        public SignalingState SignalingState { get { return Helpers.SignalingState(wrapped.SignalingState); } }

        PeerConnectionState? IPeerConnection.ConnectionState => Helpers.PeerConnectionState(wrapped.ConnectionState);

        SignalingState? IPeerConnection.SignalingState => Helpers.SignalingState(wrapped.SignalingState);

        IceConnectionState? IPeerConnection.IceConnectionState => Helpers.IceConnectionState(wrapped.IceConnectionState);

        public List<IDataChannel> DataChannels => throw new NotImplementedException();

        public void AddIceCandidate(IceCandidateInit rtcIceCandidate)
        {
            try
            {
                log.LogDebug($"addIceCandidate {rtcIceCandidate.Candidate}");
                RTCIceCandidateInit candidateInit = Helpers.RtcIceCandidateInit(rtcIceCandidate);
                RTCIceCandidate candidate = new RTCIceCandidate(candidateInit);

                UnityExecutor.Execute(() => wrapped.AddIceCandidate(candidate));

            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public void Close(string reason = "normal")
        {
            try
            {
                UnityExecutor.Execute(() =>
                {
                    if (wrapped != null)
                    {
                        log.LogDebug("PeerConnection Close");
                        unsubscribeToEvents();
                        disposeStreams();
                        if( wrapped.ConnectionState == RTCPeerConnectionState.Connected )
                            wrapped.Close();
                    }
                    wrapped = null;
                });
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public void CreateAnswer(Action<SessionDescriptionInit> answerCb)
        {
            log.LogDebug("CreateAnswer called");
            UnityExecutor.Execute(createAnswerCoroutine(answerCb));
        }

        private IEnumerator createAnswerCoroutine(Action<SessionDescriptionInit> answerCb)
        {
            yield return 0;

            RTCSessionDescriptionAsyncOperation op = null;
            try
            {
                op = wrapped.CreateAnswer();
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
                yield break;
            }
            yield return op;

            try
            {
                SessionDescriptionInit answer = Helpers.SessionDescriptionInit(op.Desc);
                log.LogDebug($"CreateAnswer returned \n{answer.SDP}");
                answerCb(answer);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public void CreateOffer(Action<SessionDescriptionInit> offerCb)
        {
            UnityExecutor.Execute(createOfferCoroutine(offerCb));
        }

        private IEnumerator createOfferCoroutine(Action<SessionDescriptionInit> offerCb)
        {
            yield return 0;
            RTCSessionDescriptionAsyncOperation op = null;
            foreach(var videoStream in VideoStreams)
            {
                var rainbowVideoStream = videoStream as RainbowVideoStream;
                if( rainbowVideoStream.LocalTrack != null )
                {
                    var localTrack = rainbowVideoStream.LocalTrack as RainbowMediaStreamTrack;
                    if( localTrack.isEmpty) {
                        log.LogDebug("Would create an offer containing an empty stream track");
                        RemoveTrack( localTrack );
                    }
                }
            }

            try
            {
                op = wrapped.CreateOffer();
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
            yield return op;

            SessionDescriptionInit offer = Helpers.SessionDescriptionInit(op.Desc);

            try
            {
                offerCb(offer);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
            log.LogDebug($"Unity.PeerConnection CreateOfferCoroutine \n{offer.SDP}");
        }

        public void Dispose()
        {
        }

        public void SetLocalDescription(SessionDescriptionInit init, Action callback)
        {
            RTCSessionDescription offer = Helpers.RTCSessionDescriptionInit(init);
            log.LogDebug($"SetLocalDescription\n{offer.sdp}");

            UnityExecutor.Execute(setLocalDescriptionCoroutine(init, callback));
        }

        IEnumerator setLocalDescriptionCoroutine(SessionDescriptionInit sdp, Action callback)
        {
            yield return 0;
            RTCSetSessionDescriptionAsyncOperation op = null;
            StreamInfos("Before SetLocalDescription");
            try
            {
                RTCSessionDescription sessionDescription = Helpers.RTCSessionDescriptionInit(sdp);
                op = wrapped.SetLocalDescription(ref sessionDescription);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }

            yield return op;
            
            handleNewTransceivers();
            videoStreams.Sort(comparer);
            if (op.IsError)
            {
                log.LogError($"SetLocalDescription failed {op.Error.errorType} {op.Error.message} signaling state: {wrapped.SignalingState} {sdp?.Type} \n {sdp?.SDP} ");
            }
            //else
            //    log.LogDebug("SetLocalDescription done");
            // TODO : manage failures..            
            StreamInfos("After SetLocalDescription");
            handleRemoteTracksRemoved();
            callback();
            signalingState = Helpers.SignalingState(wrapped.SignalingState);
        }

        public void SetRemoteDescription(SessionDescriptionInit offer, Action callback)
        {
            RTCSessionDescription remoteOffer = Helpers.RTCSessionDescriptionInit(offer);
            UnityExecutor.Execute(setRemoteDescriptionCoroutine(offer, callback));
        }

        IEnumerator setRemoteDescriptionCoroutine(SessionDescriptionInit sdp, Action callback)
        {
            yield return 0;
            RTCSessionDescription sessionDescription = Helpers.RTCSessionDescriptionInit(sdp);
            StreamInfos($"SetRemoteDescription {sdp.SDP}");
            var op = wrapped.SetRemoteDescription(ref sessionDescription);
            yield return op;
            if (op.IsError)
            {
                log.LogError($"SetRemoteDescription failed {op.Error.errorType} {op.Error.message}  {sessionDescription.type} \n {sessionDescription.sdp} ");
            }

            videoStreams.Sort(comparer);

            // TODO : manage failures..            
            StreamInfos("After SetRemoteDescription");
            handleNewTransceivers();
            handleRemoteTracksRemoved();
            callback();
            signalingState = Helpers.SignalingState(wrapped.SignalingState);
        }

        public bool AddTrack(IMediaStreamTrack track)
        {

            RainbowMediaStreamTrack realTrack = track as RainbowMediaStreamTrack;
            if(track == null )
            {
                log.LogError("AddTrack called with a null track");
                return false;
            }

            StreamInfos($"BEFORE AddTrack track {track.Kind} id {track.Id} isempty:{realTrack.isEmpty} ");
            RTCRtpSender sender;
            try
            {
                switch (track.Kind)
                {
                    case Abstractions.TrackKind.Audio:
                        sender = wrapped.AddTrack(realTrack.Instance);
                        foreach (var t in wrapped.GetTransceivers())
                        {
                            if (t.Sender == sender)
                            {
                                var codecsAudio = RTCRtpSender.GetCapabilities(UnityWebRTC.TrackKind.Audio).codecs;
                                var opusCodec = codecsAudio.Where(codec => { return codec.mimeType == "audio/opus"; });
                                RTCErrorType err;
                                err = t.SetCodecPreferences(opusCodec.ToArray());

                                addAudioStream(new RainbowAudioStream(t, logPrefix), TriState.Yes, TriState.Unknown);
                                return true;
                            }
                        }
                        break;

                    case Abstractions.TrackKind.Video:
                        bool foundStream = false;
                        RainbowMediaStream rainbowVideoStream = null;
                        foreach (var vs in VideoStreams)
                        {
                            if (vs.LocalTrack == null)
                            {
                                rainbowVideoStream = vs as RainbowMediaStream;
                                // StreamInfos($"AddTrack: found a stream with no localtrack .. our plan is to replace track on stream {rainbowVideoStream.Instance.Mid}:");
                                foundStream = true;
                                // StreamInfos($"Addtrack will replace null track on stream {rainbowVideoStream.Mid}");
                                vs.LocalTrack = realTrack;
                                // log.LogDebug($"Addtrack : on stream {rainbowVideoStream.Mid} localtrack is now {vs.LocalTrack}..");
                                break;
                            }
                        }
                        if (foundStream)
                        {
                            return true;
                        }

                        sender = wrapped.AddTrack(realTrack.Instance);
                        foreach (var transceiver in wrapped.GetTransceivers())
                        {
                            if (transceiver.Sender == sender)
                            {
                                // log.LogDebug($"Before AddVideoStream, Transceiver : {transceiver.Direction} {transceiver.CurrentDirection}");
                                addVideoStream(new RainbowVideoStream(transceiver, logPrefix), TriState.Yes, TriState.Unknown, track as RainbowMediaStreamTrack);
                                // log.LogDebug($"After AddVideoStream, Transceiver : {transceiver.Direction} {transceiver.CurrentDirection}");                                
                                return true;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                StreamInfos($"After AddTrack track {track.Kind} id {track.Id} isempty:{realTrack.isEmpty} ");
            }
            return false;
        }

        public bool RemoveTrack(IMediaStreamTrack streamTrack)
        {
            // log.LogDebug("RemoveTrack ");
            try
            {
                if (streamTrack is RainbowMediaStreamTrack track)
                {
                    return removeTrackFromStream(track);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
            return false;
        }
        private void disposeStreams()
        {
            try
            {
                disposeVideoStreams();
                disposeAudioStreams();

            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        #region VIDEO AND AUDIO STREAMS LIST MANAGEMENT

        private void disposeAudioStreams()
        {
            foreach (IAudioStream iaudioStream in audioStreams)
            {
                RainbowAudioStream audioStream = iaudioStream as RainbowAudioStream;
                audioStream.Dispose();
            }
            audioStreams.Clear();
        }
        private void disposeVideoStreams()
        {
            foreach (IVideoStream ivideoStream in videoStreams)
            {
                RainbowVideoStream videoStream = ivideoStream as RainbowVideoStream;
                videoStream.Dispose();
            }
            videoStreams.Clear();
        }

        private void updateVideoParameters(RTCRtpSendParameters parameters, Dictionary<string, Object> options)
        {
            if (options == null) return;
            bool hasMaxFr = false, hasMaxBr = false, hasMinBr = false, hasScaleRes = false;
            uint maxBrVal = 0, minBrVal = 0, maxFrVal = 0;
            double scaleResolutionDownVal = 0;

            if (options.ContainsKey(VideoDeviceOptionsNames.MaxFramerate))
            {
                hasMaxFr = true;
                maxFrVal = (uint)(options[VideoDeviceOptionsNames.MaxFramerate]);
            }
            if (options.ContainsKey(VideoDeviceOptionsNames.MaxBitrate))
            {
                hasMaxBr = true;
                maxBrVal = (uint)(options[VideoDeviceOptionsNames.MaxBitrate]);
            }

            if (options.ContainsKey(VideoDeviceOptionsNames.MinBitrate))
            {
                hasMinBr = true;
                minBrVal = (uint)(options[VideoDeviceOptionsNames.MinBitrate]);
            }
            if (options.ContainsKey(VideoDeviceOptionsNames.ScaleResolutionDownBy))
            {
                hasScaleRes = true;
                scaleResolutionDownVal = (double)(options[VideoDeviceOptionsNames.ScaleResolutionDownBy]);
            }
            if( (hasMaxFr || hasMaxBr || hasMinBr || hasScaleRes) == false )
            {
                return;
            }

            foreach (var encoding in parameters.encodings)
            {
                if (hasMaxFr)
                {
                    log.LogError("Set maxFrameRate");
                    encoding.maxFramerate = maxFrVal;
                }
                if (hasMaxBr)
                {
                    log.LogError("Set maxBitRate");
                    encoding.maxBitrate = maxBrVal;
                }
                if (hasMinBr)
                {
                    log.LogError("Set minBitRate");
                    encoding.minBitrate = minBrVal;
                }
                if (hasScaleRes)
                {
                    log.LogError("Set scaleResDownBy");
                    encoding.scaleResolutionDownBy = scaleResolutionDownVal;
                }
            }
        }

        internal RainbowVideoStream addVideoStream(IVideoStream param, TriState isSending, TriState isReceiving, RainbowMediaStreamTrack localTrack=null)
        {
            bool alreadyKnown = false;

            RainbowVideoStream stream = param as RainbowVideoStream;
            RainbowVideoStream result = stream;

            foreach (var aStream in videoStreams)
            {
                if (aStream is RainbowVideoStream videoStream)
                {
                    if (videoStream.Instance == stream.Instance)
                    {
                        alreadyKnown = true;
                        result = videoStream;
                        stream = videoStream;
                        break;
                    }
                }
            }

            if (isSending != TriState.Unknown)
            {
                stream.IsSending = isSending;
                if (stream.IsSending == TriState.Yes)
                    if( localTrack == null)
                    {
                        log.LogDebug($"Will add the sender track.. i.e. {stream.Instance.Sender.Track}.");
                        stream.LocalTrack = RainbowVideoStreamTrack.Wrap(stream.Instance.Sender.Track, logPrefix);
                    }
                    else
                    {
                        log.LogDebug($"Will add the passed local track.. i.e. {localTrack}.");
                        stream.LocalTrack = localTrack;
                    }
            }
            if (isReceiving != TriState.Unknown)
            {
                stream.IsReceiving = isReceiving;
                if (stream.IsReceiving == TriState.Yes)
                    stream.remoteTrack = RainbowVideoStreamTrack.Wrap(stream.Instance.Receiver.Track, logPrefix);
            }

            if (!alreadyKnown)
            {
                log.LogDebug($"This is a new stream.. prepare it");
                stream.OnRemoteTrack += onRemoteTrack;
                var codecsVideo = RTCRtpSender.GetCapabilities(UnityWebRTC.TrackKind.Video).codecs;
                RainbowVideoStreamTrack videoStreamTrack = (RainbowVideoStreamTrack)localTrack;

                RTCRtpSendParameters parameters = stream.Instance.Sender.GetParameters();

                if ( videoStreamTrack?.options != null )
                {
                    updateVideoParameters(parameters, videoStreamTrack.options);

                    // Set updated parameters.
                    stream.Instance.Sender.SetParameters(parameters);
                }

                // var vp8Codec = codecsVideo.Where(codec => { return codec.mimeType == "video/vp8"; });
                var h264codec = codecsVideo.Where(codec => { return codec.mimeType == "video/h264"; });
                RTCErrorType error;
                error = stream.Instance.SetCodecPreferences(h264codec.ToArray());
                if (error != RTCErrorType.None)
                {
                    log.LogError($"Failed to force codec: {error}");
                }

                //// Changing framerate of all encoders.
                //foreach (var encoding in parameters.encodings)
                //{
                //    // Change encoding frequency 60 frame per second.
                //    encoding.maxFramerate = 20;
                //}


                videoStreams.Add(stream);

            }
            else
            {
                RainbowVideoStreamTrack videoStreamTrack = (RainbowVideoStreamTrack)localTrack;

                RTCRtpSendParameters parameters = stream.Instance.Sender.GetParameters();

                if (videoStreamTrack?.options != null)
                {
                    updateVideoParameters(parameters, videoStreamTrack.options);

                    // Set updated parameters.
                    stream.Instance.Sender.SetParameters(parameters);
                }

            }
            return result;
        }

        internal RainbowAudioStream addAudioStream(IAudioStream param, TriState isSending, TriState isReceiving)
        {
            bool alreadyKnown = false;
            RainbowAudioStream stream = param as RainbowAudioStream;
            RainbowAudioStream result = stream;
            foreach (var storedStream in audioStreams)
            {
                if (storedStream is RainbowAudioStream audioStream)
                {
                    if (audioStream.Instance == stream.Instance)
                    {
                        stream = audioStream;
                        result = audioStream;
                        alreadyKnown = true;
                        break;
                    }
                }
            }

            if (isSending != TriState.Unknown)
            {
                stream.IsSending = isSending;
                if (isSending == TriState.Yes)
                    stream.LocalTrack = RainbowMediaStreamTrack.Wrap(stream.Instance.Sender.Track, logPrefix);
            }

            if (isReceiving != TriState.Unknown)
            {
                stream.IsReceiving = isReceiving;
                if (isReceiving == TriState.Yes)
                    stream.remoteTrack = RainbowMediaStreamTrack.Wrap(stream.Instance.Receiver.Track, logPrefix);
            }

            if (!alreadyKnown)
            {
                audioStreams.Add(stream);
                stream.OnRemoteTrack += onRemoteTrack;
            }
            return result;
        }

        internal bool removeTrackFromStream(RainbowMediaStreamTrack track)
        {
            bool result = false;
            if (track.Instance.Kind == UnityWebRTC.TrackKind.Video)
            {
                foreach (var videostream in videoStreams)
                {
                    RainbowVideoStream stream = videostream as RainbowVideoStream;
                    if (stream.LocalTrack != null && (stream.LocalTrack as RainbowMediaStreamTrack).Instance == track.Instance)
                    {
                        // log.LogDebug($"Found Track: mid={stream.Instance.Mid}");
                        stream.RemoveLocalTrack();
                        RTCErrorType err = wrapped.RemoveTrack(stream.Instance.Sender);
                        if (err != RTCErrorType.None)
                        {
                            log.LogError($"removeTrackFromStream: Calling RemoveTrack returned err: {err}");
                        }
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        internal class VideoStreamComparer : IComparer<IVideoStream>
        {
            public int Compare(IVideoStream x, IVideoStream y)
            {
                return String.Compare(x.Mid, y.Mid);
            }
        }

        // After SetLocalDescription or SetRemoteDescription is called, new transceiver with a new mid can appear to 
        // be prepared to receive remote tracks.
        // If it happens, we need to add those in the videostreams.
        private void handleNewTransceivers()
        {
            if (wrapped.SignalingState != RTCSignalingState.Stable)
            {
                return;
            }
            foreach( var t in wrapped.GetTransceivers())
            {
                if(FindMediaStream(t) == null)
                {
                    // log.LogError($"handleNewTransceivers: the transceiver mid:{t.Mid} needs to be added in list of streams");
                    RainbowVideoStream videoStream = addVideoStream(new RainbowVideoStream(t, logPrefix), TriState.Unknown, TriState.Yes);
                }
            }
        }
        private void handleRemoteTracksRemoved()
        {
            // When local and remote descriptions have been set (i.e. signalling is stable, 
            // remove localTracks from the list of Streams if the transceiver's currentDescription is neither RecvOnly nor SendRecv
            if (wrapped.SignalingState != RTCSignalingState.Stable)
            {
                return;
            }

            foreach (var s in videoStreams)
            {
                var stream = s as RainbowMediaStream;
                bool StreamIsNotReceiving = (stream.Instance.CurrentDirection == RTCRtpTransceiverDirection.SendOnly || stream.Instance.CurrentDirection == RTCRtpTransceiverDirection.Inactive);
                if (StreamIsNotReceiving && stream.RemoteTrack != null)
                {
                    log.LogDebug($"handleRemoteTracksRemoved: Removed remote video track on stream {stream.Mid}");
                    stream.SetRemoteTrack(null);
                }
            }
            foreach (var s in audioStreams)
            {
                var stream = s as RainbowMediaStream;
                bool StreamIsNotReceiving = (stream.Instance.CurrentDirection == RTCRtpTransceiverDirection.SendOnly || stream.Instance.CurrentDirection == RTCRtpTransceiverDirection.Inactive);
                if (StreamIsNotReceiving && stream.RemoteTrack != null)
                {
                    stream.SetRemoteTrack(null);
                }
            }
        }
        internal RainbowMediaStream FindMediaStream(RTCRtpTransceiver transceiver)
        {
            foreach( var s in audioStreams)
            {
                var stream = s as RainbowMediaStream;
                if ( stream.Instance == transceiver )
                {
                    return stream;
                }
            }
            foreach (var s in videoStreams)
            {
                var stream = s as RainbowMediaStream;
                if (stream.Instance == transceiver)
                {
                    return stream;
                }
            }
            return null;
        }
        internal void StreamInfos(string title)
        {
            try
            {
                List<string> result = new();
                result.Add(title);
                result.Add("List of Audio Streams:");
                foreach (var s in audioStreams)
                {
                    var stream = s as RainbowMediaStream;
                    result.Add($"Mid: {stream.Instance.Mid} Dir: {stream.Instance.Direction} CurrentDirection: {stream.Instance.CurrentDirection} StreamDir: {stream.Direction} Local: {stream.LocalTrack} Remote: {stream.RemoteTrack} {stream.RemoteTrack?.Id}");
                }

                result.Add("List of Video Streams:");
                foreach (var s in videoStreams)
                {
                    var stream = s as RainbowMediaStream;
                    result.Add($"Mid: {stream.Instance.Mid} Dir: {stream.Instance.Direction} CurrentDirection: {stream.Instance.CurrentDirection} StreamDir: {stream.Direction} Local: {stream.LocalTrack} Remote: {stream.RemoteTrack} {stream.RemoteTrack?.Id}");
                }
                result.Add("List of transceivers:");
                foreach (var transceiver in wrapped.GetTransceivers())
                {
                    result.Add($"Transceiver:  mid {transceiver.Mid} Dir: {transceiver.Direction} CurrentDirection: {transceiver.CurrentDirection}");
                }

                log.LogDebug(String.Join("\n", result));
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public IDataChannel CreateDataChannel(string label, DataChannelInit options = null)
        {
            throw new NotImplementedException();
        }

        public void CreateDataChannel(string label, DataChannelInit dataChannelInit = null, ConfigurationOptions options = null, Action<IDataChannel> callback = null)
        {
            throw new NotImplementedException();
        }

        #endregion VIDEO AND AUDIO STREAMS LIST MANAGEMENT
    }
}