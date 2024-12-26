using Rainbow.WebRTC.Unity;
using UnityEngine;
using UnityEngine.UI;
using Rainbow.Model;
using System.Collections.Generic;
using System;
using TMPro;
using SimpleDataChannelRainbow;
using System.Threading.Tasks;
using System.Threading;

[SelectionBase]
public class DataChannelController : MonoBehaviour
{
    public int SizePacket = 100000;
    public int Frequency = 1;
    public int Delay = 30;

    public RainbowController rainbow;
    private Button requestDataChannelButton;
    private TMP_InputField Output;
    private TMP_Text requestDataChannelButtonLabel;
    private Button pingDataChannelButton;
    private Button SendBulkDataChannelButton;
    private List<Contact> contactByIndex;
    private TMP_Dropdown contactsDropdown;
    private SimpleDataChannelService DataChannelService;
    private CanvasGroup Canvasgroup;
    private bool IsSendingBulk = false;
    private int nbExpectedBytes; // just to make it easy, but should be "per datachannel"

    void Start()
    {
        foreach (Transform child in transform)
        {
            switch (child.name)
            {
                case "RequestDataChannelButton":
                    requestDataChannelButton = child.GetComponent<Button>();
                    break;
                case "SelectedContactForDC":
                    contactsDropdown = child.GetComponent<TMP_Dropdown>();
                    break;
                case "PingDataChannelButton":
                    pingDataChannelButton = child.GetComponent<Button>();
                    break;
                case "SendBulkDataChannelButton":
                    SendBulkDataChannelButton = child.GetComponent<Button>();
                    break;
                case "Output":
                    Output = child.GetComponent<TMP_InputField>();
                    break;
                default:
                    break;
            }
        }
        requestDataChannelButtonLabel = requestDataChannelButton.GetComponentInChildren<TMP_Text>();
        Canvas canvas = transform.parent?.GetComponent<Canvas>();

        contactsDropdown.onValueChanged.AddListener(delegate { ContactsDropdown_onValueChanged(contactsDropdown); });

        // Hide the button
        Canvasgroup = GetComponent<CanvasGroup>();
        Canvasgroup.alpha = 0;
        rainbow.Ready += Rainbow_Ready;
        // handle buttons
        requestDataChannelButton.onClick.AddListener(RequestDataChannelClicked);
        pingDataChannelButton.onClick.AddListener(PingDataChannelClicked);
        SendBulkDataChannelButton.onClick.AddListener(SendBulkDataChannelButtonClicked);
    }

    private void SendBulkDataChannelButtonClicked()
    {
        SendBulkDataChannelButton.interactable = false;
        Contact contact = contactByIndex[contactsDropdown.value];
        var channel = DataChannelService.GetDataChannel(contact);

        Task.Factory.StartNew(()=>Send50MBToDataChannel(channel));
    }

