using Rainbow.Model;
using Rainbow.WebRTC.Abstractions;
using UnityEngine;


public interface IComCardLayoutManager 
{

    public delegate void LastPageChangedDelegate(int lastPage);
    public event LastPageChangedDelegate LastPageChanged;

    public int CurrentPage { get; }
    public int LastPage { get; }

    public void FreezeRefresh(bool frozen);
    public void NextPage();
    public void PrevPage();
    public void ClearAll();
    public void AddParticipant(Contact contact, bool isLocal = false);
    public void SetLocalSharingTrack(Contact c, IMediaStreamTrack track, bool IsInverted);
    public void SetRemoteSharingTrack(Contact c, IMediaStreamTrack track);
    public void SetRemoteVideoTrack(string publisherId, IMediaStreamTrack track);
    public void SetLocalVideoTrack(string publisherId, IMediaStreamTrack track, bool IsInverted);
    public void RemoveParticipant(Contact contact);
    public void RefreshLayout();
}

public abstract class AbstractComCardLayoutManager : MonoBehaviour, IComCardLayoutManager
{
    public abstract int CurrentPage { get; internal set; }
    public virtual int LastPage { get; internal set; }

    public event IComCardLayoutManager.LastPageChangedDelegate LastPageChanged;

    protected void RaiseLastPageChanged(int lastPage)
    {
        LastPageChanged?.Invoke(lastPage);
    } 

    public abstract void AddParticipant(Contact contact, bool isLocal = false);
    public abstract void ClearAll();
    public abstract void FreezeRefresh(bool frozen);
    public abstract void RefreshLayout();
    public virtual void NextPage()
    {
    }
    public virtual void PrevPage()
    {
    }
    public abstract void RemoveParticipant(Contact contact);
    public abstract void SetLocalSharingTrack(Contact c, IMediaStreamTrack track, bool IsInverted);
    public abstract void SetLocalVideoTrack(string publisherId, IMediaStreamTrack track, bool IsInverted);
    public abstract void SetRemoteSharingTrack(Contact c, IMediaStreamTrack track);
    public abstract void SetRemoteVideoTrack(string publisherId, IMediaStreamTrack track);
}