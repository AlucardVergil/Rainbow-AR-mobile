using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC.Abstractions;
using Rainbow.WebRTC.Unity;
using SimpleDataChannelRainbow;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Singleton object that handles state and event redirection of a rainbow connection. This is a basic abstraction layer on top of some of Rainbow's functionality to provide easier access to certain fields
    /// </summary>
    public class ConnectionModel : MonoBehaviour
    {
        /// <summary>
        /// Get the global connection model instance
        /// </summary>
        public static ConnectionModel Instance { get; private set; }

        #region events
        // TODO events might need more parameters

        /// <summary>
        /// Handler for login events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        public delegate void LoginHandler(ConnectionModel model);
        /// <summary>
        /// Event called on a successful login
        /// </summary>
        public static event LoginHandler OnLogin;

        /// <summary>
        /// Handler for login failed events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        public delegate void LoginFailedHandler(ConnectionModel model);
        /// <summary>
        /// Event called on a failed login attempt
        /// </summary>
        public static event LoginFailedHandler OnLoginFailed;

        /// <summary>
        /// Handler for logout events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        public delegate void LogoutHandler(ConnectionModel model);
        /// <summary>
        /// Event called whenever a logout occurred
        /// </summary>
        public static event LogoutHandler OnLogout;

        /// <summary>
        /// Handler for call status update events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="args">Event object describing the call and status update</param>
        public delegate void CallStatusUpdateHandler(ConnectionModel model, CallEventArgs args);
        /// <summary>
        /// Event called when the status of a call changed
        /// </summary>
        public static event CallStatusUpdateHandler OnCallStatusUpdate;

        /// <summary>
        /// Handler for conference participants update events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="args">Event object describing the current call participants</param>
        public delegate void ConferenceParticipantsUpdateHandler(ConnectionModel model, ConferenceParticipantsEventArgs args);

        /// <summary>
        /// Event called when the conference participants update
        /// </summary>
        public static event ConferenceParticipantsUpdateHandler OnConferenceParticipantsUpdate;

        /// <summary>
        /// Handler for conference talker update events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="args">Event object describing the current talkers</param>
        public delegate void ConferenceTalkersUpdateHandler(ConnectionModel model, ConferenceTalkersEventArgs args);

        /// <summary>
        /// Event called when the conference talkers update 
        /// </summary>
        public static event ConferenceTalkersUpdateHandler OnConferenceTalkersUpdate;

        /// <summary>
        /// Handler for conference update events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="args">Event object describing the changed conference</param>
        public delegate void ConferenceUpdateHandler(ConnectionModel model, ConferenceEventArgs args);

        /// <summary>
        /// Event called when a conference updates
        /// </summary>
        public static event ConferenceUpdateHandler OnConferenceUpdate;

        /// <summary>
        /// Handler for call hang up events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        public delegate void CallHangUpHandler(ConnectionModel model);
        /// <summary>
        /// Event called when a call is actively hung up.
        /// It will still be disconnected afterwards
        /// </summary>
        public static event CallHangUpHandler OnCallHangUp;

        /// <summary>
        /// Handler for call connected events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="call">The call that connected</param>
        public delegate void CallConnectedHandler(ConnectionModel model, Call call);
        /// <summary>
        /// Event called when a call is connected
        /// </summary>
        public static event CallConnectedHandler OnCallConnected;

        /// <summary>
        /// Handler for call disconnected events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="call">The call that disconnected</param>
        public delegate void CallDisconnectedHandler(ConnectionModel model, Call call);
        /// <summary>
        /// Event called when a call is disconnected for any reason
        /// </summary>
        public static event CallDisconnectedHandler OnCallDisconnected;

        /// <summary>
        /// Handler for data channel opened events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="contact">The contact for which a data channel was opened</param>
        public delegate void DataChannelOpenedHandler(ConnectionModel model, Contact contact);
        /// <summary>
        /// Event called when a data channel for a contact is open and can be written to.
        /// This is a data channel event and thus not specifically called on the unity thread!
        /// </summary>
        public static event DataChannelOpenedHandler OnDataChannelOpened;

        /// <summary>
        /// Handler for data channel closed events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="contact">The contact for which a data channel was closed</param>
        public delegate void DataChannelClosedHandler(ConnectionModel model, Contact contact);
        /// <summary>
        /// Event called when a data channel for a contact has been closed and should not be written to anymore
        /// This is a data channel event and thus not specifically called on the unity thread!
        /// </summary>
        public static event DataChannelClosedHandler OnDataChannelClosed;

        /// <summary>
        /// Handler for data channel error events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="contact">The contact for which the data channel threw an error</param>
        /// <param name="error">The error that occurred</param>
        public delegate void DataChannelErrorHandler(ConnectionModel model, Contact contact, Unity.WebRTC.RTCError error);
        /// <summary>
        /// Event called when a data channel for a contact has thrown an error and should not be written to anymore
        /// This is a data channel event and thus not specifically called on the unity thread!
        /// </summary>
        public static event DataChannelErrorHandler OnDataChannelError;

        /// <summary>
        /// Handler for messages sent on a data channel for a given contact
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="contact">The contact which send the data</param>
        /// <param name="data">The data</param>
        public delegate void DataChannelMessageHandler(ConnectionModel model, Contact contact, byte[] data);
        /// <summary>
        /// Event called when a new message for the given contact is received
        /// This is a data channel event and thus not specifically called on the unity thread!
        /// </summary>
        public static event DataChannelMessageHandler OnDataChannelMessage;

        /// <summary>
        /// Handler for remote track events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="callId">The id of the call of the remote track</param>
        /// <param name="descriptor">A descriptor of the remote track</param>
        public delegate void RemoteTrackHandler(ConnectionModel model, string callId, Rainbow.WebRTC.MediaStreamTrackDescriptor descriptor);

        /// <summary>
        /// Event called when a remote track is initiated
        /// </summary>
        public static event RemoteTrackHandler OnRemoteTrack;

        /// <summary>
        /// Handler for remote media publication updated events
        /// </summary>
        /// <param name="model">The connection model that spawned the event</param>
        /// <param name="args">Event information about the remote media publication</param>
        public delegate void MediaPublicationUpdatedHandler(ConnectionModel model, Rainbow.WebRTC.MediaPublicationEventArgs args);

        /// <summary>
        /// Event called when a remote media updates
        /// </summary>
        public static event MediaPublicationUpdatedHandler OnRemoteMediaPublicationUpdated;

        /// <summary>
        /// Handler for connection state changed events
        /// </summary>
        /// <param name="state">The current state</param>
        /// <param name="previousState">The previous state</param>
        public delegate void ConnectionStateChangedHandler(ConnectionState state, ConnectionState previousState);

        /// <summary>
        /// Event called when the connection state of the connection model changes
        /// </summary>
        public static event ConnectionStateChangedHandler OnConnectionStateChanged;

        #endregion // events

        #region public fields

        [Tooltip("The underlying rainbow controller. Will be searched for, if it doesn't exist")]
        public RainbowInterface RainbowInterface;

        /// <summary>
        /// Describes the state in which the connection currently is
        /// </summary>
        public enum ConnectionState
        {
            Initial, RequestLogin, LoggedIn, Error
        }

        // backing property for State
        private ConnectionState _state = ConnectionState.Initial;
        /// <summary>
        /// The state that this model is currently in.
        /// </summary>
        public ConnectionState State
        {
            get => _state; private set
            {
                if (value == _state)
                {
                    return;
                }

                ConnectionState old = _state;
                _state = value;

                OnConnectionStateChanged?.Invoke(_state, old);
            }
        }

        /// <summary>
        /// Convenience property to check, whether the model is currently logged in
        /// </summary>
        public bool LoggedIn { get { return State == ConnectionState.LoggedIn; } }

        /// <summary>
        /// Convenience property to get the conferences object from the rainbow application
        /// </summary>
        public Conferences Conferences
        {
            get
            {
                if (RainbowInterface == null || RainbowInterface.RainbowApplication == null)
                {
                    return null;
                }

                return RainbowInterface.RainbowApplication.GetConferences();
            }
        }

        /// <summary>
        /// Convenience property to get the contacts object from the rainbow application
        /// </summary>
        public Contacts Contacts
        {
            get
            {
                if (RainbowInterface == null || RainbowInterface.RainbowApplication == null)
                {
                    return null;
                }

                return RainbowInterface.RainbowApplication.GetContacts();
            }
        }

        /// <summary>
        /// Convenience property to get the bubbles object from the rainbow application
        /// </summary>
        public Bubbles Bubbles
        {
            get
            {
                if (RainbowInterface == null || RainbowInterface.RainbowApplication == null)
                {
                    return null;
                }

                return RainbowInterface.RainbowApplication.GetBubbles();
            }
        }

        /// <summary>
        /// Get the Contact of the currently logged in user, if it exists. Otherwise null
        /// </summary>
        public Contact CurrentUser
        {
            get
            {
                Contacts contacts = Contacts;
                if (contacts == null)
                {
                    return null;
                }

                return contacts.GetCurrentContact();
            }
        }


        // Vagelis start
        public Conversations Conversations
        {
            get
            {
                if (RainbowInterface == null || RainbowInterface.RainbowApplication == null)
                {
                    return null;
                }

                return RainbowInterface.RainbowApplication.GetConversations();
            }
        }



        public InstantMessaging InstantMessaging
        {
            get
            {
                if (RainbowInterface == null || RainbowInterface.RainbowApplication == null)
                {
                    return null;
                }

                return RainbowInterface.RainbowApplication.GetInstantMessaging();
            }
        }


        public Invitations Invitations
        {
            get
            {
                if (RainbowInterface == null || RainbowInterface.RainbowApplication == null)
                {
                    return null;
                }

                return RainbowInterface.RainbowApplication.GetInvitations();
            }
        }

        // Vagelis end

        #endregion // public fields

        #region public methods

        /// <summary>
        /// Get the data channel for the given contact. If no such channel exists, returns null
        /// </summary>
        /// <param name="contact">The contact for which to request the data channel</param>
        /// <returns>The data channel, or null, if it does not exist</returns>
        public DataChannel GetDataChannel(Contact contact)
        {
            if (contact == null)
            {
                return null;
            }

            var channel = dataChannelService?.GetDataChannel(contact);

            return channel;
        }

        /// <summary>
        /// Get the data channel for the given contact. If no such channel exists, returns null
        /// </summary>
        /// <param name="contactId">The id of the contact for which to request the data channel</param>
        /// <returns>The data channel, or null, if it does not exist</returns>
        public DataChannel GetDataChannel(string contactId)
        {
            Contact contact = Contacts.GetContactFromContactId(contactId);
            return GetDataChannel(contact);
        }

        /// <summary>
        /// Get the data channel for the currently existing contact. If no such channel exists, returns null
        /// </summary>
        /// <returns>The data channel, or null, if it does not exist</returns>
        public DataChannel GetDataChannel()
        {
            Contact c = GetConnectedContact();
            return GetDataChannel(c);
        }

        /// <summary>
        /// Request to log in to the given rainbow service.
        /// If the login attempt is successful, the OnLogin event is fired. If the attempt fails, OnLoginFailed is fired
        /// </summary>
        /// <param name="login">The username</param>
        /// <param name="password">The password</param>
        /// <param name="hostname">The hostname</param>
        public void RequestLogin(string login, string password, string hostname)
        {
            State = ConnectionState.RequestLogin;
            // TODO Use rainbow.RainbowApplication.GetAutoReconnection() to add callbacks for cancelled? Is this the same as cancelling an attempt?

            // TODO maybe make this configurable via a file
            string appId = "";
            string secret = "";
            if (hostname == "openrainbow.com")
            {
                appId = "fa6d4190e4d311ed8028dd644fb679f3";
                secret = "rvVjVVenwl7o7pHjnaI2OBWgmeOTndRdD4GGzL1Fmfhpp5O0nVIwutvmQ3biin9B";
            }
            else if (hostname == "sandbox.openrainbow.com")
            {
                appId = "618757006f6e11efa6661b0bb9c90370";
                secret = "FsjYTiDF0jMsnkuwd0EuoXF91UsC1uWqSkcsWUxEPvwuUUZdCKr9TNGjo6HXjDUu";
            }

            RequestLogin(appId, secret, login, password, hostname);
        }

        /// <summary>
        /// Request to log in to the given rainbow service.
        /// If the login attempt is successful, the OnLogin event is fired. If the attempt fails, OnLoginFailed is fired
        /// </summary>
        /// <param name="appId">The application id created on the rainbow platform</param>
        /// <param name="secret">The application secret created on the rainbow platform</param>
        /// <param name="login">The username</param>
        /// <param name="password">The password</param>
        /// <param name="hostname">The hostname</param>
        public void RequestLogin(string appId, string secret, string login, string password, string hostname)
        {
            State = ConnectionState.RequestLogin;
            // TODO Use rainbow.RainbowApplication.GetAutoReconnection() to add callbacks for cancelled? Is this the same as cancelling an attempt?

            if (!RainbowInterface.Connect(login, password, hostname, appId, secret))
            {
                State = ConnectionState.Initial;
                OnLoginFailed?.Invoke(this);
            }
        }

        /// <summary>
        /// Check whether a data channel exists for the given contact
        /// </summary>
        /// <param name="contact">The contact to check for</param>
        /// <returns>True, if there is a data channel, false otherwise</returns>
        public bool HasDataChannel(Contact contact)
        {
            return GetDataChannel(contact) != null;
        }

        /// <summary>
        /// Check whether a data channel exists for the given contact
        /// </summary>
        /// <param name="contactId">The id of the contact to check for</param>
        /// <returns>True, if there is a data channel, false otherwise</returns>
        public bool HasDataChannel(string contactId)
        {
            return GetDataChannel(contactId) != null;
        }

        /// <summary>
        /// Request to be logged out.
        /// When the actual logout occurs, the OnLogout event will be fired
        /// </summary>
        public void RequestLogout()
        {
            RainbowInterface.Disconnect(v =>
            {
                State = ConnectionState.Initial;
                // TODO this should be called during disconnect automatically
                // onLogout?.Invoke();
            });
        }

        /// <summary>
        /// Initiate a peer-to-peer call to another contact specified by its id.
        /// If the call attempt is successful, the OnCallConnected event is fired. If at one point the call is disconnected, OnCallDisconnected is fired.
        /// While the call attempt or call itself is occurring, the OnCallStatusUpdate event is fired, whenever the status changes
        /// </summary>
        /// <param name="contactId">The user id to call</param>
        /// <param name="timeout">Time after which the call will automatically be hung up</param>
        /// <returns>True, if the call to initiate the call attempt was successful, false otherwise</returns>
        public bool P2PCallContact(string contactId, float timeout = float.PositiveInfinity)
        {
            bool result = RainbowInterface.P2PCallContact(contactId);

            // we add a timeout for the call attempt
            if (float.IsFinite(timeout))
            {
                RainbowInterface.RainbowApplication.SetTimeout((int)(timeout * 1000));
            }

            return result;
        }

        /// <summary>
        /// An operation running before a hang up occurs. The returned task should signal, when the operation is finished, as the hang up waits, with a timeout, for these operations to finish.
        /// </summary>
        /// <param name="token">A cancellation token</param>
        /// <returns>The task signifying the operation</returns>
        public delegate Task PreHangUpOperation(CancellationToken token);

        /// <summary>
        /// Registers an operation that is initiated when a hang up is requested.
        /// Unless the hang up timeout has passed, the pre hang up operations are fully executed before the actual hang up occurs. This way, network state cleanup can occur.
        /// </summary>
        /// <param name="operation">The operation</param>
        /// <returns>True, if the operation was not registered before, false otherwise</returns>
        public bool RegisterPreHangUpOperation(PreHangUpOperation operation)
        {
            return preHangupOperations.TryAdd(operation, true);
        }

        /// <summary>
        /// Registers an operation that is initiated when a hang up is requested.
        /// </summary>
        /// <param name="operation">The operation</param>
        /// <returns>True, if the operation was removed, false if it wasn't registered</returns>
        public bool RemovePreHangUpOperation(PreHangUpOperation operation)
        {
            return preHangupOperations.Remove(operation, out var _);
        }

        /// <summary>
        /// Attempt to hang up the call. 
        /// Before hanging up, all pre hang operations have to be finished. If any operation does not finish in time, the hang up will commence.
        /// If the attempt was successfully initiated, the OnCallHangUp event is fired. If the call itself is stopped, the OnCallDisconnected event is fired
        /// </summary>
        /// <returns>True, if the hang up attempt was successfully initiated, false otherwise</returns>
        public async Task<bool> HangupCall(float timeout = 10.0f, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // in case of no call
            if (RainbowInterface.CurrentCall == null)
            {
                return false;
            }

            using var lts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lts.CancelAfter((int)(timeout * 1000.0f));

            try
            {
                List<PreHangUpOperation> ops = preHangupOperations.Keys.ToList();
                List<Task> tasks = new();
                foreach (var op in ops)
                {
                    tasks.Add(op(lts.Token));
                }

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == lts.Token)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Debug.LogError($"[ConnectionModel] Pre hang up operations did not finish in time: {timeout}s");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // we still want to hang up, even if the pre operations did not succeed

            bool result = RainbowInterface.HangupCall();
            if (result)
            {
                OnCallHangUp?.Invoke(this);
            }
            else
            {
                // there doesn't seem to be a way to cancel not accepted calls...
                Debug.LogError("[ConnectionModel] Could not hang up call");
                if (RainbowInterface.CurrentCall == null)
                {
                    OnCallHangUp?.Invoke(this);
                }
            }

            return result;
        }

        /// <summary>
        /// Get the first currently connected contact, if it exists.
        /// In a P2P call, there will only be one other contact, so in that case, calling this method will get the current conversation partner.
        /// </summary>
        /// <returns>The first current contact if it exists, null otherwise</returns>
        public Contact GetConnectedContact()
        {
            if (RainbowInterface.CurrentCall == null)
            {
                return null;
            }

            Call cur = RainbowInterface.CurrentCall;
            if (!cur.IsActive())
            {
                return null;
            }

            // get first
            CallParticipant par = cur.Participants.FirstOrDefault();

            // there should be at least one participant in the call
            if (par == null)
            {
                return null;
            }

            // find first participant tat is not ourself
            // one's own id should not be part of the Participants list, so in P2P this should be correct (?)
            return RainbowInterface.RainbowApplication.GetContacts().GetContactFromContactId(par.UserId);
        }

        /// <summary>
        /// Requests a data channel for the given contact.
        /// When a data channel opens, the OnDataChannelOpened event is raised. When a data channel closes, the OnDataChannelClosed event is raised. 
        /// Additionally, if the resultCallback parameter is not null, it will be called with a bool indicating whether the opening was successful or not.
        /// </summary>
        /// <param name="contact">The contact to open a data channel for</param>
        /// <param name="resultCallback">Callback for whether the request succeeded or not</param>
        public void RequestDataChannel(Contact contact, Action<bool, Contact, DataChannel> resultCallback = null)
        {
            dataChannelService.RequestDataChannel(contact, "cortexDataChanel", new DataChannelRequestOptions(),
                x =>
                {
                    if (!x.Success)
                    {
                        resultCallback?.Invoke(false, contact, null);
                        return;
                    }

                    HandleOnMessage(contact, x.DataChannel);
                    HandleOnClose(contact, x.DataChannel);
                    HandleOnError(contact, x.DataChannel);

                    resultCallback?.Invoke(true, contact, x.DataChannel);
                    OnDataChannelOpened?.Invoke(this, contact);
                });
        }

        /// <summary>
        /// Requests a data channel for the given contact.
        /// When a data channel opens, the OnDataChannelOpened event is raised. When a data channel closes, the OnDataChannelClosed event is raised. 
        /// Additionally, if the resultCallback parameter is not null, it will be called with a bool indicating whether the opening was successful or not.
        /// </summary>
        /// <param name="contactId">The id of the contact to open a data channel for</param>
        /// <param name="resultCallback">Callback for whether the request succeeded or not</param>
        public void RequestDataChannel(string contactId, Action<bool, Contact, DataChannel> resultCallback = null)
        {
            Contact contact = Contacts.GetContactFromContactId(contactId);
            RequestDataChannel(contact, resultCallback);
        }

        /// <summary>
        /// Request that a data channel of a contact should be closed
        /// </summary>
        /// <param name="contact">The contact for which to close the data channel for</param>
        public void RequestCloseDataChannel(Contact contact)
        {
            dataChannelService?.GetDataChannel(contact)?.Close();
        }

        /// <summary>
        /// Request that a data channel of a contact should be closed
        /// </summary>
        /// <param name="contactId">The id of the contact for which to close the data channel for</param>
        public void RequestCloseDataChannel(string contactId)
        {
            Contact contact = Contacts.GetContactFromContactId(contactId);
            if (contact == null)
            {
                return;
            }

            RequestCloseDataChannel(contact);
        }

        /// <summary>
        /// Get the stream track of the given type for the given contact.
        /// Currently supported are Video and Sharing media tracks.
        /// </summary>
        /// <param name="contactId">The id of the contact to query</param>
        /// <param name="mediaType">The media type. The value must be part of Call.Media</param>
        /// <returns>The stream track, if it exists, null otherwise</returns>
        public IMediaStreamTrack GetStreamTrack(string contactId, int mediaType)
        {
            if (contactId == null)
            {
                return null;
            }

            lock (mediaEntriesLock)
            {
                if (mediaEntries.TryGetValue(contactId, out MediaEntry entry))
                {
                    if (mediaType == Call.Media.VIDEO)
                    {
                        return entry.videoTrack;
                    }
                    else if (mediaType == Call.Media.SHARING)
                    {
                        return entry.sharingTrack;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get the video stream track for the given contact, if it exists.
        /// This is equivalent of calling <code>GetStreamTrack(contactId, Call.Media.VIDEO)</code>
        /// </summary>
        /// <param name="contactId">The id of the contact to query</param>
        /// <returns>The video stream track, if it exists, null otherwise</returns>
        public IMediaStreamTrack GetStreamVideoTrack(string contactId)
        {
            return GetStreamTrack(contactId, Call.Media.VIDEO);
        }

        /// <summary>
        /// Get the sharing stream track for the given contact, if it exists.
        /// This is equivalent of calling <code>GetStreamTrack(contactId, Call.Media.SHARING)</code>
        /// </summary>
        /// <param name="contactId">The id of the contact to query</param>
        /// <returns>The sharing stream track, if it exists, null otherwise</returns>
        public IMediaStreamTrack GetStreamSharingTrack(string contactId)
        {
            return GetStreamTrack(contactId, Call.Media.SHARING);
        }

        #endregion // public methods

        #region internal fields

        private SimpleDataChannelService dataChannelService;

        private bool callConnected = false;

        private readonly ConcurrentDictionary<PreHangUpOperation, bool> preHangupOperations = new();

        // this is used to keep track of shared videos, since other consumers might miss an event if it happens in another scene
        private class MediaEntry
        {
            public IMediaStreamTrack videoTrack;
            public IMediaStreamTrack sharingTrack;
        }

        private readonly Dictionary<string, MediaEntry> mediaEntries = new();
        private readonly object mediaEntriesLock = new();

        #endregion  // internal fields

        #region unity lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;

            // First check if this object has a fitting component
            if (RainbowInterface == null)
            {
                RainbowInterface = GetComponent<RainbowInterface>();
            }

            // next try to find one on the scene
            if (RainbowInterface == null)
            {
                RainbowInterface = FindFirstObjectByType<RainbowInterface>();
            }

            // create new one if it doesn't exist
            if (RainbowInterface == null)
            {
                var o = new GameObject("RainbowInterface");
                RainbowInterface = o.AddComponent<RainbowInterface>();
            }

            DontDestroyOnLoad(transform);
        }

        void Start()
        {
            // connect callbacks
            RainbowInterface.ConnectionChanged += RainbowConnectionChanged;
            RainbowInterface.Ready += RainbowReadyCallback;

            RainbowInterface.OnApplicationInit += OnApplicationInit;
            RainbowInterface.OnApplicationDispose += OnApplicationDispose;
            if (RainbowInterface.RainbowApplication != null)
            {
                OnApplicationInit(RainbowInterface.RainbowApplication);
            }
        }

        void OnDestroy()
        {
            RainbowInterface.OnApplicationInit -= OnApplicationInit;
            RainbowInterface.OnApplicationDispose -= OnApplicationDispose;

            // disconnect callbacks
            RainbowInterface.ConnectionChanged -= RainbowConnectionChanged;
            RainbowInterface.Ready -= RainbowReadyCallback;

            if (RainbowInterface.RainbowApplication != null)
            {
                OnApplicationDispose(RainbowInterface.RainbowApplication);
            }

            dataChannelService?.Stop();
        }

        private void OnApplicationDispose(Rainbow.Application application)
        {
            application.AuthenticationFailed -= OnAuthenticationFailed;
            application.AuthenticationSucceeded -= OnAuthenticationSucceeded;

            var conferences = application.GetConferences();

            conferences.ConferenceParticipantsUpdated -= OnConferenceParticipantsUpdated;
            conferences.ConferenceTalkersUpdated -= OnConferenceTalkersUpdated;
            conferences.ConferenceUpdated -= OnConferenceUpdated;
        }

        private void OnApplicationInit(Rainbow.Application application)
        {
            // This is all very fragile
            application.AuthenticationFailed += OnAuthenticationFailed;
            application.AuthenticationSucceeded += OnAuthenticationSucceeded;

            var conferences = application.GetConferences();

            conferences.ConferenceParticipantsUpdated += OnConferenceParticipantsUpdated;
            conferences.ConferenceTalkersUpdated += OnConferenceTalkersUpdated;
            conferences.ConferenceUpdated += OnConferenceUpdated;
        }

        #endregion // unity lifecycle

        #region internal methods

        private void HandleOnClose(Contact contact, DataChannel channel)
        {
            channel.OnClose += () =>
             {
                 OnDataChannelClosed?.Invoke(this, contact);
             };
        }

        private void HandleOnError(Contact contact, DataChannel channel)
        {
            channel.OnError += (err) =>
             {
                 OnDataChannelError?.Invoke(this, contact, err);
             };
        }

        private void HandleOnMessage(Contact contact, DataChannel channel)
        {
            channel.OnMessage = msg =>
            {
                OnDataChannelMessage?.Invoke(this, contact, msg);
            };
        }

        #endregion // internal methods

        #region callbacks
        private void RainbowConnectionChanged(string connectionState)
        {
            // This is called in the unity thread by the controller, so we don't need to wrap the callbacks

            // TODO Improve connection logic
            // the way that these events are fired is a bit confusing, so this might need some work to handle all cases
            // for example, the connection state changes when adding media during a call
            // there also seem to be issues, when the server can't be reached during the login process

            bool isDisconnected = connectionState == "disconnected";

            if (State == ConnectionState.RequestLogin)
            {
                // we might disconnect during the login attempt, which seems to happen, when the username or password is wrong
                // it also might in other cases as well though, but then it seems to reconnect...
                // this seems like a useful workaround?
                if (isDisconnected && !RainbowInterface.RainbowApplication.IsInitialized())
                {
                    State = ConnectionState.Initial;
                    OnLoginFailed?.Invoke(this);
                }
            }
            else if (State == ConnectionState.LoggedIn)
            {
                // we might get disconnected
                if (isDisconnected)
                {
                    State = ConnectionState.Initial;

                    OnLogout?.Invoke(this);

                    // remove callbacks
                    dataChannelService.ProposalReceived -= ProposalReceived;

                    if (RainbowInterface.RainbowWebRTC != null)
                    {
                        RainbowInterface.RainbowWebRTC.CallUpdated -= CallbackCallUpdated;
                        RainbowInterface.RainbowWebRTC.OnMediaPublicationUpdated -= RainbowWebRTCOnMediaPublicationUpdated;
                        RainbowInterface.RainbowWebRTC.OnTrack -= RainbowWebRTCOnTrack;
                    }
                }
            }
        }

        private void RainbowReadyCallback(bool ready)
        {
            // This is called in the unity thread by the controller, so we don't need to wrap the callbacks

            if (State == ConnectionState.RequestLogin)
            {
                if (ready)
                {
                    State = ConnectionState.LoggedIn;
                    OnLogin?.Invoke(this);
                    // Establish new callbacks when logged in
                    dataChannelService = new(RainbowInterface.RainbowApplication);
                    dataChannelService.ProposalReceived += ProposalReceived;
                    RainbowInterface.RainbowWebRTC.CallUpdated += CallbackCallUpdated;
                    RainbowInterface.RainbowWebRTC.OnMediaPublicationUpdated += RainbowWebRTCOnMediaPublicationUpdated;
                    RainbowInterface.RainbowWebRTC.OnTrack += RainbowWebRTCOnTrack;
                }
                else
                {
                    State = ConnectionState.Initial;
                    OnLoginFailed?.Invoke(this);
                }
            }
            else
            {
                Debug.LogError("[ConnectionModel] Invalid state transition");
            }
        }

        private void RainbowWebRTCOnTrack(string callId, Rainbow.WebRTC.MediaStreamTrackDescriptor mediaStreamTrackDescriptor)
        {
            IMediaStreamTrack mediaStreamTrack = mediaStreamTrackDescriptor.MediaStreamTrack;
            int media = mediaStreamTrackDescriptor.Media;

            var pubId = mediaStreamTrackDescriptor.PublisherId;
            // TODO add events
            lock (mediaEntriesLock)
            {
                if (!mediaEntries.TryGetValue(pubId, out MediaEntry entry))
                {
                    entry = new();
                    mediaEntries[pubId] = entry;
                }

                if (media == Call.Media.VIDEO)
                {
                    entry.videoTrack = mediaStreamTrack;
                }
                else if (media == Call.Media.SHARING)
                {
                    entry.sharingTrack = mediaStreamTrack;
                }
            }

            // adapted from RainbowController
            if (mediaStreamTrackDescriptor.PublisherId != RainbowInterface.RainbowApplication.GetContacts().GetCurrentContactId())
            {
                // Rainbow controller deprecated video handling, so we pass this event on
                UnityExecutor.Execute(() =>
                {
                    OnRemoteTrack?.Invoke(this, callId, mediaStreamTrackDescriptor);
                });
            }
        }

        private void RainbowWebRTCOnMediaPublicationUpdated(object sender, Rainbow.WebRTC.MediaPublicationEventArgs e)
        {
            // adapted from RainbowController

            Contacts ContactService = RainbowInterface.RainbowApplication.GetContacts();

            // If there is no call / no callId, do nothing
            Call call = RainbowInterface.RainbowWebRTC.GetCall(e.MediaPublication.CallId);
            if (call == null)
            {
                return;
            }

            int media = e.MediaPublication.Media;
            string pubId = e.MediaPublication.PublisherId;

            lock (mediaEntriesLock)
            {
                if (e.Status == Rainbow.WebRTC.MediaPublicationStatus.PEER_STOPPED)
                {
                    if (mediaEntries.TryGetValue(pubId, out MediaEntry entry))
                    {
                        if (media == Call.Media.VIDEO)
                        {
                            entry.videoTrack = null;
                        }
                        else if (media == Call.Media.SHARING)
                        {
                            entry.sharingTrack = null;
                        }

                        // remove entry, if nothing is left
                        if (entry.sharingTrack == null && entry.videoTrack == null)
                        {
                            mediaEntries.Remove(pubId);
                        }
                    }
                }
            }

            // Ignore events related to our own publications
            if (e.MediaPublication.PublisherJid_im == ContactService.GetCurrentContactJid())
            {
                return;
            }

            // Rainbow controller deprecated video handling, so we pass this event on
            UnityExecutor.Execute(() =>
            {
                OnRemoteMediaPublicationUpdated?.Invoke(this, e);
            });
        }

        private void ProposalReceived(DataChannelProposal offer)
        {
            // we automatically accept data channels, since we need them
            offer.Accept(dc =>
            {
                if (dc.Success)
                {
                    HandleOnMessage(dc.Contact, dc.DataChannel);
                    HandleOnClose(dc.Contact, dc.DataChannel);

                    OnDataChannelOpened?.Invoke(this, dc.Contact);
                }
                else
                {
                    Debug.LogError($"[ConnectionModel] Data channel with {Util.GetContactDisplayName(dc.Contact)} failed with error: {dc.Error}.");
                }
            });
        }

        private void CallbackCallUpdated(object sender, CallEventArgs e)
        {
            if (e.Call != null)
            {
                UnityExecutor.Execute(() =>
                {
                    OnCallStatusUpdate?.Invoke(this, e);
                    if (e.Call.CallStatus == Call.Status.ACTIVE)
                    {
                        // the connection apparently jumps between states when adding media, so we need to handle this
                        if (!callConnected)
                        {
                            callConnected = true;

                            // TODO Update this if it could be solved better
                            // the code from which RainbowInterface is adapted mentions "will be retrieved by the com manager when the call is established, and the participants retrieved" when a media publication is received during dialing. It is not clear what that means, as existing video/shares are apparently not subscribed to. So for now we subscribe to all available media
                            var mediaPubs = RainbowInterface.RainbowWebRTC.GetMediaPublicationsAvailable(e.Call.Id);
                            foreach (var m in mediaPubs)
                            {
                                if (!RainbowInterface.RainbowWebRTC.IsSubscribedToMediaPublication(m))
                                {
                                    RainbowInterface.RainbowWebRTC.SubscribeToMediaPublication(m, Conference.SubStreamLevel.MIDDLE);
                                }
                            }

                            OnCallConnected?.Invoke(this, e.Call);

                            // open necessary data channels
                        }
                    }
                    else if (e.Call.CallStatus == Call.Status.ERROR || e.Call.CallStatus == Call.Status.UNKNOWN)
                    {
                        callConnected = false;
                        // active call may have ended/be in an unknown state
                        // this seems to signify that the connection has been severed and the call was hung up
                        CleanUpCallDisconnected();
                        OnCallDisconnected?.Invoke(this, e.Call);
                    }
                });
            }
        }

        private void CleanUpCallDisconnected()
        {
            // some state seems to not be automatically cleared by rainbow
            // media publications do not generate a stopped event after the call ended, so we do it manually

            lock (mediaEntriesLock)
            {
                mediaEntries.Clear();
            }
        }

        private void OnConferenceTalkersUpdated(object sender, ConferenceTalkersEventArgs e)
        {
            UnityExecutor.Execute(() =>
            {
                OnConferenceTalkersUpdate?.Invoke(this, e);
            });
        }

        private void OnConferenceUpdated(object sender, ConferenceEventArgs e)
        {
            UnityExecutor.Execute(() =>
          {
              OnConferenceUpdate?.Invoke(this, e);
          });
        }

        private void OnConferenceParticipantsUpdated(object sender, ConferenceParticipantsEventArgs e)
        {
            UnityExecutor.Execute(() =>
            {
                OnConferenceParticipantsUpdate?.Invoke(this, e);
            });
        }

        private void OnAuthenticationSucceeded(object sender, EventArgs e)
        {
            //     UnityExecutor.Execute(() =>
            //    {
            //        Debug.Log("[ConnectionModel] Authentication succeeded");
            //    });
        }

        private void OnAuthenticationFailed(object sender, SdkErrorEventArgs e)
        {
            UnityExecutor.Execute(() =>
            {
                if (e.SdkError.Type == SdkError.SdkErrorType.Exception)
                {
                    Debug.LogError($"[ConnectionModel] Authentication failed (Exception): {e.SdkError.ExceptionError},");
                }

                State = ConnectionState.Initial;
                OnLoginFailed?.Invoke(this);
            });
        }

        #endregion // callbacks
    }
} // end namespace Cortex