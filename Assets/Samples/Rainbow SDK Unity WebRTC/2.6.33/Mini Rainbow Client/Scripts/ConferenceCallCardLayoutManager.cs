using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rainbow.Model;
using Rainbow.WebRTC.Abstractions;

public class ConferenceCallCardLayoutManager : AbstractComCardLayoutManager
{
    private const int MaxCardPerPage = 12;
    private bool canRefresh = true;
    [SerializeField]
    private RainbowController rainbow;
    [SerializeField]
    private RectTransform Layout;

    public static List<Color> Colors = new List<Color>() { new Color(62f / 255, 62f / 255, 65f / 255) }; // , Color.red, Color.blue, Color.black, Color.white, Color.green, Color.yellow };
    [SerializeField]
    private uint ratioX = 16;
    [SerializeField]
    private uint ratioY = 9;
    [SerializeField]
    private bool HideSelfCamera = false;
    [SerializeField]
    private uint Line = 1;
    [SerializeField]
    private uint Column = 1;
    [SerializeField]
    private uint StartX;
    [SerializeField]
    private int EndX;
    [SerializeField]
    private uint StartY;
    [SerializeField]
    private int EndY;
    [SerializeField]
    private float borderW = 10;
    [SerializeField]
    private float borderH = 10;
    [SerializeField]
    private float StagePercentHeight = .8f;
    
    private int lastPage = 0;
    private bool isBigStage = false;
    public override int CurrentPage { get; internal set; }

    private bool LayoutUpdated = false;

    private ParticipantCard stagedCard = null;
    private ParticipantCard sharingCard = null;
    private List<ParticipantCard> cards = new List<ParticipantCard>();

    private void StageCard(ParticipantCard card)
    {
        List<ParticipantCard> newCardsList = new(cards);
        if (card == null)
            return;

        if (stagedCard != null)
        {
            newCardsList.Add(stagedCard);
        }
        stagedCard = card;
        newCardsList.Remove(card);
        cards = newCardsList;
        UpdateLayout();
    }

    public override int LastPage
    {
        get => lastPage;
        internal set
        {
            if (value != lastPage)
            {
                lastPage = value;
                RaiseLastPageChanged(lastPage);
            }
        }
    }

    public override void FreezeRefresh(bool frozen)
    {
        canRefresh = !frozen;
        if (canRefresh)
        {
            UpdateLayout();
        }
    }
    private void recomputeLastPage()
    {
        LastPage = cards.Count / MaxCardPerPage;
    }
    public override void NextPage()
    {
        if (CurrentPage * MaxCardPerPage < cards.Count)
        {
            CurrentPage++;
        }
        UpdateLayout();
    }

    public override void PrevPage()
    {
        if (CurrentPage > 0)
        {
            CurrentPage--;
        }
        UpdateLayout();
    }

    private void UnstageCard(ParticipantCard card)
    {
        if (card == stagedCard )
        {
            card.SetVisible(false);
            isBigStage = false;
            stagedCard = null;
            UpdateLayout();
            return;
        }

    }

    public override void ClearAll()
    {
        CurrentPage = 0;


        FreezeRefresh(true);
        if (stagedCard == sharingCard)
        {
            sharingCard.SetVisible(false);
            stagedCard = null;
        }

        if (stagedCard != null)
        {
            UnstageCard(stagedCard);
        }
        foreach (ParticipantCard card in cards)
        {
            card.Dispose();
        }
        cards = new List<ParticipantCard>();
        FreezeRefresh(false);
    }

    public override void AddParticipant(Contact contact, bool isLocal = false)
    {
        ParticipantCard card = null;
        if( isLocal && HideSelfCamera )
        {
            return;
        }

        var tmpCards = new List<ParticipantCard>(cards);
        if (stagedCard != null && stagedCard.Issharing == false )
        {
            tmpCards.Add(stagedCard);
        }
        foreach (ParticipantCard p in tmpCards)
        {
            if (p.Contact.Id == contact.Id)
            {
                card = p;
                break;
            }
        }
        if (card != null)
        {
            return;
        }
        card = new ParticipantCard(contact, Layout, false, isLocal,rainbow);
        card.FullScreenButtonClicked += FullScreenButtonClicked;
        card.VideoResized += OnVideoTextureChangedOnCard;
        recomputeLastPage();
        TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources();

        //GameObject newButton = TMP_DefaultControls.CreateButton(resources);
        //newButton.transform.SetParent(card.CardBackground.rectTransform);
        //newButton.GetComponent<Button>().onClick.AddListener(() => { 
        //    Debug.Log("Destroy " + contact.DisplayName); 
        //    RemoveParticipant(contact); 
        //});
        //newButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        //newButton.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        //newButton.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
        //newButton.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 30);
        //newButton.GetComponentInChildren<TMP_Text>().text = "X";
        

        cards.Add(card);
        UpdateLayout();
        return;
    }

