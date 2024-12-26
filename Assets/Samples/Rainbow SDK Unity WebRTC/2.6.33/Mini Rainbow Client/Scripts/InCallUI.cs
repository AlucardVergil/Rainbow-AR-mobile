using UnityEngine;
using Rainbow.WebRTC.Unity;
using TMPro;
using Rainbow.Events;
using Rainbow.Model;

[SelectionBase]
public class InCallUI : MonoBehaviour
{
    public RainbowController rainbow;
    private CanvasGroup canvasGroup;
    private RainbowGraphicsButton hangup, mute, prevPage, nextPage, sharing, video, collapse;
    private RectTransform cards;
    private TMP_Text callLabelText;
    private bool publishingVideo = false;
    private bool publishingSharing = false;
    private const float SizeButton = 30;
    private const float YButton = -10;
    private const float Margin = 10;
    private float[] XPositions = new float[] { 50, 120, 190, 310, 430, 500, 570 };
    private bool participantlistVisible = false;
    private RectTransform participantList = null;
    [SerializeField]
    private CurrentCallController CallController;

    private void Awake()
    {
        rainbow.Ready += Rainbow_Ready;
        // Grab references to sub objects
        canvasGroup = GetComponent<CanvasGroup>();
        // Hide this ui
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        cards = transform.Find("Cards").GetComponent<RectTransform>();
        participantList = transform.Find("ParticipantList")?.GetComponent<RectTransform>();
        Transform buttonParent = transform.Find("Cards/BackgroundButtons");
        callLabelText = transform.Find("Cards/CallLabel").GetComponent<TMP_Text>();

        int nButton = 0;

        mute = new RainbowGraphicsButton("mute", SizeButton, SizeButton, buttonParent);
        mute.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/mic--filled"), true))
            .AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/mic-off")));
        mute.InnerMargins = Margin;
        mute.State = 0;
        // mute.rectTransform.anchoredPosition3D = new Vector3(XPositions[nButton++], YButton, 0);
        // SetAnchors(mute);

        video = new RainbowGraphicsButton("video", SizeButton, SizeButton, buttonParent);
        video.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/video--filled"), true))
            .AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/video-off")));
        video.InnerMargins = Margin;
        video.State = 0;
        // video.rectTransform.anchoredPosition3D = new Vector3(XPositions[nButton++], YButton, 0);
        // SetAnchors(video);
        if( rainbow.CameraSharing != null || rainbow.WebCamSharingDevice != null )
        {
            sharing = new RainbowGraphicsButton("sharing", SizeButton, SizeButton, buttonParent);
            sharing.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/share_screen--filled"), true))
                .AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/share_screen-off")));
            sharing.InnerMargins = Margin;
            sharing.State = 0;
            // sharing.rectTransform.anchoredPosition3D = new Vector3(XPositions[nButton++], YButton, 0);
            // SetAnchors(sharing);
            sharing.button.onClick.AddListener(StartStopSharing);
        }

        hangup = new RainbowGraphicsButton("hangup", SizeButton, SizeButton, buttonParent);
        hangup.AddState(new RainbowButtonState("0xFFFFFFFF", "0xF3483FFF", Resources.Load<Sprite>("Images/SVG/hang-up")));
        hangup.InnerMargins = Margin;
        hangup.State = 0;
        // hangup.rectTransform.anchoredPosition3D = new Vector3(XPositions[nButton++], YButton, 0);
        // SetAnchors(hangup);

        prevPage = new RainbowGraphicsButton("prevPage", SizeButton, SizeButton, buttonParent);
        prevPage.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/navigate_previous")));
        prevPage.InnerMargins = Margin;
        prevPage.State = 0;
        // prevPage.rectTransform.anchoredPosition3D = new Vector3(XPositions[nButton++], YButton, 0);
        // SetAnchors(prevPage);

        nextPage = new RainbowGraphicsButton("nextPage", SizeButton, SizeButton, buttonParent);
        nextPage.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/navigate_next")));
        nextPage.InnerMargins = Margin;
        nextPage.State = 0;
        nextPage.rectTransform.anchoredPosition3D = new Vector3(XPositions[nButton++], YButton, 0);
        // SetAnchors(nextPage);
        if( participantList != null )
        {
            collapse = new RainbowGraphicsButton("ToggleCollapse", SizeButton, SizeButton, cards);
            collapse.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/collapse")))
                .AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/expand")));
            collapse.InnerMargins = Margin;
            collapse.State = 1;
            collapse.rectTransform.anchoredPosition3D = Vector3.zero;
            collapse.rectTransform.anchorMin = new Vector2(1,1);
            collapse.rectTransform.anchorMax = new Vector2(1,1);
            collapse.rectTransform.pivot = new Vector2(1,1);
            collapse.button.onClick.AddListener(() => { toggleParticipantList(); });
        }
        

        // handle buttons
        video.button.onClick.AddListener(StartStopVideo);        
        hangup.button.onClick.AddListener(HangupCall);
        mute.button.onClick.AddListener(Mute);
        nextPage.button.onClick.AddListener(() => { CallController.ComCardLayoutManager.NextPage(); });
        prevPage.button.onClick.AddListener(() => { CallController.ComCardLayoutManager.PrevPage(); });        
        rainbow.ConnectionChanged += Rainbow_ConnectionChanged;
    }

