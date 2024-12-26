using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using System.Threading;
using Rainbow.Model;
using System;
using Rainbow.WebRTC.Unity;
using System.Collections;
using Rainbow.WebRTC.Abstractions;
using System.Linq;

namespace SimpleDataChannelRainbow
{
    internal enum NegotiationState
    {
        PROPOSALRECEIVED,
        PROPOSALSENT,
        OFFER_RECEIVED,
        OFFER_SENT,
    };
    public class ConnectionState  // TODO: internal
    {
        internal NegotiationState State;
        internal Contact Contact;
        internal String Resource;
        internal Action<DataChannelRequestResult> Callback;
        internal Action<DataChannelAcceptResult> AcceptCallback;
        internal DataChannelRequestOptions RequestOptions;        
        internal bool NegotiationFinished = false;
        public RTCPeerConnection Pc;
        public Stack<string> PendingCandidates;
        public DataChannel Channel;

        Timer timer;
        internal delegate void ConnectionTimeoutDelegate(string resource);
        internal event ConnectionTimeoutDelegate ConnectionTimeout;

        internal ConnectionState(string resource, int timeOutMs)
        {
            Resource = resource;
            PendingCandidates = new Stack<string>();
            timer = new Timer(Timeout, null, timeOutMs, -1);
        }
        private void Timeout(object o)
        {
            timer.Dispose();
            ConnectionTimeout?.Invoke(Resource);
        }
        internal void Reschedule(int timeoutMs)
        {
            timer.Change(timeoutMs, - 1);
        }
        internal void NegociationFinished()
        {
            NegotiationFinished = true;
            timer.Change(-1, -1);
        }
        internal void Dispose()
        {
            if( timer != null)
            { 
                timer.Dispose();
                timer = null;
            }
            if (Channel != null)
            {
                var channel = Channel;
                Channel = null;
                channel.Close();
                channel.Dispose();
            }
            if (Pc != null)
            {
                var pc = Pc;
                Pc = null;
                pc.Close();
                pc.Dispose();
            }

        }
    }

    public class DataChannelRequestOptions
    {
        public int TimeOutMs = 30000;
        public String Name;
        public RTCDataChannelInit DataChannelInit = new() { negotiated = false, ordered = true, maxPacketLifeTime = 5000 };
    }

