using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Handles the detailed view of a selected bubble
    /// </summary>
    public class BubbleDetails : BaseStartOrEnabled
    {
        public Bubble SelectedBubble
        {
            get => m_selectedBubble;
            set
            {
                m_selectedBubble = value;
                UpdateUi();
            }
        }

        // Backing field for property SelectedBubble
        private Bubble m_selectedBubble;

        [Header("Options")]
        [Range(1, 512)]
        public int BubbleAvatarResolution = 128;

        [Range(1, 512)]
        public int ContactAvatarResolution = 128;

        [Header("Services")]
        public RainbowAvatarLoader AvatarLoader;

        [Header("Ui")]
        [SerializeField]
        private GameObject BubbleMemberListContent;

        [SerializeField]
        public SpriteChangeButton CallButton;

        [SerializeField]
        private RawImage bubbleDetailAvatar;

        [SerializeField]
        private TextMeshProUGUI bubbleName;

        [SerializeField]
        private TextMeshProUGUI bubbleDescriptor;

        [Header("Prefabs")]
        [SerializeField]
        private GameObject MemberItemPrefab;

        private readonly Dictionary<string, ContactEntry> curMemberEntries = new();

        // for cancelling update operations
        private readonly ResetCancellationToken updateCancel = new();
        private readonly ResetCancellationToken updateDetailAvatarCancel = new();

        protected override void OnStartOrEnable()
        {
            if (AvatarLoader == null)
            {
                AvatarLoader = FindFirstObjectByType<RainbowAvatarLoader>();
            }

            Contacts contacts = ConnectionModel.Instance.Contacts;
            Conferences conferences = ConnectionModel.Instance.Conferences;
            Bubbles bubbles = ConnectionModel.Instance.Bubbles;

            if (contacts != null)
            {
                contacts.ContactPresenceChanged += OnContactPresenceChanged;
            }

            if (conferences != null)
            {
                // when someone starts a call, we need to update the selected bubble, in case we are allowed to join
                // this event does NOT trigger, when the conference is stopped. In that case, only the ConferenceRemoved event is raised
                conferences.ConferenceUpdated += OnConferenceUpdated;
                conferences.ConferenceRemoved += OnConferenceRemoved;
            }

            // we catch the bubble avatar changed event, which is triggered after the specific bubble has been refreshed
            AvatarLoader.OnBubbleAvatarChanged += OnBubbleAvatarUpdated;

            CallButton.Switched = false;
            CallButton.Button.interactable = true;

            UpdateUi();

            CallButton.Button.onClick.AddListener(OnCallButtonClick);
        }

        void OnDisable()
        {
            if (!HasStarted)
            {
                return;
            }

            CallButton.Button.onClick.RemoveListener(OnCallButtonClick);

            Contacts contacts = ConnectionModel.Instance.Contacts;
            Conferences conferences = ConnectionModel.Instance.Conferences;
            Bubbles bubbles = ConnectionModel.Instance.Bubbles;

            if (contacts != null)
            {
                contacts.ContactPresenceChanged -= OnContactPresenceChanged;
            }

            if (conferences != null)
            {
                conferences.ConferenceUpdated -= OnConferenceUpdated;
                conferences.ConferenceRemoved -= OnConferenceRemoved;
            }

            AvatarLoader.OnBubbleAvatarChanged -= OnBubbleAvatarUpdated;
            CallButton.Switched = true;

            // cancel still running operations
            updateCancel.Reset();
            updateDetailAvatarCancel.Reset();
        }

        private async void OnBubbleAvatarUpdated(Bubble b, RainbowAvatarLoader.AvatarChange change)
        {
            try
            {
                await UpdateBubbleDetailAvatar(b);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnConferenceRemoved(object sender, IdEventArgs e)
        {
            UpdateCallButton();
        }

        private void OnConferenceUpdated(object sender, ConferenceEventArgs e)
        {
            UpdateCallButton();
        }

        private void OnContactPresenceChanged(object sender, PresenceEventArgs e)
        {
            Contact c = ConnectionModel.Instance.RainbowInterface.RainbowApplication.GetContacts().GetContactFromContactJid(e.Presence.BasicNodeJid);
            if (c == null)
            {
                return;
            }

            UnityExecutor.Execute(() =>
            {
                if (curMemberEntries.TryGetValue(c.Id, out ContactEntry entry))
                {
                    if (entry == null)
                    {
                        return;
                    }

                    entry.SetPresenceLevel(e.Presence.PresenceLevel);
                }
            });
        }

        private void OnCallButtonClick()
        {
            CallButton.Switched = true;
            CallButton.Button.interactable = false;
            StartBubbleConference();
        }

        private void UpdateUi()
        {
            if (!HasStarted)
            {
                return;
            }

            updateCancel.Reset();
            updateDetailAvatarCancel.Reset();

            // remove old
            foreach (Transform child in BubbleMemberListContent.transform)
            {
                Destroy(child.gameObject);
            }

            if (SelectedBubble == null)
            {
                bubbleName.text = "";
                bubbleDescriptor.text = "";
                bubbleDetailAvatar.texture = null;
                return;
            }

            UpdateCallButton();

            bubbleName.text = SelectedBubble.Name;
            bubbleDescriptor.text = SelectedBubble.Topic;

            UpdateAvatarsAndMembers(SelectedBubble, updateCancel.Token);
        }

        private void UpdateCallButton()
        {
            Bubbles bubbles = ConnectionModel.Instance.Bubbles;

            Conferences conferences = ConnectionModel.Instance.Conferences;

            if (SelectedBubble != null)
            {
                // there is also conferences.ConferenceAllowed, but that doesn't seem correct, since it doesn't accept a bubble as an argument?
                bool canStartCall = (bubbles.IsCreator(SelectedBubble) ?? false) || (bubbles.IsModerator(SelectedBubble) ?? false);
                List<Conference> confList = conferences.ConferenceGetListFromCache();
                foreach (var conf in confList)
                {
                    // conference is in progress, we can join
                    // TODO make it clear when start/join is meant
                    if (conferences.GetBubbleByConferenceIdFromCache(conf.Id) == SelectedBubble)
                    {
                        canStartCall = true;
                    }
                }

                CallButton.gameObject.SetActive(canStartCall);
                CallButton.Button.interactable = canStartCall;

                Conference currentConference = conferences.ConferenceGetByIdFromCache(conferences.GetConferenceIdByBubbleIdFromCache(SelectedBubble.Id));

                if (currentConference != null && currentConference.Active)
                {
                    // TODO update state info
                }
            }
            else
            {
                CallButton.gameObject.SetActive(false);
                CallButton.Button.interactable = false;
            }
        }

        private void StartBubbleConference()
        {
            var conferences = ConnectionModel.Instance.Conferences;

            Conference currentConference = conferences.ConferenceGetByIdFromCache(conferences.GetConferenceIdByBubbleIdFromCache(SelectedBubble.Id));

            if (currentConference == null || !currentConference.Active)
            {
                ConnectionModel.Instance.RainbowInterface.StartAndJoinConference(SelectedBubble.Id);
            }
            else
            {
                ConnectionModel.Instance.RainbowInterface.JoinConference(SelectedBubble.Id);
            }
        }

        async void UpdateAvatarsAndMembers(Bubble b, CancellationToken cancellationToken)
        {
            // update avatar and members
            var avatarTask = UpdateBubbleDetailAvatar(b);
            var memberTask = UpdateBubbleMembers(b, cancellationToken);

            try
            {
                await Task.WhenAll(new[] { memberTask, avatarTask });
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        async Task UpdateBubbleDetailAvatar(Bubble b)
        {
            // cancel previous task
            updateDetailAvatarCancel.Reset();
            var token = updateDetailAvatarCancel.Token;

            // disable texture
            UnityExecutor.Execute(() =>
            {
                bubbleDetailAvatar.texture = null;
            });
            await AvatarLoader.UpdateAvatarImage(b, BubbleAvatarResolution, bubbleDetailAvatar, setAlpha: true, cancellationToken: token);
        }

        async Task UpdateBubbleMembers(Bubble b, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var app = ConnectionModel.Instance.RainbowInterface.RainbowApplication;

            // we cache the member dict and reset it, if needed, since a member request might take a bit and meanwhile another bubble could be selected

            curMemberEntries.Clear();

            Bubbles bubbles = app.GetBubbles();

            // Bubble members are not the same as contacts...
            // copy since this might be a global list, if available and might be modified while reading it
            List<BubbleMember> members = new(await RainbowUtils.RetrieveBubbleMembers(app, b, cancellationToken: cancellationToken));

            // after we have the bubble members, we need to get the contacts for them so we can query for more information
            List<Task<Contact>> memberTasks = new();
            foreach (var bm in members)
            {
                memberTasks.Add(RainbowUtils.RetrieveContact(app, bm.UserId, cancellationToken: cancellationToken));
            }

            // gather all update tasks
            ConcurrentBag<Task> avatarUpdateTasks = new();

            // we process retrieved contacts as they come
            while (memberTasks.Any())
            {
                Task<Contact> finished = await Task.WhenAny(memberTasks);
                memberTasks.Remove(finished);

                cancellationToken.ThrowIfCancellationRequested();

                // ignore unsuccessful queries
                if (!finished.IsCompletedSuccessfully)
                {
                    continue;
                }

                Contact contact = await finished;

                Presence p = app.GetContacts().GetAggregatedPresenceFromContact(contact);

                // update ui elements and then populate the created elements with more data
                UnityExecutor.Execute(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // If the view isn't visible or there anymore, we don't do anything
                    if (BubbleMemberListContent == null || !BubbleMemberListContent.activeInHierarchy)
                    {
                        return;
                    }

                    // get the current presence, will be updated automatically afterwards
                    GameObject item = Instantiate(MemberItemPrefab, BubbleMemberListContent.transform);
                    ContactEntry entry = item.GetComponent<ContactEntry>();
                    item.SetActive(true);

                    curMemberEntries.Add(contact.Id, entry);
                    entry.Contact = contact;
                    if (p == null)
                    {
                        entry.SetPresenceLevel(PresenceLevel.Offline);
                    }
                    else
                    {
                        entry.SetPresenceLevel(p.PresenceLevel);
                    }

                    avatarUpdateTasks.Add(LoadAvatar(entry, contact, cancellationToken));
                });
            }

            await Task.WhenAll(avatarUpdateTasks);
        }

        private async Task LoadAvatar(ContactEntry entry, Contact contact, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Texture2D tex = await AvatarLoader.RequestAvatar(contact, ContactAvatarResolution, cancellationToken: cancellationToken);

            if (tex != null)
            {
                UnityExecutor.Execute(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (entry != null)
                    {
                        entry.ContactInitialsAvatar.AvatarImage = tex;
                    }
                });
            }
        }
    }
} // end namespace Cortex