    private void toggleParticipantList()
    {
        if (participantList == null)
            return;

        participantlistVisible = !participantlistVisible;
        collapse.State = participantlistVisible ? 0 : 1;
        Rect rect = GetComponent<RectTransform>().rect;

        CanvasGroup ListCanvasGroup = participantList.gameObject.GetComponent<CanvasGroup>();
        ListCanvasGroup.alpha = participantlistVisible ? 1 : 0;
        ListCanvasGroup.blocksRaycasts = participantlistVisible;
        ListCanvasGroup.interactable = participantlistVisible;
        canvasGroup.ignoreParentGroups = participantlistVisible;
        if (!participantlistVisible)
        {
            cards.offsetMin = cards.offsetMax = Vector3.zero;
        } 
        else
        {
            cards.offsetMin = Vector3.zero;
            cards.offsetMax = new Vector3( -participantList.rect.width,0,0);
        }        
        CallController.ComCardLayoutManager.RefreshLayout();

    } 
    private void OnDestroy()
    {
        rainbow.ConnectionChanged -= Rainbow_ConnectionChanged;
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
        mute.button.enabled = false;

        rainbow.ToggleAudioMuteCurrentCall(cb =>
        {
            UnityExecutor.Execute(() => { mute.button.enabled = true; });
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
                mute.State = currentCall.IsLocalAudioMuted ? 1 : 0;
            });
        }
    }
    private void StartStopSharing()
    {
        sharing.button.enabled = false;

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
        video.button.enabled = false;

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

    private void Rainbow_ConnectionChanged(string connectionstate)
    {
        //bool isConnected = connectionstate == "connected";
        //Debug.Log("Rainbow_ConnectionStateChanged " + connectionstate );
        //canvasGroup.alpha = (isConnected ? 1 : 0 );
        //SetButtonsEnabled(isConnected);
    }

    private void RainbowWebRTC_CallUpdated(object sender, CallEventArgs e)
    {
        UnityExecutor.Execute(() =>
        {
            RainbowWebRTC_CallUpdatedInUnityThread(e);
        });
    }

    private void RainbowWebRTC_CallUpdatedInUnityThread(CallEventArgs e)
    {
        bool isVisible = (e.Call.CallStatus == Call.Status.ACTIVE) || (e.Call.CallStatus == Call.Status.CONNECTING);

        canvasGroup.alpha = isVisible ? 1 : 0;
        canvasGroup.blocksRaycasts = isVisible;
        canvasGroup.interactable = isVisible;
        canvasGroup.ignoreParentGroups = isVisible;

        if (!isVisible) // we are not in call anymore 
        {
            return;
        }
        if( sharing != null)
            sharing.button.enabled = true;
        if( video != null )
            video.button.enabled = true;
        RefreshUI();
        int localMedias = e.Call.LocalMedias;
        int remoteMedias = e.Call.RemoteMedias;
        if (!publishingSharing && (localMedias & Call.Media.SHARING) != 0)
        {
            publishingSharing = true;
        }
        else if (publishingSharing && (localMedias & Call.Media.SHARING) == 0)
        {
            publishingSharing = false;
        }
        bool canDoSharingAction = (remoteMedias & Call.Media.SHARING) == 0 || (localMedias & Call.Media.SHARING) != 0;

        // we can publish sharing even if there's already one when in a p2p call
        canDoSharingAction = canDoSharingAction || !e.Call.IsConference;
        if( sharing != null )
        {
            sharing.button.enabled = canDoSharingAction;
            sharing.State = publishingSharing ? 0 : 1;
        }


        if (!publishingVideo && (localMedias & Call.Media.VIDEO) != 0)
        {
            publishingVideo = true;
        }
        else if (publishingVideo && (localMedias & Call.Media.VIDEO) == 0)
        {
            publishingVideo = false;
        }
        video.State = publishingVideo ? 0 : 1;
    }
    #endregion events Rainbow and RainbowWebRTC
}
