using System;
using System.Collections.Generic;
using Rainbow;
using Rainbow.Model;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Scriptable object to handle the current call state
    /// </summary>
    /// </summary>
    [CreateAssetMenu(fileName = "New CallState", menuName = "Game State/Call state")]
    public class CallState : ScriptableObject
    {
        #region public fields

        [NonSerialized]
        public Contact UserContact;

        /// <summary>
        /// Get a read-only view of the currently connected contacts.
        /// Each entry consists of <Contact.Id, Contact>
        /// </summary>
        public IReadOnlyDictionary<string, Contact> ConnectedContacts { get => _connectedContacts; }

        /// <summary>
        /// Specifies whether this application is the initiator of the call
        /// </summary>
        [NonSerialized]
        public bool IsInitiator = false;

        /// <summary>
        /// Specifies whether current call is a conference or not
        /// </summary>
        [NonSerialized]
        public bool IsConference = false;

        // backing property
        [NonSerialized]
        private bool _isConferenceActive = false;

        /// <summary>
        /// Specifies whether a conference is currently in progress
        /// </summary>
        public bool IsConferenceActive
        {
            get => _isConferenceActive;
            set
            {
                if (value == _isConferenceActive)
                {
                    return;
                }
                _isConferenceActive = value;
                OnConferenceStateChanged?.Invoke(_isConferenceActive);
            }
        }

        #endregion // public fields

        #region events

        public enum ContactChangeType
        {
            Added, Removed
        }

        public delegate void ContactChangedHandler(Contact contact, ContactChangeType type);

        public event ContactChangedHandler OnContactChanged;

        /// <summary>
        /// Handler for conference state changed events
        /// </summary>
        /// <param name="isActive">True, if a conference is active, false otherwise</param>
        public delegate void ConferenceStateChangedHandler(bool isActive);

        /// <summary>
        /// Event called whenever the state of the current conference changes.
        /// The state is either connected or not
        /// </summary>
        public event ConferenceStateChangedHandler OnConferenceStateChanged;

        #endregion // events

        #region public methods

        /// <summary>
        /// Adds a contact to the state, if it isn't there already.
        /// If the contact was added, the OnContactAdded event is raised
        /// </summary>
        /// <param name="contact">The contact to add</param>
        /// <returns>True, if the contact was added, false if it was already added before</returns>
        public bool AddContact(Contact contact)
        {
            bool added = _connectedContacts.TryAdd(contact.Id, contact);

            if (added)
            {
                OnContactChanged?.Invoke(contact, ContactChangeType.Added);
            }

            return added;
        }

        /// <summary>
        /// Removes a contact from the state, if it is contained.
        /// If the contact was removed, the OnContactRemoved event is raised
        /// </summary>
        /// <param name="contact">The contact to remove</param>
        /// <returns>True, if the contact was removed, false if it wasn't contained</returns>
        public bool RemoveContact(Contact contact)
        {
            return RemoveContact(contact.Id);
        }

        /// <summary>
        /// Removes a contact from the state, if it is contained.
        /// If the contact was removed, the OnContactRemoved event is raised
        /// </summary>
        /// <param name="contactId">The id (Contact.Id) of the contact</param>
        /// <returns>True, if the contact was removed, false if it wasn't contained</returns>
        public bool RemoveContact(string contactId)
        {

            bool removed = _connectedContacts.Remove(contactId, out Contact c);

            if (removed)
            {
                OnContactChanged?.Invoke(c, ContactChangeType.Removed);
            }

            return removed;
        }

        /// <summary>
        /// Removes all currently connected contacts.
        /// For each removed contact, the OnContactRemoved callback is invoked
        /// </summary>
        public void Clear()
        {
            foreach (KeyValuePair<string, Contact> pair in _connectedContacts)
            {
                OnContactChanged?.Invoke(pair.Value, ContactChangeType.Removed);
            }
            _connectedContacts.Clear();
        }

        /// <summary>
        /// Update the current list of call participants from a map of participants. 
        /// Such a map is given by Rainbow OnConferenceParticipantsUpdate events
        /// </summary>
        /// <param name="participants"></param>
        /// <param name="contacts"></param>
        public void UpdateFromParticipants(Dictionary<string, Participant> participants, Contacts contacts)
        {
            // we will find a removal and added set
            List<string> addIds = new();
            List<string> removeIds = new();

            string userId = UserContact?.Id ?? "";

            // from the documentation (https://developers.openrainbow.com/doc/sdk/csharp/core/lts/api/Rainbow.Model.Participant), we can compare participant ids with contact ids

            // since we don't have an explicit status of whether a participant connected or disconnected, we compare both sets
            // complexity should b O(n), since set/map lookup should be O(1)

            // first search for removals -> current contacts that are not in the participant list
            foreach (string contactId in _connectedContacts.Keys)
            {
                if (participants.ContainsKey(contactId))
                {
                    // contact is still in the call -> nothing to do
                    continue;
                }

                // contact is not in the participants -> remove
                removeIds.Add(contactId);
            }

            // do the same the other way around
            foreach (string partId in participants.Keys)
            {
                if (userId == partId || _connectedContacts.ContainsKey(partId))
                {
                    // participant is already in list
                    continue;
                }

                // participant is not yet in the current list -> add
                addIds.Add(partId);
            }

            // add and remove

            foreach (string id in addIds)
            {
                Contact c = contacts.GetContactFromContactId(id);
                if (c == null)
                {
                    Debug.LogWarning($"[CallState] Invalid contact id: {id}");
                    continue;
                }

                AddContact(c);
            }

            foreach (string id in removeIds)
            {
                RemoveContact(id);
            }
        }

        #endregion // public methods

        #region internal

        // we do not expect many contacts in a call, so a list is sufficient.
        // also sets are not as nicely supported in c# compared to lists and dictionaries
        // Each entry consists of <Contact.Id, Contact>
        private readonly Dictionary<string, Contact> _connectedContacts = new();

        #endregion // internal
    }
} // end namespace Cortex