    public override void SetRemoteSharingTrack(Contact c, IMediaStreamTrack track)
    {
        SetSharingTrack(c, track, true, false);
    }
    public override void SetLocalSharingTrack(Contact c, IMediaStreamTrack track, bool IsInverted)
    {
        SetSharingTrack(c, track, false, IsInverted);
    }

    private void SetSharingTrack(Contact c, IMediaStreamTrack track, bool isRemote, bool IsInverted)
    {

        ParticipantCard card = sharingCard;
        if (card == null)
        {
            sharingCard = new ParticipantCard(c, Layout, true, isRemote, rainbow);
            sharingCard.VideoResized += OnVideoTextureChangedOnCard;
        }
        sharingCard.Contact = c;
        if (c != null)
        {
            sharingCard.Label.text = c.DisplayName;
        }
        else
        {
            sharingCard.Label.text = "";
        }
        if (track == null)
        {
            sharingCard.SetVideoTrack(track, isRemote, IsInverted);
            SharingCard_SharingTrackEnded();
        }
        else
        {
            sharingCard.SetVisible(true);
            sharingCard.SetVideoTrack(track, isRemote, IsInverted);
            StageCard(sharingCard);
        }
        
    }

    private void OnVideoTextureChangedOnCard(ParticipantCard card, Texture texture, bool isSharing)
    {
        // Debug.LogError($"Card {card.CardBackground.name} updated with texture {texture} {texture?.width} {texture.height} Issharing: {isSharing}");
        UpdateLayout();
    }


    public override void SetRemoteVideoTrack(string publisherId, IMediaStreamTrack track)
    {
        SetVideoTrack(publisherId, track, true, false);
    }

    public override void SetLocalVideoTrack(string publisherId, IMediaStreamTrack track, bool IsInverted)
    {
        if (HideSelfCamera)
            return;
        SetVideoTrack(publisherId, track, false,IsInverted);
    }

    private void SetVideoTrack(string publisherId, IMediaStreamTrack track, bool isRemote, bool IsInverted)
    {
        foreach (var c in cards)
        {
            if (c.Contact.Id == publisherId)
            {
                c.SetVideoTrack(track, isRemote, IsInverted);
                return;
            }
        }
        if( stagedCard != null)
        {
            if (stagedCard.Contact.Id == publisherId)
            {
                stagedCard.SetVideoTrack(track, isRemote, IsInverted);
                return;
            }
        }
    }

    public override void RemoveParticipant(Contact contact)
    {
        var tmpCards = new List<ParticipantCard>(cards);
        if (stagedCard != null && stagedCard.Issharing == false)
        {
            tmpCards.Add(stagedCard);
        }
        foreach (ParticipantCard card in tmpCards)
        {
            if (card.Contact.Id == contact.Id)
            {
                if( stagedCard == card )
                {
                    UnstageCard(card);
                }

                cards.Remove(card);
                card.Dispose();
                recomputeLastPage();
                UpdateLayout();
                return;
            }
        }
    }

    [ContextMenu("Force refresh layout")]
    private void UpdateLayout()
    {
        cards.Sort(new ParticipantCardComparer());
        LayoutUpdated = true;
        if (stagedCard == null)
        {
            switch (cards.Count)
            {
                case 1: Line = 1; Column = 1; break;
                case 2: Line = 2; Column = 1; break;
                case 3:
                case 4:
                    Line = 2; Column = 2; break;
                case 5:
                case 6:
                    Line = 3; Column = 2; break;
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                default:
                    Column = 3;
                    Line = (uint)Mathf.Min(((cards.Count / 3) + ((cards.Count % 3) > 0 ? 1 : 0)), 4);
                    break;
            }
        }
        else
        {
            if (cards.Count > 6)
            {
                Column = 6;
                Line = 2;
            }
            else
            {
                Column = (uint)Mathf.Min(6, cards.Count);
                Line = 1;
            }

            if (Column == 0)
                Column = 1;
        }

        if ((CurrentPage * MaxCardPerPage) >= cards.Count)
        {
            CurrentPage = ((cards.Count - 1) / MaxCardPerPage);
        }
    }
    void Start()
    {
        sharingCard = new ParticipantCard(null, Layout, true, false, rainbow);
        sharingCard.SharingTrackEnded += SharingCard_SharingTrackEnded;
        sharingCard.VideoResized += OnVideoTextureChangedOnCard;
        sharingCard.FullScreenButtonClicked += FullScreenButtonClicked;
    }

    
    private void FullScreenButtonClicked(ParticipantCard card)
    {
        if (card == stagedCard)
        {
            isBigStage = !isBigStage;            
        } 
        else
        {
            StageCard(card);
        }
        UpdateLayout();
    }

    private void SharingCard_SharingTrackEnded()
    {
        if (sharingCard == null)
        {
            Debug.Log("Received SharingTrackEnded but there is no sharing card, ignore it.");
            return;
        }
        sharingCard.Contact = null;
        if( sharingCard == stagedCard ) { 
            UnstageCard(sharingCard); 
        } else
        {
            cards.Remove(sharingCard);
            sharingCard.SetVisible(false);
            UpdateLayout();
        }
    }

