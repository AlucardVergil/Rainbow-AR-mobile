using Rainbow.Model;
using Rainbow.WebRTC.Abstractions;
using System.Collections.Generic;
using UnityEngine;

public class P2PCallCardLayoutManager : AbstractComCardLayoutManager
{
    private ParticipantCard remoteCard = null;
    private ParticipantCard localCard = null;
    private ParticipantCard remoteSharing = null;
    private ParticipantCard localSharing = null;

    [SerializeField]
    private RainbowController rainbow;

    [SerializeField]
    private RectTransform Layout;

    [SerializeField]
    private int BorderW = 10;
    [SerializeField]
    private int BorderH = 10;

    [SerializeField]
    private uint ratioX = 16;
    [SerializeField]
    private uint ratioY = 9;

    [SerializeField]
    private uint StartX;
    [SerializeField]
    private int EndX;
    [SerializeField]
    private uint StartY;
    [SerializeField]
    private int EndY;

    [SerializeField]
    private float StagePercentHeight = .8f;

    bool canRefresh = true;
    private bool layOutUpdated = false;
    private bool hasLocalSharing = false;
    private bool hasLocalVideo = false;
    private bool hasRemoteSharing = false;
    private bool hasRemoteVideo = false;
    private bool isBigStage = false;
    private ParticipantCard stagedCard = null;

    List<ParticipantCard> stagedCards = new List<ParticipantCard>();
    List<ParticipantCard> otherCards = new List<ParticipantCard>();

    public override int CurrentPage { get => 0; internal set { } }

    public override void AddParticipant(Contact contact, bool isLocal = false)
    {
        Debug.Log("Add Participant");
        if (isLocal)
        {
            if (localCard == null)
            {
                localCard = new ParticipantCard(contact, Layout, false, isLocal,rainbow);
                localCard.VideoResized += OnVideoTextureChangedOnCard;
                localCard.FullScreenButtonClicked += FullScreenButtonClicked;
                UpdateLayout();
            }
            return;
        }
        if (remoteCard == null)
        {
            remoteCard = new ParticipantCard(contact, Layout, false, isLocal, rainbow);
            remoteCard.FullScreenButtonClicked += FullScreenButtonClicked;
            remoteCard.VideoResized += OnVideoTextureChangedOnCard;
            UpdateLayout();
        }
        return;
    }

    public override void ClearAll()
    {
        Debug.Log("ClearAll Called");
        if (remoteCard != null)
        {
            remoteCard.Dispose();
            remoteCard = null;
        }
        if (localCard != null)
        {
            localCard.Dispose();
            localCard = null;
        }
        if (remoteSharing != null)
        {
            remoteSharing.SetVisible(false);
        }
        if (localSharing != null)
        {
            localSharing.SetVisible(false);
        }

        hasLocalSharing = false;
        hasLocalVideo = false;
        hasRemoteSharing = false;
        hasRemoteVideo = false;
        stagedCard = null;
        isBigStage = false;
        stagedCards.Clear();
        otherCards.Clear();
    }

    [ContextMenu("Force refresh layout")]
    private void UpdateLayout()
    {
        layOutUpdated = true;
    }

    public override void FreezeRefresh(bool frozen)
    {
        canRefresh = !frozen;
    }

    public override void RemoveParticipant(Contact contact)
    {
    }

    public override void SetLocalSharingTrack(Contact c, IMediaStreamTrack track, bool IsInverted)
    {
        hasLocalSharing = track != null;
        localSharing.Contact = c;
        localSharing.SetVideoTrack(track, false, IsInverted);
        UpdateLayout();
    }

    public override void SetLocalVideoTrack(string publisherId, IMediaStreamTrack track, bool IsInverted)
    {
        hasLocalVideo = (track != null);
        localCard.SetVideoTrack(track, false, IsInverted);
        UpdateLayout();
    }

    public override void SetRemoteSharingTrack(Contact c, IMediaStreamTrack track)
    {
        hasRemoteSharing = (track != null);
        remoteSharing.Contact = c;
        remoteSharing.SetVideoTrack(track, true,false);
        UpdateLayout();

    }