    public class DataChannelRequestResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }   // TODO: define a better type
        public DataChannel DataChannel { get; set; }
    }

    public class DataChannelAcceptResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }   // TODO: define a better type
        public DataChannel DataChannel { get; set; }
        public Contact Contact { get; set; }
    }


    /// <summary>
    /// DataChannelProposal is passed to delegates attached to the DataChannelService.ProposalReceived
    /// It contains methods to Accept and Reject the proposal.
    /// </summary>
    public class DataChannelProposal
    {
        SimpleDataChannelService dataChannelService;
        internal Contact contact;
        internal String resource;
        internal Timer timer;
        const int NBMSBeforeWeRejectOffer = 15 * 1000;
        internal DataChannelProposal(SimpleDataChannelService service, string resource, Contact contact)
        {

            this.dataChannelService = service;
            this.contact = contact;
            this.resource = resource;
            timer = new Timer(TimeOut, null, NBMSBeforeWeRejectOffer, 0);
        }
        public void Accept(Action<DataChannelAcceptResult> cb)
        {
            timer.Dispose();
            dataChannelService.AcceptOffer(contact, resource,cb);
        }
        public void Reject()
        {
            timer.Dispose();
            dataChannelService.RejectOffer(contact, resource);
        }

        private void TimeOut(object o)
        {
            Debug.Log($"Reject The offer from {contact.DisplayName} because of timeout");
            dataChannelService.RejectOffer(contact, resource);
            timer.Dispose();
        }
    }

    /// <summary>
    /// SimpleDataChannelService is the main class to establish data channels.
    /// </summary>
    public class SimpleDataChannelService
    {
        private Dictionary<String, ConnectionState> DataChannelConnections;
        HashSet<String> useablePresences = new HashSet<string>() { PresenceLevel.Online, PresenceLevel.Away, PresenceLevel.Busy };
        private int TimeOutMilliSeconds;
        private Rainbow.Application RainbowApplication;
        private Rainbow.Contacts ContactService;
        private Rainbow.InstantMessaging IMService;
        public delegate void DataChannelProposalDelegate(DataChannelProposal offer);
        public event DataChannelProposalDelegate ProposalReceived;

        public class DataChannelAction
        {
            public const String REQUEST_DC = "request_datachannel";
            public const String ACCEPT_DC = "accept_datachannel";
            public const String REJECT_DC = "reject_datechannel";
            public const String OFFER_DC = "offer_datachannel";
            public const String ANSWER_DC = "answer_datachannel";
            public const String CANDIDATE_DC = "candidate";
            public const String CANCEL_DC = "cancel_datachannel";
        }

        public void Stop()
        {
            var connections = DataChannelConnections.Values.ToArray();
            foreach(var cnx in connections)
            {
                CancelNegotiation(cnx,"Service stopped");
            }
        }
        public SimpleDataChannelService(Rainbow.Application rainbowApplication)
        {
            this.RainbowApplication = rainbowApplication;
            DataChannelConnections = new();
            TimeOutMilliSeconds = 5000;
            this.ContactService = RainbowApplication.GetContacts();
            this.IMService = RainbowApplication.GetInstantMessaging();
            IMService.AckMessageReceived += IMService_AckMessageReceived;
        }
        public bool HasDataChannel(Contact contact)
        {
            var dc = GetDataChannel(contact);
            return dc != null;
        }
        public DataChannel GetDataChannel(Contact contact)
        {
            List<String> dcs = new();
            foreach( var cnx in DataChannelConnections.Values)
            {
                dcs.Add( cnx.Contact.DisplayName );
            }
            Debug.Log($"DCS are: {String.Join(",",dcs)}");
            foreach (var cnx in DataChannelConnections.Values)
            {
                Debug.Log($"comparing {contact.DisplayName} and {cnx.Contact.DisplayName}");
                
                if (cnx.Contact.Id == contact.Id)
                {
                    return cnx.Channel;
                }
            }
            return null;
        }

         
        internal void AcceptOffer(Contact contact, string resource, Action<DataChannelAcceptResult> cb)
        {
            ConnectionState newCnx = new ConnectionState(resource, TimeOutMilliSeconds) { State = NegotiationState.PROPOSALRECEIVED, Contact = contact };
            AddConnectionState(resource, newCnx);
            var pc = CreatePeerConnection(newCnx);
            newCnx.Pc = pc;
            newCnx.Contact = contact;
            newCnx.AcceptCallback = cb;
            SendSessionMessage(contact, resource, DataChannelAction.ACCEPT_DC, DataChannelAction.ACCEPT_DC, todo => { });
        }

        internal void RejectOffer(Contact contact, string resource)
        {
            SendSessionMessage(contact, resource, DataChannelAction.REJECT_DC, DataChannelAction.REJECT_DC, todo => { });
        }

        internal void AddConnectionState(String resource, ConnectionState cnx)
        {
            DataChannelConnections[resource] = cnx;
            cnx.ConnectionTimeout += Cnx_ConnectionTimeout;
        }

        private void Cnx_ConnectionTimeout(string resource)
        {
            ConnectionState cnx = DataChannelConnections[resource];
            Debug.LogError($"Connection {cnx.Contact.DisplayName} rsrc {cnx.Resource} timed out");

            CancelNegotiation(cnx, "ERROR_TIMEOUT");
        }
        private void CancelNegotiation(string jid, string resource, string reason)
        {
            SendSessionMessage(jid, resource, DataChannelAction.CANCEL_DC, reason, todo => { });
        }

        private void CancelNegotiation(ConnectionState cnx, string reason)
        {
            // invoke if we are the requester, invoke the callback 
            
            if (cnx.Callback != null && !cnx.NegotiationFinished)
            {
                DataChannelRequestResult result = new DataChannelRequestResult();
                result.Success = false;
                result.Error = reason;
                cnx.Callback(result);
            }
            cnx.ConnectionTimeout -= Cnx_ConnectionTimeout;
            if(!cnx.NegotiationFinished)
            {
                SendSessionMessage(cnx.Contact, cnx.Resource, DataChannelAction.CANCEL_DC, reason, todo => { });
            }
            cnx.Dispose();
            DataChannelConnections.Remove(cnx.Resource);
        }
        private void SendCandidate(ConnectionState cnx, RTCIceCandidate candidate)
        {
            Debug.Log("need to send candidate");
            string candidateStr = $"{candidate.SdpMLineIndex},{candidate.SdpMid},{candidate.Candidate}";
            if ( cnx.Pc.SignalingState == RTCSignalingState.Stable)
            {
                SendSessionMessage(cnx.Contact, cnx.Resource, DataChannelAction.CANDIDATE_DC, candidateStr, todo => { });
            }  
            else cnx.PendingCandidates.Push(candidateStr);
        }

        private void OnPeerConnectionStateChanged(ConnectionState cnx, RTCPeerConnectionState state) {
            if(state == RTCPeerConnectionState.Connected) {
                cnx.NegociationFinished();
            }
            if( state == RTCPeerConnectionState.Disconnected || state == RTCPeerConnectionState.Closed || state == RTCPeerConnectionState.Failed) {
                CancelNegotiation(cnx, $"CONNECTIONSTATECHANGED ({state})");
            }
            Debug.Log($"PeerConnectionStateChanged: {state}");
        }
        private void OnDataChannel(ConnectionState cnx, RTCDataChannel dataChannel)
        {
            Debug.Log("DataChannel ON DATA CHANNEL ");
            cnx.Channel = DataChannel.Wrap(dataChannel,this,cnx);
            cnx.Channel.connected = true;
            cnx.NegociationFinished();
            if( cnx.AcceptCallback != null)
            {
                DataChannelAcceptResult result = new DataChannelAcceptResult {
                    Contact = cnx.Contact,
                    DataChannel = cnx.Channel,
                    Success = true
                };
                cnx.AcceptCallback(result);
            }
            Debug.Log("DataChannel established by remote");
        }

        private RTCPeerConnection CreatePeerConnection(ConnectionState cnx)
        {
            RTCConfiguration config = default;

            // TODO: this is temporary and must be changed
            config.iceServers = UnityWebRTCFactory.GetIceServers();
            if(config.iceServers == null)
            {
                config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };
            }
 
            RTCPeerConnection pc = new RTCPeerConnection(ref config);

            pc.OnIceCandidate = candidate => SendCandidate(cnx, candidate);
            pc.OnConnectionStateChange = state => OnPeerConnectionStateChanged(cnx,state);
            pc.OnDataChannel = dc => OnDataChannel(cnx,dc);
            return pc;
        }

        private void SendWebRTCOffer(string resource)
        {
            ConnectionState cnx = DataChannelConnections[resource];

            UnityExecutor.Execute(() =>
            {
                var pc = CreatePeerConnection(cnx);
                cnx.Pc = pc;
                cnx.Channel = DataChannel.Wrap(pc.CreateDataChannel(cnx.RequestOptions.Name, cnx.RequestOptions.DataChannelInit), this,cnx); // { negotiated = false, ordered = true });
                PrepareDataChannel(cnx, cnx.Channel);

                UnityExecutor.Execute(CreateAndSendOfferCoroutine(cnx));

            });

        }

        private void SendWebRTCAnswer(string jid, string resource, string offer)
        {
            ConnectionState cnx = DataChannelConnections[resource];

            if (cnx == null)
            {
                CancelNegotiation(jid, resource, "ERROR_UNEXPECTED_OFFER");
                return;
            }
            UnityExecutor.Execute(() =>
            {
                UnityExecutor.Execute(AcceptOfferAndReplyCoroutine(cnx, offer));

            });
        }

        private void AcceptWebRTCAnswer(string jid, string resource, string answer)
        {
            ConnectionState cnx = DataChannelConnections[resource];

            if (cnx == null)
            {
                CancelNegotiation(jid, resource, "ERROR_UNEXPECTED_ANSWER");
                return;
            }
            UnityExecutor.Execute(() =>
            {
                UnityExecutor.Execute(AcceptAnswerCoroutine(cnx, answer));
            });
        }

        private void AddCandidate(string jid, string resource, string content)
        {
            ConnectionState cnx = DataChannelConnections[resource];
            if(cnx == null)
            {
                Debug.LogError("Received unexpected candidate");
                return;
            }

            if( cnx.Pc == null )
            {
                Debug.LogError("Received candidate but have no pc..");
                CancelNegotiation(cnx, "UNEXPECTED_CANDIDATE");
                return;
            }
            UnityExecutor.Execute(()=>AddCandidate(cnx, content));
        }

        private void AddCandidate( ConnectionState cnx, string candidateStr )
        {
            // format is LineIndex,Mid,Candidate            
            var splitted = candidateStr.Split(",");
            RTCIceCandidate candidate = new RTCIceCandidate(new RTCIceCandidateInit
            {
                sdpMLineIndex = int.Parse(splitted[0]),
                sdpMid = splitted[1],
                candidate = splitted[2]
            });

            bool res = cnx.Pc.AddIceCandidate(candidate);
        }
        private IEnumerator AcceptAnswerCoroutine(ConnectionState cnx, string answer)
        {
            yield return 0;
            var offerSessionDescription = new RTCSessionDescription() { type = RTCSdpType.Answer, sdp = answer };
            var op = cnx.Pc.SetRemoteDescription(ref offerSessionDescription);

            yield return op;
            if (op.IsError)
            {
                Debug.LogError($"failed to set remote description: {op.Error}");
                CancelNegotiation(cnx, "ERROR_SETREMOTEDESCRIPTION");
                yield break;
            }
            cnx.Reschedule(30 * 1000);
            if (cnx.Pc.SignalingState == RTCSignalingState.Stable)
            {
                FlushPendingCandidates(cnx);
            }
        }
        private IEnumerator AcceptOfferAndReplyCoroutine(ConnectionState cnx, string offer)
        {
            yield return 0;
            var offerSessionDescription = new RTCSessionDescription() { type = RTCSdpType.Offer, sdp = offer };
            var op = cnx.Pc.SetRemoteDescription(ref offerSessionDescription);

            yield return op;
            if (op.IsError)
            {
                Debug.LogError($"failed to set remote description: {op.Error}");
                CancelNegotiation(cnx, "ERROR_SETREMOTEDESCRIPTION");
                yield break;
            }

            var opAnswer = cnx.Pc.CreateAnswer();
            yield return opAnswer;
            if (opAnswer.IsError)
            {
                Debug.LogError($"failed to create answer: {op.Error}");
                CancelNegotiation(cnx, "ERROR_CREATEANSWER");
                yield break;
            }

            var opAnswerSessionDescription = opAnswer.Desc;
            var opSLD = cnx.Pc.SetLocalDescription(ref opAnswerSessionDescription);
            yield return opSLD;
            if (opSLD.IsError)
            {
                Debug.LogError($"failed to set local description on anser: {op.Error}");
                CancelNegotiation(cnx, "ERROR_SETLOCALDESCRIPTION_ANSWER");
                yield break;
            }
            cnx.Reschedule(30 * 1000);
            SendSessionMessage(cnx.Contact, cnx.Resource, DataChannelAction.ANSWER_DC, opAnswer.Desc.sdp, ackMessage => {
                if (ackMessage.Result.Success)
                {
                    Debug.Log("Sent Answer");
                    if (cnx.Pc.SignalingState == RTCSignalingState.Stable)
                    {
                        FlushPendingCandidates(cnx);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to send answer: {ackMessage.Result}");
                    CancelNegotiation(cnx, "ERROR_SENDANSWER");
                }
            });
           
        }

        private void FlushPendingCandidates(ConnectionState cnx)
        {
            while(cnx.PendingCandidates.Count > 0 )
            {
                string candidateStr = cnx.PendingCandidates.Pop();
                SendSessionMessage(cnx.Contact, cnx.Resource, DataChannelAction.CANDIDATE_DC, candidateStr, todo => { });
            }
        }
        private IEnumerator CreateAndSendOfferCoroutine(ConnectionState cnx)
        {
            yield return 0;

            var op = cnx.Pc.CreateOffer();
            yield return op;

            if (op.IsError)
            {
                Debug.LogError($"failed to create offer: {op.Error}");
                CancelNegotiation(cnx, "ERROR_CREATEPEERCONNECTION");
                yield break;
            }
            var offer = op.Desc;
            var opSLD = cnx.Pc.SetLocalDescription(ref offer);
            yield return opSLD;
            if (opSLD.IsError)
            {
                Debug.LogError($"failed to set local description: {opSLD.Error}");
                CancelNegotiation(cnx, "ERROR_SETLOCALDESCRIPTION");
                yield break;
            }

            Debug.Log($"Got offer, will send it to {cnx.Contact.DisplayName} state {cnx.Pc.SignalingState} connectionstate {cnx.Pc.ConnectionState}");

            SendSessionMessage(cnx.Contact, cnx.Resource, DataChannelAction.OFFER_DC, offer.sdp, ackMessage =>
            {
                if (ackMessage.Result.Success)
                {
                    Debug.Log("Sent Offer");
                }
                else
                {
                    Debug.LogError($"Failed to send offer: {ackMessage.Result}");
                    CancelNegotiation(cnx, "ERROR_SENDOFFER");

                }
            });
        }

        private void PrepareDataChannel(ConnectionState cnx, DataChannel channel)
        {
            Contact contact = cnx.Contact;
            channel.OnOpen = () =>
            {
                Debug.Log($"DataChannel of {contact.DisplayName} is open !!");
                // channel.Send("Hello my maan");
                if (cnx.Callback != null)
                {
                    DataChannelRequestResult result = new DataChannelRequestResult();
                    result.Success = true;
                    result.DataChannel = channel;
                    cnx.Callback(result);
                }


            };
            channel.OnMessage = msg =>
            {
                var msgStr = System.Text.Encoding.UTF8.GetString(msg);
                Debug.Log($"DataChannel of {contact.DisplayName} : received message of {msg.Length} bytes");
            };

            channel.OnClose = () =>
            {
                Debug.Log($"DataChannel of user {contact.DisplayName} is closed");
                // TODO make some cleanup
            };

        }

        private void IMService_AckMessageReceived(object sender, Rainbow.Events.AckMessageEventArgs e)
        {
            if (e.AckMessage.Type == MessageType.Get || e.AckMessage.Type == MessageType.Set)
            {
                switch (e.AckMessage.Action)
                {
                    case DataChannelAction.REQUEST_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        Contact c = ContactService.GetContactFromContactJid(e.AckMessage.FromJid);
                        Debug.Log("received Request");
                        if (ProposalReceived == null)
                        {
                            // If the app didn't position a handler, reject the offer
                            RejectOffer(c, e.AckMessage.FromResource);
                        }
                        else
                        {
                            DataChannelProposal proposal = new(this, e.AckMessage.FromResource, c);
                            ProposalReceived?.Invoke(proposal);
                        }
                        break;

                    case DataChannelAction.ACCEPT_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        Debug.Log("received Accept");
                        SendWebRTCOffer(e.AckMessage.FromResource);
                        break;

                    case DataChannelAction.OFFER_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        Debug.Log("received Offer");
                        SendWebRTCAnswer(e.AckMessage.FromJid, e.AckMessage.FromResource, e.AckMessage.Content);
                        break;

                    case DataChannelAction.ANSWER_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        Debug.Log("received Answer");
                        AcceptWebRTCAnswer(e.AckMessage.FromJid, e.AckMessage.FromResource, e.AckMessage.Content);
                        break;

                    case DataChannelAction.REJECT_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        CancelNegotiation(DataChannelConnections[e.AckMessage.FromResource], "REJECTED_BY_PEER");
                        break;

                    case DataChannelAction.CANCEL_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        Debug.Log($"received Cancel: {e.AckMessage.Content}");

                        ConnectionState cnx = DataChannelConnections[e.AckMessage.FromResource];
                        Debug.LogError($"Connection {cnx.Contact.DisplayName} rsrc cancelled");
                        // invoke a callback
                        if (cnx.Callback != null)
                        {
                            DataChannelRequestResult result = new DataChannelRequestResult();
                            result.Success = false;
                            result.Error = "ERROR_CANCELLED";
                            cnx.Callback(result);
                        }
                        cnx.ConnectionTimeout -= Cnx_ConnectionTimeout;
                        cnx.Dispose();
                        DataChannelConnections.Remove(e.AckMessage.FromResource);
                        break;

                    case DataChannelAction.CANDIDATE_DC:
                        IMService.AnswerToAckMessage(e.AckMessage, MessageType.Result);
                        Debug.Log("received Candidate");
                        AddCandidate(e.AckMessage.FromJid, e.AckMessage.FromResource, e.AckMessage.Content);
                        break;
                }
            }
            else
            {
                switch (e.AckMessage.Action)
                {
                    case DataChannelAction.REQUEST_DC:
                    case DataChannelAction.ACCEPT_DC:
                    case DataChannelAction.OFFER_DC:
                    case DataChannelAction.ANSWER_DC:
                    case DataChannelAction.REJECT_DC:
                    case DataChannelAction.CANCEL_DC:
                    case DataChannelAction.CANDIDATE_DC:
                        break;
                }
            }
        }

        public void RequestDataChannel(Contact contact, string name, DataChannelRequestOptions options, Action<DataChannelRequestResult> callback)
        {
            String reason;
            String resource = FindUsableResource(contact, out reason);

            Action<String> fail = reason =>
            {
                DataChannelRequestResult result = new() { Success = false, Error = reason };
                callback(result);
            };

            if (String.IsNullOrEmpty(resource))
            {
                fail(reason);
                return;
            }

            if (DataChannelConnections.ContainsKey(resource))
            {
                fail("ALREADY IN PROGRESS");
                return;
            }

            ConnectionState newConnection = new ConnectionState(resource, TimeOutMilliSeconds)
            {
                State = NegotiationState.OFFER_SENT,
                Contact = contact,
                Callback = callback,
                RequestOptions = new() { DataChannelInit = options.DataChannelInit, Name = name }
            };

            AddConnectionState(resource, newConnection);
            SendProposal(contact, resource);
        }

        private void SendProposal(Contact contact, String resource)
        {
            SendSessionMessage(contact, resource, DataChannelAction.REQUEST_DC, "-", result =>
            {
                Debug.Log($"Sent message .. result is type {result.Result} Data is {result.Data}.");
                if (result.Result.Success)
                {
                    Debug.Log($"{result.Data.Type} content {result.Data.Content} id {result.Data.Id} action {result.Data.Action}");
                }
                else
                {
                    Debug.LogError("SendProposal failed");
                }
            });
        }

        private void SendSessionMessage(string jid, String resource, String action, String content, Action<Rainbow.SdkResult<AckMessage>> cb)
        {
            AckMessage message = new AckMessage();
            message.FromJid = ContactService.GetCurrentContactJid();
            message.FromResource = RainbowApplication.GetResource();
            message.ToJid = jid;
            message.ToResource = resource;
            message.Content = content;
            message.Type = MessageType.Set;
            message.Action = action;
            IMService.SendAckMessage(message, cb, TimeOutMilliSeconds);
        }
        private void SendSessionMessage(Contact contact, String resource, String action, String content, Action<Rainbow.SdkResult<AckMessage>> cb)
        {
            SendSessionMessage(contact.Jid_im, resource, action, content, cb);
        }

        public String FindUsableResource(Contact contact, out string reason)
        {
            string resource = string.Empty;

            if (contact == null || contact.Equals(ContactService.GetCurrentContact()))
            {
                reason = "WRONG_CONTACT_ERROR";
                return resource;
            }

            Dictionary<String, Presence> presences = ContactService.GetPresencesFromContact(contact);

            reason = "NOT_FOUND";
            if (presences == null || presences.Count <= 0)
            {
                return resource;
            }


            foreach (var presence in presences)
            {
                if (presence.Key.StartsWith("sdk_net_")) 
                {
                    reason = "NOT_CONNECTED";
                    if (useablePresences.Contains(presence.Value.PresenceLevel))
                    {
                        reason = "SUCCESS";
                        resource = presence.Key;
                        break;
                    }
                }
            }

            return resource;
        }

        internal void CloseDataChannel(ConnectionState cnx )
        {
            // warn the remote end.
            CancelNegotiation(cnx, "CLOSED");
            DataChannelConnections.Remove(cnx.Resource);
        }

    }
}