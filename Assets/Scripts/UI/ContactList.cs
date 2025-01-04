using System;
using System.Collections.Generic;
using System.Threading;
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using UnityEngine;

namespace Cortex
{
    public class ContactList : BaseStartOrEnabled
    {
        public RainbowAvatarLoader AvatarLoader;

        public GameObject ListItemPrefab;

        public RectTransform ContentPanel;

        public Contact CurrentlySelected
        {
            get; private set;
        }
        private ContactEntry selectedEntry;
        public event Action<ContactList, Contact> OnSelectionChanged;

        private readonly ResetCancellationToken cancelLoad = new();


        public GameObject rainbowGameobject;

        // Start is called before the first frame update
        void Awake()
        {
            if (AvatarLoader == null)
            {
                AvatarLoader = FindFirstObjectByType<RainbowAvatarLoader>();
            }
        }

        protected override void OnStartOrEnable()
        {
            UpdateList();
            ConnectionModel.Instance.Contacts.ContactAggregatedPresenceChanged += ContactPresenceChanged;
        }

        void OnDisable()
        {
            Contacts contacts = ConnectionModel.Instance.Contacts;
            if (contacts != null)
            {
                contacts.ContactAggregatedPresenceChanged -= ContactPresenceChanged;
            }
            CurrentlySelected = null;
            selectedEntry = null;

            foreach (Transform child in ContentPanel)
            {
                Destroy(child.gameObject);
            }
        }

        private void ContactPresenceChanged(object sender, PresenceEventArgs e)
        {
            UnityExecutor.Execute(() =>
            {
                Contact c = ConnectionModel.Instance.Contacts.GetContactFromContactJid(e.Presence.BasicNodeJid);
                if (c == null)
                {
                    return;
                }

                // TODO handle this via internal list or something else that is nicer/more performant
                foreach (Transform child in ContentPanel)
                {

                    if (!child.gameObject.TryGetComponent(out ContactEntry entry))
                    {
                        continue;
                    }

                    if (entry.Contact.Id == c.Id)
                    {
                        entry.SetPresenceLevel(e.Presence.PresenceLevel);
                        return;
                    }
                }
            });
        }

        void OnClickContact(ContactEntry entry)
        {
            if (CurrentlySelected != entry.Contact)
            {
                if (selectedEntry != null)
                {
                    selectedEntry.Selected = false;
                }
                entry.Selected = true;
                selectedEntry = entry;
                CurrentlySelected = entry.Contact;
                OnSelectionChanged?.Invoke(this, entry.Contact);

                rainbowGameobject.GetComponent<ConversationsManager>().DisplayConversationsWithContact(entry.Contact);
            }
        }

        private void UpdateList()
        {
            if (ListItemPrefab == null)
            {
                return;
            }

            foreach (Transform child in ContentPanel)
            {
                Destroy(child.gameObject);
            }

            Contacts contacts = ConnectionModel.Instance.Contacts;

            if (contacts == null)
            {
                return;
            }

            List<Contact> contactList = new();

            foreach (var c in contacts.GetAllContactsFromCache())
            {
                if (c.Id != contacts.GetCurrentContactId() && c.InRoster)
                {
                    contactList.Add(c);
                }
            }
            contactList.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName, StringComparison.CurrentCultureIgnoreCase));

            cancelLoad.Reset();
            var token = cancelLoad.Token;
            foreach (Contact c in contactList)
            {
                Presence p = contacts.GetAggregatedPresenceFromContact(c);
                GameObject item = Instantiate(ListItemPrefab, ContentPanel);
                ContactEntry entry = item.GetComponent<ContactEntry>();
                entry.Contact = c;
                if (p == null)
                {
                    entry.SetPresenceLevel(PresenceLevel.Offline);
                }
                else
                {
                    entry.SetPresenceLevel(p.PresenceLevel);

                }

                LoadAvatar(entry, c, token);
                entry.OnClick += OnClickContact;
            }
        }

        private async void LoadAvatar(ContactEntry entry, Contact contact, CancellationToken cancellationToken)
        {
            try
            {
                Texture2D tex = await AvatarLoader.RequestAvatar(contact, 64, cancellationToken: cancellationToken);

                if (tex != null)
                {
                    UnityExecutor.Execute(() =>
                    {
                        if (entry != null)
                        {
                            entry.ContactInitialsAvatar.AvatarImage = tex;
                        }
                    });
                }

            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // canceled -> nothing to do
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
} // end namespace Cortex