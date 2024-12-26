using Nobi.UiRoundedCorners;
using Rainbow.Model;
using Rainbow.WebRTC.Abstractions;
using Rainbow.WebRTC.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AvatarHelper
{
    static string[] ColorsForTextAvatars = new string[12] { "0xca8741", "0xe44647", "0xc62582", "0x9b4ce7", "0x6573ee", "0x1b70e0", "0x6ec7d9", "0x98d1b1", "0x7fbd40", "0xb2d334", "0xdcd232", "0xf1be00" };


    public static Color ComputeAvatarColor(string displayname)
    {

        // Compute contact color
        string upperCaseDisplayName = displayname.ToUpper();
        int sum = 0;
        for (int i = 0; i < upperCaseDisplayName.Length; i++)
        {
            sum += upperCaseDisplayName[i];
        }
        int colorIndex = sum % 12;
        // int intValue = System.Convert.ToInt32(ColorsForTextAvatars[colorIndex], 16);
        Color result = ColorHelper.ColorFromRGB(ColorsForTextAvatars[colorIndex]);
        return result;
    }
}

public class ParticipantCard
{
    private Contact contact;
    public delegate void SharingCardTrackEnded();
    public event SharingCardTrackEnded SharingTrackEnded;
    public delegate void VideoResizedDelegate(ParticipantCard card, Texture texture, bool isSharing);
    public event VideoResizedDelegate VideoResized;
    public delegate void FullScreenButtonClickedDelegate(ParticipantCard card);
    public event FullScreenButtonClickedDelegate FullScreenButtonClicked;
    private RainbowController rainbow;
    public RawImage Avatar;
    public RawImage AvatarBorder;
    public RawImage CardBackground;
    public RawImage VideoImage;
    public RawImage BackgroundLabel;
    public RainbowGraphicsButton FullScreenButton;
    public bool hasMuteButton = true;
    public bool IsStaged;
    public bool IsLocal;
    public TextMeshProUGUI Label;
    public TextMeshProUGUI TextAvatar;
    public RemoteVideoDisplay RemoteVideoDisplay;
    const float sizeBorder = 8;

    public bool Issharing = false;
    static Color ColorBackgroundVideo = new Color(62f / 255, 62f / 255, 65f / 255);

    private static Color LabelBackgroundOff = ColorHelper.ColorFromRGBA("0x00000000");
    private static Color LabelBackgroundOn = ColorHelper.ColorFromRGBA("0x000000B0");
    public float X, Y, Width, Height;

    public void ShowFullScreenButton(bool Visible, int State)
    {
        FullScreenButton.Background.gameObject.SetActive(Visible);
        if (Visible)
        {
            FullScreenButton.State = State;
        }
    }
    public Contact Contact
    {
        get => contact;
        set
        {
            contact = value;
            UnityExecutor.Execute(() =>
            {

                if (CardBackground != null)
                {
                    if (contact == null)
                    {
                        CardBackground.name = Issharing ? "Sharing" : "Participant";
                    }
                    else
                    {
                        string contactDisplayName = getDisplayName(contact);
                        if (Label != null)
                        {
                            Label.text = contactDisplayName;
                            UpdateLabelBg();
                        }
                        CardBackground.name = Issharing ? $"Sharing {contactDisplayName}" : $"Participant {contactDisplayName}";
                    }
                }
            });
        }
    }

    private void SetAvatarTexture(Texture texture)
    {
        if (Issharing)
        {
            Debug.LogError("Tried to set avatar texture on sharing card");
            return;
        }
        try
        {
            Avatar.texture = texture;
            Avatar.color = Color.white;
            if (TextAvatar != null)
            {
                Object.Destroy(TextAvatar.gameObject);
                TextAvatar = null;
            }

            ImageWithRoundedCorners rc = Avatar.GetComponent<ImageWithRoundedCorners>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SetAvatar texture " + getDisplayName(Contact) + " " + ex.Message + " " + ex.StackTrace);
        }
    }