    public override void SetRemoteVideoTrack(string publisherId, IMediaStreamTrack track)
    {
        Debug.Log($"SETREMOTE VIDEO TRACK {track != null}");
        hasRemoteVideo = (track != null);
        remoteCard.SetVideoTrack(track, true, false);
        UpdateLayout();
    }

    public override void RefreshLayout()
    {
        Debug.Log("RefreshLayout P2P called");
        UpdateLayout();
    }

    void Start()
    {
        // Layout = GetComponent<Transform>().parent.GetComponent<RectTransform>();
        localSharing = new ParticipantCard(null, Layout, true, true, rainbow);
        localSharing.VideoResized += OnVideoTextureChangedOnCard;
        localSharing.FullScreenButtonClicked += FullScreenButtonClicked;
        remoteSharing = new ParticipantCard(null, Layout, true, false, rainbow);
        remoteSharing.VideoResized += OnVideoTextureChangedOnCard;
        remoteSharing.FullScreenButtonClicked += FullScreenButtonClicked;
    }

    private void FullScreenButtonClicked(ParticipantCard card)
    {
        if (stagedCards.Contains(card))
        {
            isBigStage = !isBigStage;
        }
        else
        {
            stagedCard = card;
        }
        UpdateLayout();
    }

    private void OnVideoTextureChangedOnCard(ParticipantCard card, Texture texture, bool isSharing)
    {
        Debug.Log($"Card {card.CardBackground.name} updated with texture {texture} {texture?.width} {texture?.height} Issharing: {isSharing}");
        UpdateLayout();
    }

    int layoutMask()
    {
        int mask = 0;
        if (hasLocalVideo)
        {
            mask |= 8;
        }
        if (hasLocalSharing)
        {
            mask |= 4;
        }
        if (hasRemoteVideo)
        {
            mask |= 2;
        }
        if (hasRemoteSharing)
        {
            mask |= 1;
        }
        return mask;
    }