    private void DisplayNImages(uint nbLine, uint nbCol, float x, float xmax, float y, float ymax)
    {
        // RectTransform parentEstate = canvas.GetComponent<RectTransform>();        
        float parentW = (xmax - x);
        float parentH = (ymax - y);
        Debug.Log($"compute resize : j'ai {parentW}x{parentH} pour afficher {nbCol} images avec des border de {borderW}x{borderH}");
        float widthImg = parentW / nbCol - borderW;
        float heightImg = widthImg * ((float)ratioY / (float)ratioX) - borderH;

        if (heightImg * nbLine > parentH)
        {
            heightImg = parentH / nbLine - borderH;
            widthImg = heightImg * ((float)ratioX / (float)ratioY) - borderW;
        }

        float heightTotal = heightImg * nbLine + (nbLine - 1) * borderH;
        float deltaY = (parentH - heightTotal) / 2; // y offset to center the resulting "grid" vertically
        float ImgY = deltaY + y;

        for (int l = 0; l < nbLine; l++)
        {
            float deltaX = 0; // x offet to center the resulting line horizontally

            long indexFirstCardOfLine = CurrentPage * MaxCardPerPage + l * nbCol;
            long nbCardsInLine = (cards.Count - indexFirstCardOfLine) > nbCol ? nbCol : (cards.Count - indexFirstCardOfLine);
            float estateX = nbCardsInLine * widthImg + (nbCardsInLine - 1) * borderW;
            deltaX = (parentW - estateX) / 2;


            float ImgX = deltaX + x;
            for (int c = 0; c < nbCardsInLine; c++)
            {
                ParticipantCard card = cards[(int)(indexFirstCardOfLine + c)];
                card.Resize(widthImg, heightImg, ratioX, ratioY);
                card.SetPosition(ImgX, ImgY);
                ImgX += widthImg + borderW;
            }
            ImgY += heightImg + borderH;
        }

    }
    void showRelevantCards()
    {

        for (int i = 0; i < cards.Count; i++)
        {
            if (((i / MaxCardPerPage) < CurrentPage) || ((i / MaxCardPerPage) > CurrentPage) || isBigStage)
                cards[i].SetVisible(false);
            else
            {
                cards[i].SetVisible(true);
                cards[i].ShowFullScreenButton(true, 2);
            }
        }
    }
    void DrawLayout()
    {
        float DeltaY = StartY;
        float xMax = EndX;
        float yMax = EndY;
        // parentRectTransform  = parentRectTransform = transform.parent.GetComponent<RectTransform>();
        
        RectTransform parentRectTransform = Layout;
        //xMax = Mathf.Min((parentRectTransform.sizeDelta.x + xMax), parentRectTransform.sizeDelta.x);
        //yMax = Mathf.Min((parentRectTransform.sizeDelta.y + yMax), parentRectTransform.sizeDelta.y);
        xMax = Mathf.Min((parentRectTransform.rect.width + xMax), parentRectTransform.rect.width);
        yMax = Mathf.Min((parentRectTransform.rect.height + yMax), parentRectTransform.rect.height);

        if (stagedCard != null)
        {
            stagedCard.ShowFullScreenButton(true, isBigStage ? 1 : 0);
            float widthStage = xMax - StartX;
            float heightStage = (yMax - StartY) * (isBigStage ? 1 : StagePercentHeight);
            if (ratioY > ratioX)
            {
                float w = heightStage * ratioX / ratioY;
                if (w <= widthStage)
                {
                    stagedCard.Resize(heightStage, ratioX, ratioY);
                }
                else
                {
                    stagedCard.Resize(heightStage * widthStage / w, ratioX, ratioY);
                }
            }
            else
            {
                float h = widthStage * ratioY / ratioX;
                if (h <= heightStage)
                {
                    stagedCard.Resize(widthStage, ratioX, ratioY);
                }
                else
                {
                    stagedCard.Resize(widthStage * heightStage / h, ratioX, ratioY);
                }
            }

            // Debug.LogError($"heightStage = {heightStage} widthStage = {widthStage} xMAx {xMax} yMax{yMax} ");
            // Debug.LogError($"After resize heightStage = {stagedCard.Height} widthStage = {stagedCard.Width} ");
            stagedCard.SetPosition((widthStage - stagedCard.Width) / 2 + StartX, StartY + (heightStage - stagedCard.Height) / 2);
            DeltaY = heightStage + borderH + StartY;
        }
        DisplayNImages(Line, Column, StartX, xMax, DeltaY, yMax);
        showRelevantCards();
    }

    public override void RefreshLayout()
    {
        UpdateLayout();
    }

    void Update()
    {
        if (LayoutUpdated && canRefresh)
        {
            DrawLayout();
            LayoutUpdated = false;

        }
    }
}
