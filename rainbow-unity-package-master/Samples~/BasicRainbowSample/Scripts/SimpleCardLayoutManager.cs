using UnityEngine;
using UnityEngine.UI;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using Rainbow.WebRTC.Abstractions;

public class SimpleCardLayoutManager : AbstractComCardLayoutManager
{
    private RawImage VideoImage;
    private RawImage SharingImage;
    private CanvasGroup VideoImageCanvasGroup;
    private CanvasGroup SharingImageCanvasGroup;
    private RemoteVideoDisplay RemoteVideoDisplaySharing;
    private RemoteVideoDisplay RemoteVideoDisplayVideo;
     
    public override int CurrentPage { get => throw new System.NotImplementedException(); internal set => throw new System.NotImplementedException(); }

    private void Awake()
    {
        VideoImage = transform.Find("VideoImage").GetComponent<RawImage>();
        VideoImageCanvasGroup = VideoImage.GetComponent<CanvasGroup>();
        RemoteVideoDisplayVideo = new(null, VideoImage);
        RemoteVideoDisplayVideo.ActiveChanged += active => {
            OnRemoteVideoDisplayActiveChanged(active, false);
        };
        SharingImage = transform.Find("SharingImage").GetComponent<RawImage>();
        SharingImageCanvasGroup = SharingImage.GetComponent<CanvasGroup>();
        RemoteVideoDisplaySharing = new(null, SharingImage);
        RemoteVideoDisplaySharing.ActiveChanged += active => { 
            OnRemoteVideoDisplayActiveChanged(active, true);
        };
    }
     
    public override void ClearAll()
    {
        
    }

    public override void AddParticipant(Contact contact, bool isLocal = false)
    {
    }

    public override void SetRemoteSharingTrack(Contact c, IMediaStreamTrack track)
    {
        // Debug.LogError($"Received remote SharingTrack for {c.DisplayName} {track}");
        RemoteVideoDisplaySharing.Track = (IVideoStreamTrack)track;
        RemoteVideoDisplaySharing.Active = (track != null);
        RemoteVideoDisplaySharing.Active = (track != null);
    }
    public override void SetLocalSharingTrack(Contact c, IMediaStreamTrack track, bool IsInverted)
    {
        // Debug.LogError($"Received local SharingTrack for {c.DisplayName} {track}");
    }
      

    private void OnRemoteVideoDisplayActiveChanged(bool active, bool isSharing)
    {
        if (isSharing)
        {
            SharingImageCanvasGroup.alpha = (active) ? 1:0;
        }
        else
        {
            VideoImageCanvasGroup.alpha = (active) ? 1:0;
        }
    }


    public override void SetRemoteVideoTrack(string publisherId, IMediaStreamTrack track)
    {
        Debug.LogError($"Received remote VideoTrack from {publisherId} {track}");

        RemoteVideoDisplayVideo.Track = (IVideoStreamTrack)track;
        RemoteVideoDisplayVideo.Active = (track != null);
    }

    public override void SetLocalVideoTrack(string publisherId, IMediaStreamTrack track, bool IsInverted)
    {
        // Debug.LogError($"Received local VideoTrack from {publisherId} {track}");
    }
     

    public override void RemoveParticipant(Contact contact)
    {
    }
       
     
    public override void RefreshLayout()
    {
    }

    void Update()
    {
         
    }

    public override void FreezeRefresh(bool frozen)
    {        
    }
}
