using System;
using System.Collections.Generic;
using UnityEngine;
using Rainbow;
using Rainbow.Model;
using Rainbow.Events;
using TMPro;
using UnityEngine.UI;

public class BubbleManager : MonoBehaviour
{
    private Rainbow.Application rbApplication;
    private Contacts rbContacts;
    private Contact myContact;
    private Bubbles rbBubbles;
    private Conferences rbConferences;

    public GameObject bubblePrefab;
    public Transform bubblesScrollViewContent;

    private Bubble currentSelectedBubble;


    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    GetAllBubbles();
        //}
    }


    private void Start()
    {
        //InitializeBubblesManager();
    }

    public void InitializeBubblesManager() // Probably will need to assign the variables in the other function bcz they are called too early and not assigned (TO CHECK)
    {
        rbApplication = RainbowManager.Instance.GetRainbowApplication();

        rbBubbles = rbApplication.GetBubbles();

        rbContacts = rbApplication.GetContacts();
        myContact = rbContacts.GetCurrentContact();

        rbConferences = rbApplication.GetConferences();

        // Subscribe to invitation received event
        rbBubbles.BubbleInvitationReceived += Bubbles_BubbleInvitationReceived;
    }


    #region Bubbles Management

    public void GetAllBubbles()
    {
        rbBubbles = rbApplication.GetBubbles();
        myContact = rbContacts.GetCurrentContact();

        rbBubbles.GetAllBubbles(async callback =>
        {
            if (callback.Result.Success)
            {
                List<Bubble> listBubbles = callback.Data;
                foreach (Bubble bubble in listBubbles)
                {
                    if (bubble.Creator == myContact.Id)
                    {
                        Debug.Log("You are the creator of the bubble: " + bubble.Name);
                    }
                    else
                    {
                        Debug.Log("You are a member of the bubble: " + bubble.Name);
                    }
                    
                    await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                    {
                        GameObject bubbleGameobject = Instantiate(bubblePrefab, bubblesScrollViewContent);

                        bubbleGameobject.GetComponentInChildren<TMP_Text>().text = bubble.Name;

                        bubbleGameobject.GetComponent<BubbleGameobject>().bubble = bubble;

                        bubbleGameobject.GetComponent<Button>().onClick.AddListener(() => {

                            GetComponent<MenuManager>().OpenCloseChatPanels(3);

                            // Used siblingIndex instead of i because in addlistener it used the last assigned value of i in everything
                            Bubble bubble = listBubbles[bubbleGameobject.transform.GetSiblingIndex()];

                            currentSelectedBubble = bubble;


                        });

                    });

                }
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void GetBubbleMembers(Bubble bubble)
    {
        foreach (BubbleMember member in bubble.Users)
        {
            if (member.Status == Bubble.MemberStatus.Accepted)
            {
                Debug.Log("Accepted member: " + member.UserId);
            }

            if (member.Privilege == Bubble.MemberPrivilege.Owner)
            {
                Debug.Log("Owner: " + member.UserId);
            }
        }
    }

    public void AddMemberToBubble(Bubble bubble, Contact contact, string privilege = Bubble.MemberPrivilege.User)
    {
        rbBubbles.AddContactById(bubble.Id, contact.Id, privilege, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Invitation sent to: " + contact.DisplayName);
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void RemoveMemberFromBubble(Bubble bubble, Contact contact)
    {
        rbBubbles.RemoveContactById(bubble.Id, contact.Id, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log(contact.DisplayName + " has been removed from the bubble.");
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void LeaveBubble(Bubble bubble)
    {
        rbBubbles.LeaveBubble(bubble.Id, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("You left the bubble: " + bubble.Name);
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    #endregion

    #region Invitations Management

    private void Bubbles_BubbleInvitationReceived(object sender, BubbleInvitationEventArgs evt)
    {
        Debug.Log("Invitation received from bubble: " + evt.BubbleName);
        AcceptBubbleInvitation(evt.BubbleId); // NOTE: for testing. To remove from here
    }


    public void AcceptBubbleInvitation(string bubbleId)
    {
        rbBubbles.AcceptInvitation(bubbleId, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("You are now a member of the bubble.");
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void DeclineBubbleInvitation(string bubbleId)
    {
        rbBubbles.DeclineInvitation(bubbleId, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Invitation to bubble declined.");
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void CheckPendingInvitations(Bubble bubble)
    {
        bool pendingInvitation = false;
        foreach (BubbleMember member in bubble.Users)
        {
            if (member.UserId == myContact.Id && member.Status == Bubble.MemberStatus.Invited)
            {
                pendingInvitation = true;
                break;
            }
        }

        if (pendingInvitation)
        {
            Debug.Log("You have a pending invitation in bubble: " + bubble.Name);
        }
    }

    #endregion

    #region Bubble Creation and Management

    public void CreateBubble(string bubbleName, string bubbleTopic, string visibility = Bubble.BubbleVisibility.AsPrivate)
    {
        rbBubbles.CreateBubble(bubbleName, bubbleTopic, visibility, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Bubble created: " + bubbleName);
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void UpdateBubble(Bubble bubble, string newName, string newTopic, string visibility = Bubble.BubbleVisibility.AsPrivate)
    {
        rbBubbles.UpdateBubble(bubble.Id, newName, newTopic, visibility, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Bubble updated: " + newName);
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }


    public void ArchiveBubble(Bubble bubble)
    {
        rbBubbles.ArchiveBubble(bubble.Id, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Bubble archived: " + bubble.Name);
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    public void DeleteBubble(Bubble bubble)
    {
        rbBubbles.DeleteBubble(bubble.Id, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("Bubble deleted: " + bubble.Name);
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    #endregion

    private void HandleError(SdkError error)
    {
        if (error.Type == SdkError.SdkErrorType.IncorrectUse)
        {
            Debug.LogError("Error: " + error.IncorrectUseError.ErrorMsg);
        }
        else if (error.Type == SdkError.SdkErrorType.Exception)
        {
            Debug.LogError("Exception: " + error.ExceptionError.Message);
        }
    }
}
