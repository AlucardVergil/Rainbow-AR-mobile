using Rainbow;
using Rainbow.Model;
using Rainbow.WebRTC;
using Rainbow.WebRTC.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CurrentCallController : MonoBehaviour
{
    public  const string ServiceName = "CallController";

    [SerializeField]
    private RainbowController rainbow;

    [SerializeField]
    private bool TestMode = false;

    [SerializeField]
    private AbstractComCardLayoutManager ConferenceCardLayoutManager = null;

    [SerializeField]
    private AbstractComCardLayoutManager P2PCardLayoutManager = null;

    [SerializeField]
    private AbstractParticipantsList ParticipantsList = null;

    public AbstractComCardLayoutManager ComCardLayoutManager = null;

    private HashSet<string> participants = new();
    private Call Call = null; // Cached call
    
    void Start()
    {         
        rainbow.Ready += Rainbow_Ready;
        rainbow.RegisterService(ServiceName, this);
    }

    private void OnDestroy()
    {
        rainbow.Ready -= Rainbow_Ready;
        disposeEvents();
        if (participants != null)
        {
            participants.Clear();
            participants = null;
        }
       
    }
    private void disposeEvents()
    {
        if (rainbow != null)
        {
            if (rainbow.RainbowWebRTC != null)
            {
                rainbow.RainbowWebRTC.CallUpdated -= RainbowWebRTC_CallUpdated;
                rainbow.RainbowWebRTC.OnMediaPublicationUpdated -= RainbowWebRTC_OnMediaPublicationUpdated;
                rainbow.RainbowWebRTC.OnTrack -= RainbowWebRTC_OnTrack;
            }
            if (rainbow.RainbowApplication != null)
            {
                rainbow.RainbowApplication.GetConferences().ConferenceParticipantsUpdated -= ComCardDisplay_ConferenceParticipantsUpdated;
            }
            rainbow.LocalVideoStreamUpdated -= Rainbow_LocalVideoStreamUpdated;
        }

    }
    private void Rainbow_Ready(bool isReadyAndConnected)
    {
        if (isReadyAndConnected)
        {
            rainbow.RainbowWebRTC.CallUpdated += RainbowWebRTC_CallUpdated;
            rainbow.RainbowWebRTC.OnMediaPublicationUpdated += RainbowWebRTC_OnMediaPublicationUpdated;
            rainbow.RainbowWebRTC.OnTrack += RainbowWebRTC_OnTrack;
            rainbow.RainbowApplication.GetConferences().ConferenceParticipantsUpdated += ComCardDisplay_ConferenceParticipantsUpdated;
            rainbow.LocalVideoStreamUpdated += Rainbow_LocalVideoStreamUpdated;
            if (TestMode)
            {
                ComCardLayoutManager = ConferenceCardLayoutManager;
                Contacts contacts = rainbow.RainbowApplication.GetContacts();
                foreach (Contact contact in contacts.GetAllContactsFromCache())
                {
                    AddParticipant(contact);
                }

            }
        }
    }

    private void Rainbow_LocalVideoStreamUpdated(bool isSharing, Rainbow.WebRTC.Abstractions.IMediaStreamTrack videoTrack)
    {
        Contact c = rainbow.RainbowApplication.GetContacts().GetCurrentContact();
        // Debug.LogError($"Rainbow_LocalVideoStreamUpdated triggered track: {videoTrack}");
        bool isInverted = false;
        
        UnityExecutor.Execute(() =>
        {
            if (!isSharing)
            {
                isInverted = rainbow.IsLocalVideoFlipped;
                ComCardLayoutManager.SetLocalVideoTrack(c.Id, videoTrack, isInverted);
            }
            else
            {
                isInverted = rainbow.IsLocalSharingFlipped;
                ComCardLayoutManager.SetLocalSharingTrack(c, videoTrack, isInverted);
            }
        });
    }

    private void ComputeDeltaParticipants(Dictionary<string, Participant> participantsFromEvent, out HashSet<String> addedParticipants, out HashSet<String> removedParticipants)
    {
        HashSet<string> updatedParticipants = new();

        foreach (var p in participantsFromEvent.Keys)
        {
            updatedParticipants.Add(p);
        }

        addedParticipants = new HashSet<string>(updatedParticipants);
        addedParticipants.ExceptWith(participants);
        removedParticipants = new HashSet<string>(participants);
        HashSet<string> oldParticipants = new HashSet<string>(updatedParticipants);
        oldParticipants.IntersectWith(participants);

        removedParticipants.ExceptWith(oldParticipants);
        //Debug.LogError($"known participants at this time is {participants.Count} : {string.Join(",", participants)}");
        //Debug.LogError($"changeparticipants: new : {addedParticipants.Count} : {string.Join(",", addedParticipants)}");
        //Debug.LogError($"changeparticipants removed: {removedParticipants.Count} : {string.Join(",", removedParticipants)}");
    }

    private void ComCardDisplay_ConferenceParticipantsUpdated(object sender, Rainbow.Events.ConferenceParticipantsEventArgs e)
    {
        var participantsInCall = rainbow.RainbowApplication.GetConferences().ConferenceGetParticipantsFromCache(e.ConferenceId);
        if (Call == null)
        {
            // Debug.Log($"exit early: no call");
            // The call is on another resource: ignore
            return;
        }

        if (Call.ConferenceId != e.ConferenceId)
        {
            // Debug.Log($"exit early: not our conference");
            // this is not our call : ignore
            return;
        }

        // Debug.LogError($"Participant updated {e.Participants.Count}");
        HashSet<string> addedParticipants, removedParticipants;
        ComputeDeltaParticipants(participantsInCall, out addedParticipants, out removedParticipants);

        Debug.Log($"Participants: before: {participants.Count} in event {e.Participants.Count} in Call {participantsInCall.Count} new {addedParticipants.Count} removed {removedParticipants.Count}");
        // Store the new list of participants.
        participants.Clear();
        foreach (var p in e.Participants.Keys)
        {
            participants.Add(p);
        }

        rainbow.RainbowExecutor.Execute(() =>
        {
            foreach (var p in addedParticipants)
            {
                Contact participant = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(e.Participants[p].Id);
                AddParticipant(e.Participants[p].Id);
            }

            foreach (var p in removedParticipants)
            {
                Contact participant = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(p);
                if (participant != null)
                {
                    RemoveParticipant(participant);
                }
            }
        });
    }

    private void AddParticipant(string participantId)
    {
        Contact contact = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(participantId);
        if (contact != null)
        {
            AddParticipant(contact);
            return;
        }

        rainbow.RainbowExecutor.Execute(() =>
        {
            rainbow.RainbowApplication.GetContacts().GetContactFromContactIdFromServer(participantId, result =>
            {
                if (result.Result.Success)
                {
                    AddParticipant(result.Data);
                }
                else
                {
                    Debug.LogError($"GetContact from server failed for {participantId}");
                }
            });
        });
    }

    private void RainbowWebRTC_OnTrack(string callId, MediaStreamTrackDescriptor mediaStreamTrackDescriptor)
    {

        if (mediaStreamTrackDescriptor.PublisherId == rainbow.RainbowApplication.GetContacts().GetCurrentContactId())
        {
            return;
        }

        if (mediaStreamTrackDescriptor != null)
        {
            UnityExecutor.Execute(() =>
            { 
                // Debug.Log($"OnTrack remoteTrack track {mediaStreamTrackDescriptor.MediaStreamTrack} media {mediaStreamTrackDescriptor.Media}");
                if (mediaStreamTrackDescriptor.Media == Call.Media.VIDEO)
                {
                    ComCardLayoutManager.SetRemoteVideoTrack(mediaStreamTrackDescriptor.PublisherId, mediaStreamTrackDescriptor.MediaStreamTrack);
                }
                else if (mediaStreamTrackDescriptor.Media == Call.Media.SHARING)
                {
                    Contact c = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(mediaStreamTrackDescriptor.PublisherId);
                    ComCardLayoutManager.SetRemoteSharingTrack(c, mediaStreamTrackDescriptor.MediaStreamTrack);
                }
            });
        }
    }

    private void RainbowWebRTC_OnMediaPublicationUpdated(object sender, MediaPublicationEventArgs e)
    {
        string myID = rainbow.RainbowApplication.GetContacts().GetCurrentContactId();

        if (e.MediaPublication == null) { return; }

        UnityExecutor.Execute(() =>
        {
            if (e.MediaPublication.PublisherId == myID)
            {
                if (e.Status == MediaPublicationStatus.PEER_STARTED)
                {
                    if (e.MediaPublication.Media == Call.Media.VIDEO)
                    {

                    }
                }
                else if (e.Status == MediaPublicationStatus.PEER_STOPPED)
                {
                    if (e.MediaPublication.Media == Call.Media.VIDEO)
                    {
                        // Debug.Log("LOCAL VIDEO STOPPED");
                        ComCardLayoutManager.SetLocalVideoTrack(myID, null, false);
                    }
                    else if (e.MediaPublication.Media == Call.Media.SHARING)
                    {
                        // Debug.Log("LOCAL SHARING STOPPED");
                        ComCardLayoutManager.SetLocalSharingTrack(rainbow.RainbowApplication.GetContacts().GetCurrentContact(), null,false);
                    }
                }
                return;
            }
            try
            {
                Contact contact = rainbow.RainbowApplication.GetContacts().GetContactFromContactId(e.MediaPublication.PublisherId);
                if (e.Status == MediaPublicationStatus.PEER_STOPPED)
                {
                    UnityExecutor.Execute(() => { 
                        if (e.MediaPublication.Media == Call.Media.VIDEO)
                        {
                            ComCardLayoutManager.SetRemoteVideoTrack(e.MediaPublication.PublisherId, null);
                        }
                        else if (e.MediaPublication.Media == Call.Media.SHARING)
                        {
                            ComCardLayoutManager.SetRemoteSharingTrack(contact, null);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{ex.Message} {ex.StackTrace}");
            }
        });
    }

    private void RainbowWebRTC_CallUpdated(object sender, Rainbow.Events.CallEventArgs e)
    {
        // If the call is finished, inform the layouter.
        if (e.Call.CallStatus == Call.Status.UNKNOWN)
        {
            Call = null;
            participants.Clear();
            if( ParticipantsList != null )
            {
                UnityExecutor.Execute(() => { ParticipantsList.ClearAll(); });
            }
            if( ComCardLayoutManager != null)
            {
                UnityExecutor.Execute(() => { ComCardLayoutManager.ClearAll(); });
            }
        }

        if (e.Call.CallStatus == Call.Status.ACTIVE || e.Call.CallStatus == Call.Status.CONNECTING )
        {
            // If we already are in a call nothing to do, it's just that the call went to active again.
            if (Call != null)
                return;

            Call = e.Call;
            if (!e.Call.IsConference)
            {
                ComCardLayoutManager = P2PCardLayoutManager;
                List<String> participants = new List<String>
                {
                    rainbow.RainbowApplication.GetContacts().GetCurrentContactId(),
                    e.Call.PeerId
                };
                AddParticipants(participants);
                return;
            }

            ComCardLayoutManager = ConferenceCardLayoutManager;
            
            try
            {
                // We are entering a conference call:
                // we didn't have a call yet, so retrieve the participants from the cache and create cards.            
                var participantsInCall = rainbow.RainbowApplication.GetConferences().ConferenceGetParticipantsFromCache(e.Call.ConferenceId);

                participants.Clear();
                var particpantsToAdd = new List<String>();

                foreach (var participant in participantsInCall.Values)
                {
                    participants.Add(participant.Id);
                    particpantsToAdd.Add(participant.Id);
                }

                // Create the cards, and when they are created, request remote video streams
                AddParticipants(particpantsToAdd, () =>
                {
                    rainbow.RainbowExecutor.Execute(
                        () => { rainbow.SubscribeToPendingMediaPublications(); });
                });


            } catch(Exception ex )
            {
                Debug.LogError($"Failed to get participants: {ex.Message} {ex.StackTrace}");
            }
            return;
        }
    }

    // Adds a batch of cards, then optionnaly execute an Action
    private void AddParticipants(ICollection<string> participantsIds, Action callback = null)
    {
        UnityExecutor.Execute(() =>
        {
            ComCardLayoutManager.FreezeRefresh(true);
            foreach (var participantId in participantsIds)
            {
                AddParticipant(participantId);
            }
            ComCardLayoutManager.FreezeRefresh(false);

            if (callback != null)
            {
                callback();
            }
        });
    }

    public void AddParticipant(Contact contact)
    {
        bool isLocalContact = rainbow.RainbowApplication.GetContacts().GetCurrentContactId() == contact.Id;
        UnityExecutor.Execute(() =>
        {
            ComCardLayoutManager.AddParticipant(contact, isLocalContact);

            if( ParticipantsList != null)
            {
                ParticipantsList.AddParticipant(contact, isLocalContact );
            }
        });
    }

    public void RemoveParticipant(Contact contact)
    {
        UnityExecutor.Execute(() =>
        {
            ComCardLayoutManager.RemoveParticipant(contact);
            if( ParticipantsList != null)
            {
                ParticipantsList.RemoveParticipant(contact);
            }
        });
    }
}
