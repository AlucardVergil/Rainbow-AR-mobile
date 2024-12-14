using System;
using UnityWebRTC=Unity.WebRTC;
using Rainbow.WebRTC.Abstractions;


namespace Rainbow.WebRTC.Unity
{
    internal class Helpers
    {      
        internal static UnityWebRTC.RTCIceTransportPolicy IceTransportPolicy(IceTransportPolicy transportPolicy)
        {
            switch( transportPolicy)
            {
                case Abstractions.IceTransportPolicy.all:
                    return UnityWebRTC.RTCIceTransportPolicy.All;
                    
                default:
                    return UnityWebRTC.RTCIceTransportPolicy.Relay;                    
            }
        }

        internal static UnityWebRTC.RTCIceServer RTCIceServer( IceServer iceServer)
        {
            UnityWebRTC.RTCIceServer result = new UnityWebRTC.RTCIceServer();
            result.credential = iceServer.Credential;
            result.urls = new String[] { iceServer.Url };
            result.username = iceServer.UserName;
            return result;
        }

        internal static UnityWebRTC.RTCConfiguration RTCConfiguration(Configuration configuration )
        {
            UnityWebRTC.RTCConfiguration result = new UnityWebRTC.RTCConfiguration( );
            UnityWebRTC.RTCIceServer[] iceServers = new UnityWebRTC.RTCIceServer[configuration.IceServers.Count];
            int i = 0;
            foreach (var iceServer in configuration.IceServers)
            {
                iceServers[i++] = RTCIceServer(iceServer);
            }
            result.iceServers = iceServers;
            result.iceTransportPolicy = IceTransportPolicy(configuration.TransportPolicy);
            return result;
        }

        internal static  PeerConnectionState PeerConnectionState(UnityWebRTC.RTCPeerConnectionState state )
        {
            switch (state)
            {
                case UnityWebRTC.RTCPeerConnectionState.New:
                    return Abstractions.PeerConnectionState.@new;
                case UnityWebRTC.RTCPeerConnectionState.Connecting:
                    return Abstractions.PeerConnectionState.connecting;
                case UnityWebRTC.RTCPeerConnectionState.Closed:
                    return Abstractions.PeerConnectionState.closed;
                case UnityWebRTC.RTCPeerConnectionState.Failed:
                    return Abstractions.PeerConnectionState.failed;
                case UnityWebRTC.RTCPeerConnectionState.Disconnected:
                    return Abstractions.PeerConnectionState.disconnected;
                case UnityWebRTC.RTCPeerConnectionState.Connected:
                    return Abstractions.PeerConnectionState.connected;
                default:
                    return Abstractions.PeerConnectionState.disconnected;
            }
        }
        
        internal static IceConnectionState IceConnectionState(UnityWebRTC.RTCIceConnectionState state)
        {
            switch (state)
            {
                case UnityWebRTC.RTCIceConnectionState.New:
                    return Abstractions.IceConnectionState.@new;
                case UnityWebRTC.RTCIceConnectionState.Checking:
                    return Abstractions.IceConnectionState.checking;
                case UnityWebRTC.RTCIceConnectionState.Closed:
                    return Abstractions.IceConnectionState.closed;
                case UnityWebRTC.RTCIceConnectionState.Failed:
                    return Abstractions.IceConnectionState.failed;
                case UnityWebRTC.RTCIceConnectionState.Disconnected:
                    return Abstractions.IceConnectionState.disconnected;
                case UnityWebRTC.RTCIceConnectionState.Connected:
                    return Abstractions.IceConnectionState.connected;
                case UnityWebRTC.RTCIceConnectionState.Max:
                    return Abstractions.IceConnectionState.failed;
                case UnityWebRTC.RTCIceConnectionState.Completed:
                    return Abstractions.IceConnectionState.connected;
                default:
                    return Abstractions.IceConnectionState.disconnected;
            }
        }

        internal static UnityWebRTC.RTCIceCandidateInit RtcIceCandidateInit(IceCandidateInit init)
        {
            UnityWebRTC.RTCIceCandidateInit result = new UnityWebRTC.RTCIceCandidateInit();
            result.candidate = init.Candidate;
            result.sdpMid = init.Mid;
            result.sdpMLineIndex = init.LineIndex;
            return result;
        }