    private void DisplayCards(List<ParticipantCard> cards, float x, float xmax, float y, float ymax)
    {
        try
        {
            // compute desired width and height for every video.             
            float parentW = (xmax - x);
            float parentH = (ymax - y);

            Debug.Log($"compute resize : j'ai x {x} xmax {xmax} y {y} ymax {ymax} {parentW}x{parentH} pour afficher {cards.Count} cartes avec des border de {BorderW}x{BorderH}");
            parentW -= (cards.Count - 1) * BorderW;
            parentH -= (cards.Count - 1) * BorderH;

            float widthImg = parentW / (cards.Count);
            float heightImg = widthImg * ((float)ratioY / (float)ratioX);

            if (heightImg > parentH)
            {
                heightImg = parentH;
                widthImg = heightImg * ((float)ratioX / (float)ratioY);
            }

            float remainingEstateX = 0;
            remainingEstateX = parentW - cards.Count * widthImg - (cards.Count - 1) * BorderW;
            float marginX = remainingEstateX / 2;
            float ImgX = marginX;

            for (int c = 0; c < cards.Count; c++)
            {
                ParticipantCard card = cards[c];
                float ImgY = y + BorderH;
                Debug.Log($"Lining up " + cards.Count + " " + c + " " + card.Contact?.DisplayName + " x " + ImgX + " y " + ImgY + " w " + widthImg + " h " + heightImg);

                card.Resize(widthImg, heightImg, ratioX, ratioY);
                card.SetPosition(ImgX, ImgY);
                ImgX += widthImg + BorderH;

                
                if (stagedCards.Contains(card))
                {
                    int buttonState = 0;
                    if (isBigStage)  // we want to display the "reduction" icon
                        buttonState = 1;

                    card.ShowFullScreenButton(true, buttonState );
                }
                else
                {
                    card.ShowFullScreenButton(true, 2);
                }
                card.SetVisible(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception {ex.Message} {ex.StackTrace}");
        }
    }
    void addCardToFirstRow(ParticipantCard card)
    {
        if (stagedCard == null || stagedCard == card)
        {
            stagedCards.Add(card);
            //            ShowStageCardButton(card, false);
            return;
        }
        otherCards.Add(card);
        //ShowStageCardButton(card, true );
    }

    void addCardToSecondRow(ParticipantCard card)
    {
        if (stagedCard == null || stagedCard != card)
        {
            otherCards.Add(card);
            //            ShowStageCardButton(card, true);
            return;
        }

        stagedCards.Add(card);
        //ShowStageCardButton(card, false);
    }

    void layout()
    {
        stagedCards.Clear();
        otherCards.Clear();
        if (remoteSharing != null)
        {
            remoteSharing.SetVisible(false);

        }
        if (remoteCard != null)
        {
            remoteCard.SetVisible(false);
        }
        if (localSharing != null)
        {
            localSharing.SetVisible(false);
        }
        if (localCard != null)
        {
            localCard.SetVisible(false);
        }

        int layoutmask = layoutMask();
        // Debug.LogError("Layout called mask " + layoutmask);


        // maintain stageCard : if a card which needs to be hidden is the staged card, there's no stagecard anymore
        if (stagedCard != null)
        {
            if ((stagedCard == localSharing && hasLocalSharing == false) ||
                (stagedCard == remoteSharing && hasRemoteSharing == false) ||
                (stagedCard == remoteCard && hasRemoteVideo == false))
            {
                stagedCard = null;
                isBigStage = false;
            }
        }

        switch (layoutmask)
        {
            case 0:
            case 2:
                addCardToFirstRow(remoteCard);
                break;

            case 1:
                addCardToFirstRow(remoteSharing);
                break;

            case 3:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(remoteCard);
                break;

            case 4:
                addCardToFirstRow(localSharing);
                break;

            case 5:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(localSharing);
                break;

            case 6:
                addCardToFirstRow(localSharing);
                addCardToSecondRow(remoteCard);
                break;

            case 7:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(localSharing);
                addCardToSecondRow(remoteCard);
                break;

            case 8:
            case 10:
                addCardToFirstRow(remoteCard);
                addCardToSecondRow(localCard);
                break;

            case 9:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(localCard);
                break;

            case 11:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(localCard);
                addCardToSecondRow(remoteCard);
                break;

            case 12:
                addCardToFirstRow(localSharing);
                addCardToSecondRow(localCard);
                break;

            case 13:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(localSharing);
                addCardToSecondRow(localCard);
                break;

            case 14:
                addCardToFirstRow(localSharing);
                addCardToSecondRow(localCard);
                addCardToSecondRow(remoteCard);
                break;

            case 15:
                addCardToFirstRow(remoteSharing);
                addCardToSecondRow(remoteCard);
                addCardToSecondRow(localSharing);
                addCardToSecondRow(localCard);
                break;
        }


        try
        {            
            float MaxY, MaxX;
            MaxY = EndY;

            if (MaxY <= 0)
            {                
                MaxY = Layout.rect.height + EndY;
            }
            MaxX = EndX;
            if (MaxX <= 0)
            {
                MaxX = Layout.rect.width + EndX;
            }

            float heightAvailable = MaxY - StartY;
            float heightForStage = heightAvailable * ( isBigStage ? 1 : StagePercentHeight );
            // Debug.Log("yMaxY is " + MaxY + "yEndY is " + EndY + " StageCards: " + stagedCards.Count + " OtherCards: " + otherCards.Count + " heightAvailable " + heightAvailable + " heightForStage " + heightForStage);

            if (stagedCards.Count == 1)
            {
                if (otherCards.Count != 0)
                {
                    DisplayCards(stagedCards, StartX, MaxX, StartY, StartY + heightForStage );
                }
                else
                {  
                    // Only stage.
                    DisplayCards(stagedCards, StartX, MaxX, StartY, MaxY);
                    return;
                }
            } 
            else
            {
                isBigStage = false;
            }

            if (otherCards.Count == 0) return;
            if (isBigStage) return;

            // Display Other cards

            if (stagedCards.Count == 1)
            {
                // Display a second line of cards after the stage
                DisplayCards(otherCards, StartX, MaxX, StartY + heightAvailable * StagePercentHeight + BorderH, MaxY);
            }
            else
            {
                // Stage was not displayed (no staged cards)                
                DisplayCards(otherCards, StartX, MaxX, StartY, MaxY);
                return;
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{ex.Message} {ex.StackTrace}");
        }
    }

    void Update()
    {
        try
        {
            if (canRefresh && layOutUpdated)
            {
                layout();
                layOutUpdated = false;
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{ex.Message} {ex.StackTrace}");
        }
    }
}
