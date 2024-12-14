using Rainbow.WebRTC;
using Rainbow.WebRTC.Abstractions;
using System.Collections.Generic;
using static Rainbow.Model.Call;

public class MediaStreamTrackCache
{
    // Start is called before the first frame update
    private RainbowController rainbow;
    private Dictionary<string, IMediaStreamTrack> videoTracks;
    private IMediaStreamTrack audioTrack;
    private IMediaStreamTrack sharingTrack;

    public MediaStreamTrackCache(RainbowController rainbow)
    {
        this.rainbow = rainbow;
        audioTrack = null;
        sharingTrack = null;
        videoTracks = new Dictionary<string, IMediaStreamTrack>();
        rainbow.Ready += Rainbow_Ready;
    }

    public IMediaStreamTrack GetAudioTrack()
    {
        return audioTrack;
    }

    public IMediaStreamTrack GetSharingTrack()
    {
        return sharingTrack;
    }

    public IMediaStreamTrack GetVideoTrack(string Id) { 
        if( videoTracks.ContainsKey(Id))
            return videoTracks[Id];
        return null;
    }

    private void Rainbow_Ready(bool isReadyAndConnected)
    {
        rainbow.RainbowWebRTC.OnTrack += RainbowWebRTC_OnTrack;
        rainbow.RainbowWebRTC.CallUpdated += RainbowWebRTC_CallUpdated;
        rainbow.RainbowWebRTC.OnMediaPublicationUpdated += RainbowWebRTC_OnMediaPublicationUpdated;
    }

    private void RainbowWebRTC_OnMediaPublicationUpdated(object sender, Rainbow.WebRTC.MediaPublicationEventArgs e)
    {
        if (e.Status != MediaPublicationStatus.PEER_STOPPED)
        {
            return;
        }
        switch (e.MediaPublication.Media)
        {


            case Media.AUDIO:
                audioTrack = null;
                break;

            case Media.SHARING:
                sharingTrack = null;
                break;
            default:

                if (videoTracks.ContainsKey(e.MediaPublication.PublisherId))
                {
                    videoTracks.Remove(e.MediaPublication.PublisherId);
                }
                break;
        }
    }

    private void RainbowWebRTC_CallUpdated(object sender, Rainbow.Events.CallEventArgs e)
    {
        if (e.Call == null)
            return;
        switch( e.Call.CallStatus)
        {
            case Status.RELEASING:
            case Status.UNKNOWN:
                ClearCache();
                break;
        } 
    }

    private void RainbowWebRTC_OnTrack(string callId, Rainbow.WebRTC.MediaStreamTrackDescriptor mediaStreamTrackDescriptor)
    {
        switch (mediaStreamTrackDescriptor.Media)
        {
            case Media.AUDIO:
                audioTrack = mediaStreamTrackDescriptor.MediaStreamTrack;
                break;

            case Media.SHARING:
                sharingTrack = mediaStreamTrackDescriptor.MediaStreamTrack;
                break;

            case Media.VIDEO:
            default:
                videoTracks[mediaStreamTrackDescriptor.PublisherId] = mediaStreamTrackDescriptor.MediaStreamTrack;
                break;
        }
    }

    
    public void ClearCache()
    {
        audioTrack = null;
        sharingTrack = null;
        videoTracks.Clear();
    }



}