        internal static SdpType SdpType(UnityWebRTC.RTCSdpType type)
        {
            switch (type)
            {
                case UnityWebRTC.RTCSdpType.Offer:
                    return Abstractions.SdpType.offer;
                case UnityWebRTC.RTCSdpType.Answer:
                    return Abstractions.SdpType.answer;
                case UnityWebRTC.RTCSdpType.Pranswer:
                    return Abstractions.SdpType.pranswer;
                case UnityWebRTC.RTCSdpType.Rollback:
                    return Abstractions.SdpType.rollback;
                default:
                    return Abstractions.SdpType.rollback;
            }
        }
        internal static UnityWebRTC.RTCSdpType RtcSdpType(SdpType type)
        {
            switch( type)
            {
                case Abstractions.SdpType.answer:
                    return UnityWebRTC.RTCSdpType.Answer;
                case Abstractions.SdpType.offer:
                    return UnityWebRTC.RTCSdpType.Offer;
                case Abstractions.SdpType.pranswer:
                    return UnityWebRTC.RTCSdpType.Pranswer;
                case Abstractions.SdpType.rollback:
                    return UnityWebRTC.RTCSdpType.Rollback;
                default:
                    return UnityWebRTC.RTCSdpType.Rollback;
            }
        }

        internal static TrackKind TrackKind(UnityWebRTC.TrackKind kind)
        {
            switch(kind)
            {
                case UnityWebRTC.TrackKind.Audio: return Abstractions.TrackKind.Audio;
                default: return Abstractions.TrackKind.Video;

            }
        }

        internal static ReadyState ReadyState(UnityWebRTC.TrackState state)
        {
            switch (state) {
                case UnityWebRTC.TrackState.Live:
                    return Abstractions.ReadyState.Live;
                default:
                    return Abstractions.ReadyState.Ended;
            }
        }
        internal static UnityWebRTC.RTCSessionDescription RTCSessionDescriptionInit(SessionDescriptionInit desc)
        {
            UnityWebRTC.RTCSessionDescription result = new UnityWebRTC.RTCSessionDescription();
            result.type = RtcSdpType(desc.Type);
            result.sdp = desc.SDP;
            return result;
        }

        internal static SessionDescriptionInit SessionDescriptionInit(UnityWebRTC.RTCSessionDescription descriptionInit)
        {
            SessionDescriptionInit result = new();
            result.SDP = descriptionInit.sdp;
            result.Type = SdpType(descriptionInit.type);
            return result;
        }


        internal static IceCandidate IceCandidate(UnityWebRTC.RTCIceCandidate candidate)
        {
            IceCandidate result = new IceCandidate();
            result.SDPMid = candidate.SdpMid;
            result.Candidate = candidate.Candidate;
            return result;
        }

        internal static IceGatheringState IceGatheringState(UnityWebRTC.RTCIceGatheringState state)
        {
            switch(state)
            {
                case UnityWebRTC.RTCIceGatheringState.New:
                    return Abstractions.IceGatheringState.@new;
                case UnityWebRTC.RTCIceGatheringState.Complete:
                    return Abstractions.IceGatheringState.complete;
            }
            return Abstractions.IceGatheringState.gathering;
        }

        internal static SignalingState SignalingState(UnityWebRTC.RTCSignalingState state)
        {
            switch(state)
            {
                case UnityWebRTC.RTCSignalingState.Stable:
                    return Abstractions.SignalingState.stable;
                case UnityWebRTC.RTCSignalingState.HaveLocalOffer:
                    return Abstractions.SignalingState.have_local_offer;
                case UnityWebRTC.RTCSignalingState.HaveLocalPrAnswer:
                    return Abstractions.SignalingState.have_local_pranswer;
                case UnityWebRTC.RTCSignalingState.HaveRemoteOffer:
                    return Abstractions.SignalingState.have_remote_offer;
                case UnityWebRTC.RTCSignalingState.HaveRemotePrAnswer:
                    return Abstractions.SignalingState.have_remote_pranswer;
                case UnityWebRTC.RTCSignalingState.Closed:
                    return Abstractions.SignalingState.closed;
                default:
                    return Abstractions.SignalingState.closed;
            }
        }
    }
}