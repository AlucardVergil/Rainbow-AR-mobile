using Nobi.UiRoundedCorners;
using Rainbow;
using Rainbow.Model;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ParticipantListAvatar
{
    public Contact Contact;
    public bool IsMe;
    public RawImage Background;
    public TMP_Text TextAvatar;

    public ParticipantListAvatar(Contact contact, bool isMe, GameObject ItemTemplate )
    {
        Contact = contact;
        IsMe = isMe;
        Background = ItemTemplate.transform.Find("Avatar").GetComponent<RawImage>();

        TextAvatar = Background.gameObject.transform.Find("Initials").GetComponent<TMP_Text>();
        TextAvatar.text  = Util.GetContactInitials(contact).ToUpper();
        Background.color = AvatarHelper.ComputeAvatarColor(Contact.DisplayName);
    }

    public void SetAvatarTexture( Texture t)
    {
        if( t!=null)
        {
            Object.Destroy(TextAvatar);
            TextAvatar = null;
            Background.color = Color.white;
            Background.texture = t;
        }
    }
}
public class ParticipantListItem
{
    public Button Item;
    public ParticipantListAvatar avatar;    
    public TMP_Text ContactDisplayName;
    public TMP_Text ContactCompanyName;
    public Button AdditionalButton;
    public CanvasGroup AdditionalButtonCanvasGroup;

    public void ShowExtraButton(bool show)
    {
        if(show)
        {
            if( AdditionalButton == null )
            {
                TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources();
                GameObject button = TMP_DefaultControls.CreateButton(resources);                
                AdditionalButton = button.GetComponent<Button>();
                AdditionalButton.AddComponent<ImageWithRoundedCorners>().radius = 5;
                RectTransform rectTransform= AdditionalButton.GetComponent<RectTransform>();
                rectTransform.parent = Item.transform;
                rectTransform.sizeDelta = new Vector2(30, 30);
                rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(1,0.5f);
                rectTransform.anchoredPosition3D = new Vector3(-5, 0, 0);
                rectTransform.gameObject.name = "ExtraButton";
                var Label = rectTransform.GetComponentInChildren<TMP_Text>();
                Label.text = "X";
                Label.gameObject.name = "Label";
                Label.fontStyle = FontStyles.Bold;
                Label.fontSize = 24;
                // rectTransform.offsetMax = new Vector3
            }
            //AdditionalButtonCanvasGroup.alpha = 1;
            //AdditionalButtonCanvasGroup.interactable = true;
            //AdditionalButtonCanvasGroup.blocksRaycasts = true;
            //AdditionalButtonCanvasGroup.ignoreParentGroups = true;
            return;
        }
        if( AdditionalButton != null)
        {
            // Debug.LogError("Delete AdditionalButton");
            Object.Destroy(AdditionalButton.gameObject);
            AdditionalButton = null;
        }

    }

    public ParticipantListItem( Contact c,bool isMe, GameObject item)
    {
        Item = item.GetComponent<Button>();
        avatar = new ParticipantListAvatar(c, isMe, Item.gameObject);
        ContactDisplayName = Item.transform.Find("DisplayName").GetComponent <TMP_Text>();
        ContactCompanyName = Item.transform.Find("CompanyName").GetComponent<TMP_Text>();
        ContactDisplayName.text = c.DisplayName;
        ContactCompanyName.text = c.CompanyName;
        AdditionalButton = null;
        AdditionalButton =   Item.transform.Find("ExtraButton").GetComponent<Button>();        
        AdditionalButtonCanvasGroup = AdditionalButton.GetComponent<CanvasGroup>();
        ShowExtraButton(false);
    }
}

public class ParticipantListItemComparer : IComparer<ParticipantListItem>
{
    public int Compare(ParticipantListItem x, ParticipantListItem y)
    {
        if (x.avatar.IsMe) return -1;
        if (y.avatar.IsMe) return 1;
        return string.Compare(x.avatar.Contact.DisplayName, y.avatar.Contact.DisplayName);
    }
}

public class ParticipantsList : AbstractParticipantsList
{
    [SerializeField]
    private RainbowController rainbow;
    private static ParticipantListItemComparer comparer = new();
    private RectTransform GridContent;
    private List<ParticipantListItem> items;
    [SerializeField]
    private GameObject ItemTemplate;
    
    void Awake()
    {
        GridContent = transform.Find("ScrollableList/GridContent").GetComponent<RectTransform>();
        items = new();
    }

     
    public override void RemoveParticipant(Contact c)
    {
        foreach( var p in items)
        {
            if( p.avatar.Contact.Id == c.Id )
            {
                items.Remove(p);
                Object.Destroy(p.Item.gameObject);
                return;
            }
        }
        Debug.LogError("Couldn't remove participant item: Didn't find " + c.DisplayName);
    }

    public Button ShowExtraButton(Contact c, bool show)
    {
        ParticipantListItem participant = findListItem(c.Id);
        if (participant == null)
            return null;
        
        participant.ShowExtraButton(show);
        return participant.AdditionalButton;        
    }

    private ParticipantListItem findListItem( string Id)
    {
        foreach( var p in items)
        {
            if( p.avatar.Contact.Id == Id )
            {
                return p;
            }
        }
        return null;
    }
    public override void ClearAll()
    {
        foreach (var p in items)
        {
            Object.Destroy(p.Item.gameObject);
        }
        items.Clear();
    }

    public override void AddParticipant(Contact c, bool isMe)
    {
        GameObject instance  = Instantiate(ItemTemplate);
        instance.name = c.DisplayName; 
        ParticipantListItem item = new ParticipantListItem(c, isMe, instance);
        item.Item.transform.SetParent(GridContent);
        items.Add(item);
        items.Sort(comparer);
        int index = 0;
        foreach( var p in items)
        {
            p.Item.transform.SetSiblingIndex(index++);
        }

        if (c.LastAvatarUpdateDate == System.DateTime.MinValue)
        {
            return;
        }

        Debug.Log("Request Avatar for " + c.Id + " " + c.DisplayName);
        rainbow.AvatarCache.GetContactAvatarTexture(c.Id, 512, texture =>
        {
            Debug.Log("got Requested Avatar  for " + c.Id + " " + c.DisplayName + " : " + texture);
            if (texture != null)
            {
                item.avatar.SetAvatarTexture(texture);
            }
        });
    }
}