    public void SetVisible(bool visible)
    {
        CardBackground.gameObject.SetActive(visible);
    }
     
    public void SetVideoTrack(IMediaStreamTrack track, bool isRemote, bool IsInverted)
    {
        UnityExecutor.Execute(() =>
        {
            try
            {
                if (!isRemote)
                {
                    if( !IsInverted )
                    {
                        VideoImage.uvRect = new Rect(0, 1, 1, -1);
                    } 
                    else
                    {
                        VideoImage.uvRect = new Rect(1, 1, -1, -1);
                    }
                }
                else
                {
                    VideoImage.uvRect = new Rect(0, 0, 1, 1);
                }

                IVideoStreamTrack t = track as IVideoStreamTrack;
                RemoteVideoDisplay.Track = t;
                RemoteVideoDisplay.Active = (t != null);

                Debug.Log($"Set Video Track {track} IsSharing {Issharing} IsRemote{isRemote} name {CardBackground.name}");
                
                if (track == null)
                {                
                    RemoveVideoTrack();
                }
                else
                {                 
                    VideoImage.gameObject.SetActive(true);
                    if (!Issharing)
                    {
                        AvatarBorder.gameObject.SetActive(false);
                    }

                }
                UpdateLabelBg();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex.Message} {ex.StackTrace}");
            }
        }
        );
    }

    private void UpdateLabelBg()
    {
        // var RV = Label.GetRenderedValues(true);
        BackgroundLabel.color = (RemoteVideoDisplay.Track == null) ? LabelBackgroundOff : LabelBackgroundOn;
        
        TMP_TextInfo info = Label.GetTextInfo(Label.text);
        if( info.characterCount < 1 )
        {
            return;
        }         
    }

    public void RemoveVideoTrack()
    {
        UnityExecutor.Execute(() =>
        {
            Debug.Log($"Will remove video on {CardBackground.name} IsSharing = {Issharing}");
            if (!Issharing)
            {
                AvatarBorder.gameObject.SetActive(true);
            }
            VideoImage.color = ColorBackgroundVideo;
            VideoImage.texture = null;
            VideoImage.gameObject.SetActive(false);
            UpdateLabelBg();

        });
    }
     
    internal void SetTextAvatar(string str)
    {
        TextAvatar = new GameObject("TextAvatar").AddComponent<TextMeshProUGUI>();
        TextAvatar.transform.SetParent(Avatar.rectTransform);
        TextAvatar.rectTransform.anchoredPosition = new Vector2(0, 0);
        TextAvatar.enableAutoSizing = true;
        // TextAvatar.fontSize = 100;
        TextAvatar.fontSizeMin = 6;
        TextAvatar.fontStyle = FontStyles.Bold;
        TextAvatar.text = str;
        TextAvatar.alignment = TextAlignmentOptions.Center;
        TextAvatar.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        TextAvatar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        Avatar.color = AvatarHelper.ComputeAvatarColor(Contact.DisplayName);
        if (Avatar.texture != null)
        {
            TextAvatar.gameObject.SetActive(false);
        }
    }
    public void Resize(float LongDimension, uint ratioX, uint ratioY)
    {
        float width, height;
        if (ratioX > ratioY)
        {
            width = LongDimension;
            height = LongDimension / (float)ratioX * (float)ratioY;
        }
        else
        {
            height = LongDimension;
            width = LongDimension * (float)ratioX / (float)ratioY;
        }
        Resize(width, height,ratioX,ratioY);
    }

    // If the card is showing a video, resize the video according to its texture dimensions and the widthCard and heightCard of the CardBackground
    private void ResizeVideo(float widthCard, float heightCard, uint ratioX, uint ratioY)
    {
        // to fit in the card
        if (VideoImage.texture == null)
            return;

        float textureW = VideoImage.texture.width;
        float textureH = VideoImage.texture.height;
        float hTexture = heightCard; // by default try to use the height of the card
        float wTexture = textureW * (heightCard / textureH);
        float xTexture = (widthCard - wTexture) / 2;
        float yTexture = 0;
        // Debug.LogError($"ResizeVideo: card: {widthCard}x{heightCard} texture: {textureW}x{textureH} => try 1: {wTexture}x{hTexture} en ({xTexture},{yTexture})");
        // if it's too wide, use the width of the card
        if (wTexture > widthCard)
        {
            xTexture = 0;
            wTexture = widthCard;
            hTexture = textureH * (widthCard / textureW);
            yTexture = - (heightCard - hTexture) / 2;
            // Debug.LogError($"ResizeVideo: card: {widthCard}x{heightCard} texture: {textureW}x{textureH} => try 2: {wTexture}x{hTexture} en ({xTexture},{yTexture})");
        }

        // reposition video
        VideoImage.rectTransform.sizeDelta = new Vector2(wTexture, hTexture);
        VideoImage.rectTransform.anchoredPosition = new Vector2(xTexture, yTexture);

        float radius = 0;
        float radiusCard = CardBackground.GetComponent<ImageWithRoundedCorners>().radius; 

        // Make rounded corners if the video is the same size as the card
        if (xTexture < radiusCard && - yTexture < radiusCard )
        {
            radius = CardBackground.GetComponent<ImageWithRoundedCorners>().radius;
        }
        VideoImage.GetComponent<ImageWithRoundedCorners>().radius = radius;
        VideoImage.GetComponent<ImageWithRoundedCorners>().Refresh();
    }

    public void Resize(float width, float height,uint ratioX,uint ratioY)
    {
        string ContactName = "unkn";
        if (Contact != null)
        {
            ContactName = getDisplayName(Contact);
        }
        if (VideoImage.texture != null)
            Debug.Log($"Resize card {ContactName} {width} x {height} Issharing: {Issharing} Texture: {VideoImage.texture} {VideoImage.texture.width}x{VideoImage.texture.height}");
        else
            Debug.Log($"Resize card {ContactName} {width} x {height} Issharing: {Issharing} no texture ");

        try
        {
            CardBackground.rectTransform.sizeDelta = new Vector2(width, height);
            ResizeVideo(width, height,ratioX, ratioY);

            if (!Issharing)
            {
                float r = Mathf.Min(width, height) * .5f;
                if( Avatar.texture != null )
                {
                    if( r > Avatar.texture.width ) { 
                        r =  Avatar.texture.width;
                    }
                }
                if (AvatarBorder != null)
                {
                    AvatarBorder.rectTransform.sizeDelta = new Vector2(r + sizeBorder, r + sizeBorder);
                    var rcBorder = AvatarBorder.GetComponent<ImageWithRoundedCorners>();
                    rcBorder.radius = (r + sizeBorder)/2;
                    rcBorder.Refresh();
                }

                Avatar.rectTransform.sizeDelta = new Vector2(r, r);
                if(TextAvatar != null )
                {
                    Debug.Log("We have a text avatar, radius is " + r);
                    TextAvatar.rectTransform.sizeDelta = new Vector2(r -10, r - 10);
                }
                var rc = Avatar.GetComponent<ImageWithRoundedCorners>();
                rc.radius = r/2;
                rc.Refresh();
            }
            // BackgroundLabel.rectTransform.sizeDelta = new Vector2(width, Label.rectTransform.sizeDelta.y);
            // Label.rectTransform.sizeDelta = new Vector2(width, Label.rectTransform.sizeDelta.y);
            UpdateLabelBg();
            Width = width;
            Height = height;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message + " " + e.StackTrace);
        }
    }

    public void SetPosition(float x, float y)
    {
        this.X = x;
        this.Y = y;
        CardBackground.rectTransform.anchoredPosition3D = new Vector3(x, -y, -1);
    }
    public void Dispose()
    {
        RemoteVideoDisplay.ActiveChanged -= RemoteVideoDisplay_ActiveChanged;
        RemoteVideoDisplay.TextureChanged -= RemoteVideoDisplay_TextureChanged;
        RemoteVideoDisplay.Dispose();
        RemoteVideoDisplay = null;

        if (AvatarBorder != null)
        {
            if (Avatar != null)
            {
                if (TextAvatar != null)
                {
                    Object.Destroy(TextAvatar.gameObject);
                    TextAvatar = null;
                }
                Object.Destroy(Avatar.gameObject);
                Avatar = null;
            }
            Object.Destroy(AvatarBorder.gameObject);
            AvatarBorder = null;
        }

        if( VideoImage != null )
        {
            Object.Destroy(VideoImage.gameObject);
            VideoImage = null;
        }

        Object.Destroy(CardBackground.gameObject);
        CardBackground = null;
        Object.Destroy(Label.gameObject);
        Label = null;
    }

    private RainbowGraphicsButton CreateFullScreenButton()
    {
        RainbowGraphicsButton sb = new RainbowGraphicsButton("mute", 20, 20, CardBackground.transform);
        sb.AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/fullsize")))
            .AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/fullsize-exit")))
            .AddState(new RainbowButtonState(Resources.Load<Sprite>("Images/SVG/grow")));
        sb.InnerMargins = 10;
        sb.Round = true;
        sb.State = 0;
        sb.rectTransform.anchoredPosition3D = new Vector3(0, 20, 0);
        sb.rectTransform.anchorMin = new Vector2(1, 1);
        sb.rectTransform.anchorMax = new Vector2(1, 1);
        sb.rectTransform.pivot = new Vector2(1, 1);
        sb.rectTransform.anchoredPosition = new Vector2(-3, -3);

        sb.button.onClick.AddListener(() =>
        {
            FullScreenButtonClicked?.Invoke(this);
        });
        return sb;
    }

    private RawImage CreateCardBackground(Transform transform)
    {
        string name = $"Card {getDisplayName(Contact)}";
        if (Contact == null)
            name = "Sharing";
        RawImage img = new GameObject(name).AddComponent<RawImage>();
        img.gameObject.transform.SetParent(transform.transform);
        img.color = ColorBackgroundVideo;
        ImageWithRoundedCorners roundedCorners = img.gameObject.AddComponent<ImageWithRoundedCorners>();
        roundedCorners.radius = 15;
        img.rectTransform.anchorMin = new Vector2(0, 1);
        img.rectTransform.anchorMax = new Vector2(0, 1);
        img.rectTransform.pivot = new Vector2(0, 1);
        return img;
    }

    private RawImage CreateVideoImage()
    {
        string name = $"Video";
        RawImage img = new GameObject(name).AddComponent<RawImage>();
        img.gameObject.transform.SetParent(CardBackground.transform);
        img.color = Color.white;

        ImageWithRoundedCorners roundedCorners = img.gameObject.AddComponent<ImageWithRoundedCorners>();
        roundedCorners.radius = 15;
        img.rectTransform.anchorMin = new Vector2(0, 1);
        img.rectTransform.anchorMax = new Vector2(0, 1);
        img.rectTransform.pivot = new Vector2(0, 1);
        img.rectTransform.anchoredPosition = new Vector2(0, 0);
        img.rectTransform.sizeDelta = new Vector2(0, 0);
        return img;
    }
    private void CreateAvatar()
    {
        RectTransform parent = CardBackground.rectTransform;

        RawImage imgBorder = new GameObject($"Border {getDisplayName(Contact)}").AddComponent<RawImage>();
        ImageWithRoundedCorners roundedCornersBorder = imgBorder.gameObject.AddComponent<ImageWithRoundedCorners>();
        imgBorder.color = Color.white;
        imgBorder.gameObject.transform.SetParent(parent);
        imgBorder.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        imgBorder.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        imgBorder.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        imgBorder.rectTransform.anchoredPosition = new Vector2(0, 0);
        AvatarBorder = imgBorder;
        roundedCornersBorder.radius = 10;

        RawImage img = new GameObject($"Avatar {getDisplayName(Contact)}").AddComponent<RawImage>();
        img.gameObject.transform.SetParent(imgBorder.transform);
        img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchoredPosition = new Vector2(0, 0);
        ImageWithRoundedCorners roundedCorners = img.gameObject.AddComponent<ImageWithRoundedCorners>();
        roundedCorners.radius = 1;
        // roundedCorners.Refresh();
        Avatar = img;
    }

    private void CreateLabel()
    {
        BackgroundLabel = new GameObject($"BG Label").AddComponent<RawImage>();
        BackgroundLabel.transform.SetParent(CardBackground.transform);
        
        BackgroundLabel.rectTransform.anchorMin = new Vector2(0, 0);
        BackgroundLabel.rectTransform.anchorMax = new Vector2(1, 0);
        BackgroundLabel.rectTransform.pivot = new Vector2(0, 0);
        BackgroundLabel.rectTransform.anchoredPosition = new Vector2(12,2);
        BackgroundLabel.rectTransform.sizeDelta = new Vector2(-24, 20);
        TextMeshProUGUI text = new GameObject($"Label").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(BackgroundLabel.transform,true) ;
        text.color = new Color(169, 169, 169);
        text.rectTransform.anchorMin = new Vector2(0, 0);
        text.rectTransform.anchorMax = new Vector2(1, 0);
        text.rectTransform.pivot = new Vector2(0, 0);
        text.rectTransform.sizeDelta = new Vector2(5, 15);
        text.rectTransform.anchoredPosition = new Vector2(5, 0);
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.fontSize = 10;
        if (Contact != null)
            text.text = getDisplayName( Contact);

        Label = text;
        UpdateLabelBg();
    }
    private string getDisplayName( Contact contact )
    {
        if (contact == null) return "";
        if (!string.IsNullOrEmpty(contact.DisplayName)) return contact.DisplayName;
        return Rainbow.Util.GetContactDisplayName(contact);
    }

    public ParticipantCard(Contact c, RectTransform transform, bool isSharing, bool isLocal, RainbowController rainbow)
    {
        this.rainbow = rainbow;
        UnityExecutor.ExecuteSync(() =>
        {
            Issharing = isSharing;
            IsLocal = isLocal;
            Contact = c;
            CardBackground = CreateCardBackground(transform);            
            VideoImage = CreateVideoImage();
            VideoImage.gameObject.SetActive(false);
            CardBackground.gameObject.SetActive(false);
            
            RemoteVideoDisplay = new RemoteVideoDisplay(null, VideoImage);
            RemoteVideoDisplay.ActiveChanged += RemoteVideoDisplay_ActiveChanged;
            RemoteVideoDisplay.TextureChanged += RemoteVideoDisplay_TextureChanged;

            if (!isSharing)
            {
                CreateAvatar();
                CreateLabel();
            }
            else
            {
                CreateLabel();
                SetVisible(false);
            }
            FullScreenButton = CreateFullScreenButton();
            Contact = c;
            if( contact == null )
            {
                return;
            }

            string contactInitials = Rainbow.Util.GetContactInitials(c);
            SetTextAvatar(contactInitials.ToUpper());

            if (c.LastAvatarUpdateDate == System.DateTime.MinValue)
            {
                return;
            }

            rainbow.AvatarCache.GetContactAvatarTexture(c.Id, 512, texture =>
            {
                if (texture != null)
                {
                    UnityExecutor.Execute(() => { 
                        SetAvatarTexture(texture);
                    });
                }
            });
        });
        IsLocal = isLocal;
    }

    private void RemoteVideoDisplay_TextureChanged(Texture texture)
    {
        VideoResized?.Invoke(this, texture, Issharing);
    }

    private void RemoteVideoDisplay_ActiveChanged(bool active)
    {
        string name = "unkown";
        if (Contact != null)
        {
            name = Contact.DisplayName;
        }
        Debug.Log($"Remote Video Display Active changed act:{active} sharing:{Issharing} name:{name}");
        if (!active)
        {
            RemoveVideoTrack();            
            if (Issharing)
            {
                SharingTrackEnded?.Invoke();
            }
        }
        UpdateLabelBg();
    }
}