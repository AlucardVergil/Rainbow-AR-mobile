using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC;
using Rainbow.WebRTC.Unity;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class MakeCallUI : MonoBehaviour
{
    public RainbowController rainbow;
    private CanvasGroup canvasGroup;
    private WebRTCCommunications rbwWebrtcCommunication;
    private TMP_Dropdown contactsDropdown;
    private List<string> contactByIndex;
    private Button p2pCallButton;
    private TMP_Dropdown conferencesDropdown;
    private List<string> conferenceByIndex;
    private Button conferenceCallButton;
    private TMP_Dropdown bubblesDropdown;
    private List<string> bubbleByIndex;
    private Button conferenceStartButton;

    IComparer<Contact> contactComparer;
    private void Awake()
    {
        // Grab references to sub objects
        foreach (Transform child in transform)
        {
            switch (child.name)
            {
                case "SelectedContact":
                    contactsDropdown = child.GetComponent<TMP_Dropdown>();
                    break;
                case "P2PCallButton":
                    p2pCallButton = child.GetComponent<Button>();
                    break;
                case "SelectedConference":
                    conferencesDropdown = child.GetComponent<TMP_Dropdown>();
                    break;
                case "SelectedBubble":
                    bubblesDropdown = child.GetComponent<TMP_Dropdown>();
                    break;
                case "ConferenceCallButton":
                    conferenceCallButton = child.GetComponent<Button>();
                    break;
                case "ConferenceStartButton":
                    conferenceStartButton = child.GetComponent<Button>();
                    break;

            }
        }
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        rainbow.ConnectionChanged += Rainbow_ConnectionChanged;
        rainbow.Ready += RainbowController_Ready;
    }


    // Start is called before the first frame update
    void Start()
    {
    }

    private void fillListOfContacts()
    {
        List<TMP_Dropdown.OptionData> options = new();
        contactByIndex = new List<string>();
        Contacts contacts = rainbow.RainbowApplication.GetContacts();
        List<Contact> contactList = new List<Contact>();

        foreach (var c in contacts.GetAllContactsFromCache())
        {
            if (c.Id != contacts.GetCurrentContactId())
            {
                contactList.Add(c);
            }
        }
        contactList.Sort(new ContactsComparer());
        foreach (var c in contactList)
        {
            contactByIndex.Add(c.Id);
            options.Add(new(c.DisplayName));
        }
        contactsDropdown.ClearOptions();
        contactsDropdown.AddOptions(options);
        p2pCallButton.enabled = (contactByIndex.Count > 0);
    }

    private void RainbowController_Ready(bool isReadyAndConnected)
    {
        if (isReadyAndConnected)
        {
            InitializeUI();
        }
    }
    private void InitializeUI()
    {
        try
        {            
            GetComponent<CanvasGroup>().alpha = 1;
            fillListOfContacts();
            p2pCallButton.onClick.AddListener(P2PCallClick);
            conferenceCallButton.onClick.AddListener(ConferenceCallClick);
            conferenceStartButton.onClick.AddListener(ConferenceStartClick);
            conferencesDropdown.ClearOptions();
            bubblesDropdown.ClearOptions();

            rainbow.RainbowApplication.GetConferences().ConferenceUpdated += RefreshConferenceAndBubbleAfterUpdate;
            rainbow.RainbowApplication.GetConferences().ConferenceRemoved += RefreshConferenceAndBubbleAfterRemove;
            rainbow.RainbowApplication.GetBubbles().BubbleAffiliationChanged += MakeCallUI_BubbleAffiliationChanged;
            rainbow.RainbowApplication.GetBubbles().BubbleMemberUpdated += MakeCallUI_BubbleMemberUpdated;

            rainbow.RainbowWebRTC.CallUpdated += RainbowWebRTC_CallUpdated1;
            fillListOfContacts();
            RefreshConferenceAndBubbleAfterUpdate(null, null);
        } catch (Exception ex)
        {
            Debug.LogError($"Exception {ex.Message} {ex.StackTrace}");
        }
    }

    private void RainbowWebRTC_CallUpdated1(object sender, CallEventArgs e)
    {
        if (e.Call != null)
        {
            UnityExecutor.Execute(() =>
            {
                RainbowWebRTC_CallUpdated(e);
            });
        }
    }

    private void MakeCallUI_BubbleMemberUpdated(object sender, BubbleMemberEventArgs e)
    {
        if (e.Member.UserJid == rainbow.RainbowApplication.GetContacts().GetCurrentContactJid())
        {
            RefreshBubble();
        }
    }

    private void MakeCallUI_BubbleAffiliationChanged(object sender, BubbleAffiliationEventArgs e)
    {
        RefreshBubble();
    }

    private void Rainbow_ConnectionChanged(string connectionstate)
    {
        //bool isConnected = connectionstate == "connected";
        //Debug.Log("Rainbow_ConnectionStateChanged " + connectionstate);
        //canvasGroup.alpha = (isConnected ? 1 : 0);
        //SetButtonsEnabled(isConnected);
    }

    private void RainbowWebRTC_CallUpdated(CallEventArgs e)
    {
        Debug.Log($"LocalMedias: {e.Call.LocalMedias} RemoteMedias: {e.Call.RemoteMedias}");
        bool isVisible = (e.Call.CallStatus == Call.Status.ACTIVE) || (e.Call.CallStatus == Call.Status.CONNECTING);

        canvasGroup.alpha = isVisible ? 0 : 1;
        if (!isVisible)
        {
            return;
        }
    }
    
    private void RefreshConferenceAndBubbleAfterRemove(object sender, IdEventArgs e)
    {
        UnityExecutor.Execute(() =>
        {
            RefreshBubble();
            RefreshConference();
        });
    }
    private void RefreshConferenceAndBubbleAfterUpdate(object sender, ConferenceEventArgs e )
    {
        UnityExecutor.Execute(() =>
        {
            RefreshBubble();
            RefreshConference();
        });
    }

    private void SetButtonsEnabled(bool enabled)
    {
        conferenceCallButton.enabled = enabled;
        conferenceStartButton.enabled = enabled;
        p2pCallButton.enabled = enabled;
    }
    private void RefreshBubble()
    {
        List<TMP_Dropdown.OptionData> options = new();
        bubbleByIndex = new List<string>();
        Conferences conferences = rainbow.RainbowApplication.GetConferences();

        Bubbles bubbles = rainbow.RainbowApplication.GetBubbles();
        HashSet<string> conferenceIds = new();
        List<Conference> conferenceList = conferences.ConferenceGetListFromCache();
        AlphabeticBubbleComparer cmp = new();

        foreach (var c in conferences.ConferenceGetListFromCache())
        {
            conferenceIds.Add(c.Id);
        }
        List<Bubble> bubbleList = bubbles.GetAllBubblesFromCache();
        bubbleList.Sort(cmp);
        foreach (var b in bubbleList)
        {
            if (conferenceIds.Contains(b.Id))
                continue;

            if (bubbles.IsModerator(b) == true || bubbles.IsCreator(b) == true)
            {
                options.Add(new(b.Name));
                bubbleByIndex.Add(b.Id);
            }
        }

        UnityExecutor.Execute(() =>
        {
            bubblesDropdown.ClearOptions();
            bubblesDropdown.AddOptions(options);
            bubblesDropdown.enabled = (bubbleByIndex.Count > 0);
        });
    }

    private void RefreshConference()
    {
        UnityExecutor.Execute(() =>
        {
            List<TMP_Dropdown.OptionData> options = new();
            conferenceByIndex = new List<string>();
            Conferences conferences = rainbow.RainbowApplication.GetConferences();
            Bubbles bubbles = rainbow.RainbowApplication.GetBubbles();
            List<Conference> conferenceList = conferences.ConferenceGetListFromCache();
            AlphabeticConferenceComparer cmp = new(bubbles);
            conferenceList.Sort(cmp);
            foreach (var c in conferenceList)
            {
                Bubble bubble = bubbles.GetBubbleByIdFromCache(c.Id);
                options.Add(new(bubble.Name));
                conferenceByIndex.Add(c.Id);
            }
            conferencesDropdown.ClearOptions();
            conferencesDropdown.AddOptions(options);
            conferenceCallButton.enabled = (conferenceByIndex.Count > 0);
            conferencesDropdown.enabled = (conferenceByIndex.Count > 0);
        });
    }
    private void ConferenceCallClick()
    {
        string id = conferenceByIndex[conferencesDropdown.value];
        rainbow.JoinConference(id);
    }

    private void ConferenceStartClick()
    {
        string id = bubbleByIndex[bubblesDropdown.value];
        rainbow.StartAndJoinConference(id);
    }

    private void P2PCallClick()
    {
        string id = contactByIndex[contactsDropdown.value];
        Contact contact = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(contactByIndex[contactsDropdown.value]);
        Debug.Log($"Must call {contact.DisplayName}");
        rainbow.P2PCallContact(id);
    }
    internal class ContactsComparer : IComparer<Contact>
    {
        public int Compare(Contact x, Contact y)
        {
            return String.Compare(x.DisplayName, y.DisplayName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
    internal class AlphabeticBubbleComparer : IComparer<Bubble>
    {
        public int Compare(Bubble x, Bubble y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    internal class AlphabeticConferenceComparer : IComparer<Conference>
    {
        private Bubbles bubbles;
        public int Compare(Conference x, Conference y)
        {
            string xName, yName;
            xName = bubbles.GetBubbleByIdFromCache(x.Id).Name;
            yName = bubbles.GetBubbleByIdFromCache(y.Id).Name;
            return string.Compare(xName, yName, StringComparison.CurrentCultureIgnoreCase);
        }

        public AlphabeticConferenceComparer(Bubbles bubbles)
        {
            this.bubbles = bubbles;
        }
    }

    private void OnDestroy()
    {
        if( rainbow != null && rainbow.RainbowApplication != null )
        {
            Conferences conferences = rainbow.RainbowApplication.GetConferences();
            Bubbles bubbles = rainbow.RainbowApplication.GetBubbles();

            rainbow.Ready -= RainbowController_Ready;
            rainbow.ConnectionChanged -= Rainbow_ConnectionChanged;
            if (rainbow.RainbowWebRTC != null )
            {
                rainbow.RainbowWebRTC.CallUpdated -= RainbowWebRTC_CallUpdated1;
            }
            if ( bubbles != null)
            {
                bubbles.BubbleAffiliationChanged -= MakeCallUI_BubbleAffiliationChanged;
                bubbles.BubbleMemberUpdated -= MakeCallUI_BubbleMemberUpdated;
            }
            if ( conferences != null )
            {
                conferences.ConferenceUpdated -= RefreshConferenceAndBubbleAfterUpdate;
                conferences.ConferenceRemoved -= RefreshConferenceAndBubbleAfterRemove;
            }

        }

    }
}