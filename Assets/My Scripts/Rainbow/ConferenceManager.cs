using System;
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using UnityEngine;

public class ConferenceManager : MonoBehaviour
{
    private Rainbow.Application rbApplication;
    private Conferences rbConferences;
    private string conferenceId;




    public void InitializeConferenceManager()
    {
        // Initialize Rainbow Application
        rbApplication = RainbowManager.Instance.GetRainbowApplication();
        rbConferences = rbApplication.GetConferences();

        // Register for event listeners
        rbConferences.ConferenceUpdated += Conferences_ConferenceUpdated;
        rbConferences.ConferenceParticipantsUpdated += Conferences_ConferenceParticipantsUpdated;
        rbConferences.ConferenceOwnerUpdated += Conferences_ConferencePublishersUpdated;
        rbConferences.ConferenceTalkersUpdated += Conferences_ConferenceTalkersUpdated;

        // Check if conference is allowed
        CheckConferencePermissions();
    }

    // Check if WebRTC and PSTN rbConferences are allowed
    private void CheckConferencePermissions()
    {
        if (rbConferences.ConferenceAllowed())
        {
            Debug.Log("WebRTC conference is allowed.");
        }
        else
        {
            Debug.Log("WebRTC conference is not allowed.");
        }

        if (rbConferences.ConferenceAllowed())
        {
            Debug.Log("PSTN conference is allowed.");
        }
        else
        {
            Debug.Log("PSTN conference is not allowed.");
        }
    }

    // Start Personal Conference (PSTN)
    public void StartPersonalConference(string bubbleId)
    {
        rbConferences.ConferenceStart(bubbleId, callbackStart =>
        {
            if (callbackStart.Result.Success)
            {
                Debug.Log("PSTN Conference started successfully.");
                JoinPersonalConference(bubbleId);
            }
            else
            {
                HandleError(callbackStart.Result);
            }
        });
    }

    // Join a Personal Conference
    private async void JoinPersonalConference(string bubbleId)
    {
        bool asModerator = true;           // User wants to join as moderator
        bool muted = false;                 // User wants to join muted
        string phoneNumber = "+306972186490";  // Use your phone number here
        string country = "GRC";               // Country code

        Debug.Log("Bubble id 2 = " + bubbleId);

        await UnityMainThreadDispatcher.Instance().EnqueueAsync(() => 
        {
            rbConferences.ConferenceJoin(bubbleId, muted, phoneNumber, country, callback =>
            {
                if (callback.Result.Success)
                {
                    Debug.Log("User joined the Personal Conference.");
                }
                else
                {
                    HandleError(callback.Result);
                }
            });
        });

        
    }

    // Handle Errors (Incorrect use or exceptions)
    private void HandleError(SdkError result)
    {
        if (result.Type == SdkError.SdkErrorType.IncorrectUse)
        {
            Debug.Log("Incorrect parameters: " + result.IncorrectUseError.ErrorMsg);
        }
        else
        {
            Debug.LogError("Exception occurred: " + result.ExceptionError);
        }
    }

    // Fetch PSTN Conference Phone Numbers
    //public void GetPersonalConferencePhoneNumbers()
    //{
    //    rbConferences.PersonalConferenceGetPhoneNumbers(callback =>
    //    {
    //        if (callback.Result.Success)
    //        {
    //            PersonalConferencePhoneNumbers phoneNumbers = callback.Data;
    //            Debug.Log("Phone numbers to join the PSTN Conference: " + phoneNumbers);
    //        }
    //        else
    //        {
    //            HandleError(callback.Result);
    //        }
    //    });
    //}

    //// Fetch Passcodes for the Conference
    //public void GetConferencePassCodes()
    //{
    //    rbConferences.PersonalConferenceGetPassCodes(callback =>
    //    {
    //        if (callback.Result.Success)
    //        {
    //            ConferencePassCodes passCodes = callback.Data;
    //            string moderatorCode = passCodes.ModeratorPassCode;
    //            string participantCode = passCodes.ParticipantPassCode;
    //            Debug.Log($"Moderator passcode: {moderatorCode}, Participant passcode: {participantCode}");
    //        }
    //        else
    //        {
    //            HandleError(callback.Result);
    //        }
    //    });
    //}

    //// Fetch the Public URL for the Conference
    //public void GetConferencePublicUrl()
    //{
    //    rbConferences.PersonalConferenceGetPublicUrl(callback =>
    //    {
    //        if (callback.Result.Success)
    //        {
    //            string url = callback.Data;
    //            Debug.Log("Public URL for the conference: " + url);
    //        }
    //        else
    //        {
    //            HandleError(callback.Result);
    //        }
    //    });
    //}

    // Lock or Unlock the Conference
    public void LockOrUnlockConference(string bubbleId, bool lockConference)
    {
        rbConferences.ConferenceLockOrUnlocked(bubbleId, lockConference, callback =>
        {
            if (callback.Result.Success)
            {
                string action = lockConference ? "locked" : "unlocked";
                Debug.Log($"Conference is {action}.");
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    // Mute/Unmute all participants in the conference
    public void MuteOrUnmuteConference(string bubbleId, bool mute)
    {
        rbConferences.ConferenceMuteOrUnmute(bubbleId, mute, callback =>
        {
            if (callback.Result.Success)
            {
                string action = mute ? "muted" : "unmuted";
                Debug.Log($"Conference participants are {action}.");
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    // Stop the conference (end the session)
    public void StopConference(string bubbleId)
    {
        rbConferences.ConferenceStop(bubbleId, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Conference stopped successfully.");
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    // Event handler: Conference Updated
    private void Conferences_ConferenceUpdated(object sender, ConferenceEventArgs evt)
    {
        if (evt.Conference.Id == conferenceId)
        {
            Conference conference = evt.Conference;
            if (conference.Active)
            {
                Debug.Log("Conference is active.");
            }
        }
    }

    // Event handler: Conference Participants Updated
    private void Conferences_ConferenceParticipantsUpdated(object sender, ConferenceParticipantsEventArgs evt)
    {
        if (evt.ConferenceId == conferenceId)
        {
            Debug.Log("Participants updated.");
        }
    }

    // Event handler: Conference Publishers Updated
    private void Conferences_ConferencePublishersUpdated(object sender, ConferenceOwnerEventArgs evt)
    {
        if (evt.ConferenceId == conferenceId)
        {
            Debug.Log("Publishers updated.");
        }
    }

    // Event handler: Conference Talkers Updated
    private void Conferences_ConferenceTalkersUpdated(object sender, ConferenceTalkersEventArgs evt)
    {
        if (evt.ConferenceId == conferenceId)
        {
            Debug.Log("Talkers updated.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        rbConferences.ConferenceUpdated -= Conferences_ConferenceUpdated;
        rbConferences.ConferenceParticipantsUpdated -= Conferences_ConferenceParticipantsUpdated;
        rbConferences.ConferenceOwnerUpdated -= Conferences_ConferencePublishersUpdated;
        rbConferences.ConferenceTalkersUpdated -= Conferences_ConferenceTalkersUpdated;
    }
}
