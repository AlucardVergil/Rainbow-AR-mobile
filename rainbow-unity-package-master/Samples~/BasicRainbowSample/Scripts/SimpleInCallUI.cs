using UnityEngine;
using Rainbow.WebRTC.Unity;
using TMPro;
using Rainbow.Events;
using Rainbow.Model;
using UnityEngine.UI;

[SelectionBase]
public class SimpleInCallUI : MonoBehaviour
{
    public RainbowController rainbow;
    private CanvasGroup canvasGroup;
    private Button hangup, mute, sharing, video;
    private TMP_Text callLabelText;
    private bool publishingVideo = false;
    private bool publishingSharing = false;



    private void ShowUI(bool show)
    {
        canvasGroup.alpha = show?1:0;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable = show;
    }

    private void Awake()
    {
        if( rainbow == null )
        {
            var rainbows = FindObjectsOfType<RainbowController>();
            if(rainbows.Length == 1)
            {
                rainbow = rainbows[0];
            }
            else
            {
                Debug.LogError("SimpleInCallUI is missing a reference to a RainbowController");
                return;
            }
        }

        rainbow.Ready += Rainbow_Ready;
        // Grab references to sub objects
        canvasGroup = GetComponent<CanvasGroup>();
        // Hide this ui
        ShowUI(false);

        hangup = transform.Find("Hangup").GetComponent<Button>();
        mute = transform.Find("Mute").GetComponent<Button>();
        sharing = transform.Find("StartStopSharing").GetComponent<Button>();
        video = transform.Find("StartStopVideo").GetComponent<Button>();
        callLabelText = transform.Find("CallLabel").GetComponent<TMP_Text>();

 
        // handle buttons
        video.onClick.AddListener(StartStopVideo);
        sharing.onClick.AddListener(StartStopSharing);
        hangup.onClick.AddListener(HangupCall);
        mute.onClick.AddListener(Mute);
    }
     
    private void OnDestroy()
    {
        rainbow.Ready -= Rainbow_Ready;
        if (rainbow.RainbowWebRTC != null)
        {
            rainbow.RainbowWebRTC.CallUpdated -= RainbowWebRTC_CallUpdated;
        }
    }
    #region button handlers
    private void HangupCall()
    {
        rainbow.HangupCall();
    }
    private void Mute()
    {
        mute.enabled = false;

        rainbow.ToggleAudioMuteCurrentCall(cb =>
        {
            UnityExecutor.Execute(() => { mute.enabled = true; });
        });
    }

    private string BuildLabelForCall(Call currentCall)
    {
        if (currentCall == null)
        {
            return "";
        }

        if (currentCall.IsConference)
        {
            Bubble bubble = rainbow.RainbowApplication.GetBubbles().GetBubbleByIdFromCache(currentCall.ConferenceId);
            return $"{bubble.Name}";
        }

        Contact RemotePeer = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(currentCall.PeerId);
        if (RemotePeer != null)
        {
            return $"{RemotePeer.DisplayName}";
        }
        return "";
    }
    private void RefreshUI()
    {
        var currentCall = rainbow.CurrentCall;
        if (currentCall != null)
        {
            string callDescription = BuildLabelForCall(currentCall);
            UnityExecutor.Execute(() =>
            {
                callLabelText.text = callDescription;
                SetButtonLabel(mute, currentCall.IsLocalAudioMuted ? "Unmute" : "Mute");
            });
        }
    }
    private void StartStopSharing()
    {
        sharing.enabled = false;

        if (publishingSharing)
        {
            rainbow.RemoveMediaFromCurrentCall(Call.Media.SHARING);
        }
        else
        {
            rainbow.AddMediaToCurrentCall(Call.Media.SHARING);
        }
    }

    private void StartStopVideo()
    {
        video.enabled = false;

        if (publishingVideo)
        {
            rainbow.RemoveMediaFromCurrentCall(Call.Media.VIDEO);
        }
        else
        {
            rainbow.AddMediaToCurrentCall(Call.Media.VIDEO);
        }

    }

    #endregion button handlers

    #region events Rainbow and RainbowWebRTC

    private void Rainbow_Ready(bool isReadyAndConnected)
    {
        rainbow.RainbowWebRTC.CallUpdated += RainbowWebRTC_CallUpdated;
    }
     
    private void RainbowWebRTC_CallUpdated(object sender, CallEventArgs e)
    {
        UnityExecutor.Execute(() =>
        {
            RainbowWebRTC_CallUpdatedInUnityThread(e);
        });
    }

    private void SetButtonLabel( Button button, string Label )
    {
        button.GetComponentInChildren<TMP_Text>().text = Label;
    }
    private void RainbowWebRTC_CallUpdatedInUnityThread(CallEventArgs e)
    {
        bool isVisible = (e.Call.CallStatus == Call.Status.ACTIVE) || (e.Call.CallStatus == Call.Status.CONNECTING);

        ShowUI(isVisible);

        if (!isVisible) // we are not in call anymore 
        {
            return;
        }

        sharing.enabled = ((e.Call.RemoteMedias & Call.Media.SHARING) == 0) || ((e.Call.LocalMedias & Call.Media.SHARING) != 0)  || (!e.Call.IsConference);
        video.enabled = true;

        RefreshUI();
        int localMedias = e.Call.LocalMedias;
        int remoteMedias = e.Call.RemoteMedias;
        publishingSharing = (localMedias & Call.Media.SHARING) != 0;
        SetButtonLabel(sharing, publishingSharing ? "Stop Sharing" : "Start Sharing");

        publishingVideo = (localMedias & Call.Media.VIDEO) != 0;
        SetButtonLabel(video, publishingVideo ? "Stop Video" : "Start Video");

        SetButtonLabel(mute, e.Call.IsLocalAudioMuted ? "Unmute" : "Mute");
    }
    #endregion events Rainbow and RainbowWebRTC
}