    void Send50MBToDataChannel(DataChannel dc )
    {        
        byte[] packet = new byte[SizePacket];
        int nbPackets = 500;
        
        try
        {
            dc.Send($"bulk{nbPackets*SizePacket}");

            IsSendingBulk = true;
            for ( int i = 0; i < nbPackets; i++ )
            {                
                for (int j = 0; j < SizePacket; j++)
                {
                    packet[j] = (byte)((i*SizePacket + j ) % byte.MaxValue);
                }
                dc.Send(packet);
                UnityExecutor.Execute(() =>
                {
                    if( i ==  nbPackets - 1 )
                    {
                        Output.text = $"Sent {nbPackets * SizePacket} bytes .";
                    } else
                    {
                        Output.text = $"Sent {SizePacket * (1 + i)} bytes / {nbPackets * SizePacket} ({i + 1}/{nbPackets}).";
                    }
                    

                });
                if( i%Frequency == Frequency-1)
                   Thread.Sleep(Delay);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception " + ex.Message + " " + ex.StackTrace);
        }
        finally
        {
            IsSendingBulk = false;
            UnityExecutor.Execute(() =>
            {
                refreshUI();
            });
        }
    }
    private void PingDataChannelClicked()
    {
        Contact contact = contactByIndex[contactsDropdown.value];
        var channel = DataChannelService.GetDataChannel(contact);
        channel.Send("ping");
    }

    private void UpdateListOfContacts()
    {
        List<TMP_Dropdown.OptionData> options = new();
        Contact previousContact = null;
        int IndexToRestore = -1;
        if(contactByIndex != null && contactByIndex.Count > 0)
        {
            previousContact = contactByIndex[contactsDropdown.value];
        }
        contactByIndex = new List<Contact>();
        
        Rainbow.Contacts contacts = rainbow.RainbowApplication.GetContacts();
        foreach (var c in contacts.GetAllContactsFromCache())
        {
            if (c.Id != contacts.GetCurrentContactId())
            {
                string reason;
                if (String.IsNullOrEmpty(DataChannelService.FindUsableResource(c, out reason)))
                    continue;
                if (previousContact != null && previousContact.Id == c.Id)
                    IndexToRestore = contactByIndex.Count;
                options.Add(new(c.DisplayName));
                contactByIndex.Add(c);
            }
        }
        contactsDropdown.ClearOptions();
        contactsDropdown.AddOptions(options);
        if( IndexToRestore != -1 )
        {
            contactsDropdown.value = IndexToRestore;
        }

        Canvasgroup.alpha = ( options.Count > 0 ) ? 1 : 0;
        refreshUI();
    }

    private void ContactsDropdown_onValueChanged(TMP_Dropdown sender)
    {
        Debug.Log("onvaluechanged");
        refreshUI();
    }

    private void refreshUI()
    {        
        if(contactByIndex == null || contactByIndex.Count == 0 )
        {
            return;
        }
        var selectedContact = contactByIndex[contactsDropdown.value];
        bool hasDataChannel = false;


        if (DataChannelService != null)
        {
            hasDataChannel = DataChannelService.HasDataChannel(selectedContact);
            Debug.Log($"{selectedContact.DisplayName} hasDataChannel ? {hasDataChannel}");
        }

        pingDataChannelButton.interactable = hasDataChannel;
        SendBulkDataChannelButton.interactable = (hasDataChannel && !IsSendingBulk);
        
        if (hasDataChannel)
        {
            requestDataChannelButtonLabel.text = "Close Data Channel";
        }
        else
        {
            requestDataChannelButtonLabel.text = "Request Data Channel";
        }
    }

    private void Rainbow_Ready(bool isReadyAndConnected)
    {
        GetComponent<CanvasGroup>().alpha = 1;
        rainbow.RainbowApplication.GetContacts().ContactPresenceChanged += DataChannelController_ContactPresenceChanged;
        rainbow.RainbowApplication.GetContacts().PeerAdded += DataChannelController_PeerAdded;
        rainbow.RainbowApplication.GetContacts().PeerInfoChanged += DataChannelController_PeerInfoChanged;
        rainbow.RainbowApplication.GetContacts().RosterPeerAdded += DataChannelController_RosterPeerAdded;
        rainbow.RainbowApplication.GetContacts().RosterPeerRemoved += DataChannelController_RosterPeerRemoved; ;

        DataChannelService = new(rainbow.RainbowApplication);
        DataChannelService.ProposalReceived += DataChannelService_ProposalReceived;
        UpdateListOfContacts();
        ContactsDropdown_onValueChanged(contactsDropdown);
    }

    private void DataChannelController_RosterPeerRemoved(object sender, Rainbow.Events.PeerEventArgs e)
    {
        Debug.Log("contact activity");
        UnityExecutor.Execute(() =>
        {
            UpdateListOfContacts();
        });
    }

    private void DataChannelController_RosterPeerAdded(object sender, Rainbow.Events.PeerEventArgs e)
    {
        Debug.Log("contact activity");
        UnityExecutor.Execute(() =>
        {
            UpdateListOfContacts();
        });
    }

    private void DataChannelController_PeerInfoChanged(object sender, Rainbow.Events.PeerEventArgs e)
    {
        Debug.Log("contact activity");
        UnityExecutor.Execute(() =>
        {
            UpdateListOfContacts();
        });
    }

    private void DataChannelController_PeerAdded(object sender, Rainbow.Events.PeerEventArgs e)
    {
        Debug.Log("contact activity");
        UnityExecutor.Execute(() =>
        {
            UpdateListOfContacts();
        });
    }

    private void DataChannelController_ContactPresenceChanged(object sender, Rainbow.Events.PresenceEventArgs e)
    {
        if( e.Presence.Resource.StartsWith("sdk_net_"))
        //if (contactByIndex != null && contactByIndex.Count > 0)
        //{
        //    // Contact previousContact = contactByIndex[contactsDropdown.value];
        //    // if( previousContact.Id == e.. )
        //}
        UnityExecutor.Execute(() =>
        {
            UpdateListOfContacts();
        });
    }

    private void DataChannelService_ProposalReceived(DataChannelProposal offer)
    {
        offer.Accept(dc =>
        {
            if (dc.Success)
            {
                nbExpectedBytes = 0;
                RefreshUI();
                Output.text = ($"Data channel with {dc.Contact.DisplayName} established.");
                HandleOnMessage(dc.Contact, dc.DataChannel);
                HandleOnClose(dc.Contact, dc.DataChannel);
            } else
            {
                Output.text = ($"Data channel with {dc.Contact.DisplayName} failed with error: {dc.Error}.");
            }
        });
    }

    void RequestDataChannelClicked()
    {
        var selectedContact = contactByIndex[contactsDropdown.value];
        DataChannel dc = null;
        if (DataChannelService != null)
        {
            dc = DataChannelService.GetDataChannel(selectedContact);

            if (dc == null)
            {
                RequestDataChannel();
                return;
            }
        }
        if( dc != null)
        {
            Output.text = $"Closed the data channel wih {selectedContact.DisplayName}";
            dc.Close();
            RefreshUI();
        }

    }

    void RefreshUI()
    {
        Debug.Log("Refresh UI");
        UnityExecutor.Execute(() => refreshUI());
    }
    void RequestDataChannel()
    {
        Contact contact = contactByIndex[contactsDropdown.value];
        Output.text = ($"will request a datachannel to {contact.DisplayName}");
        DataChannelService.RequestDataChannel(contact, "myDataChannel", new DataChannelRequestOptions(),
            x =>
            {
                nbExpectedBytes = 0;
                if (!x.Success)
                {
                    Output.text = ($"Datachannel with {contact.DisplayName} failed : {x.Error}");
                    RefreshUI();
                    return;
                }
                else
                {
                    Output.text = ($"Datachannel with {contact.DisplayName} established");
                }

                HandleOnClose(contact, x.DataChannel);  
                HandleOnMessage(contact, x.DataChannel);
                 
                RefreshUI();

            });
    }

    private void HandleOnClose(Contact contact, DataChannel channel)
    {
        channel.OnClose = () =>
         {
             Output.text = ($"DataChannel with {contact.DisplayName} is closed");
             nbExpectedBytes = 0;
             RefreshUI();
         };
    }
    private void HandleOnMessage(Contact contact, DataChannel channel)
    {
        channel.OnMessage = msg =>
        {
            var msgStr = System.Text.Encoding.UTF8.GetString(msg);
            Output.text = ($"DataChannel of {contact.DisplayName} : received message ");
            if (msgStr == "ping")
            {
                try
                {
                    channel.Send("pong");
                    Output.text = ("Got pinged, sent pong");
                }
                catch (Exception e)
                {
                    Debug.LogError($"failed to reply to ping: {e.Message} {e.StackTrace}");
                }
            }
            else if (msgStr == "pong")
            {
                Output.text = ("Got ponged");
            }
            else if (msgStr.StartsWith("bulk"))
            {
                nbExpectedBytes = int.Parse(msgStr.Substring(4));
                if (nbExpectedBytes != 0)
                    Output.text = ($"Expecting " + nbExpectedBytes + " bytes");
            }
            else
            {
                if (nbExpectedBytes > 0)
                {
                    nbExpectedBytes -= msg.Length;
                    if(nbExpectedBytes > 0 )
                        Output.text = ("received a buffer: " + msg.Length + " bytes - expecting " + nbExpectedBytes + " more");
                    else
                        Output.text = ($"Received all");
                } 
            }
        };
    }
    private void OnDestroy()
    {
        DataChannelService?.Stop();
    }
}
