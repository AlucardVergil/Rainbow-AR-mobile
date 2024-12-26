using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rainbow.Model;
using SimpleDataChannelRainbow;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cortex
{
    /// <summary>
    /// Class for grouping the data of a message with some convenience methods
    /// </summary>
    public class MessageData
    {
        /// <summary>
        /// Construct a new MessageData object
        /// </summary>
        /// <param name="connectionModel">The connection that this message comes from</param>
        /// <param name="contactId">The id of the contact that sent the message</param>
        /// <param name="data">The message data</param>
        /// <param name="topicName">The topic that this message comes from</param>
        /// <param name="messageId">The unique id of the message</param>
        public MessageData(DataTransportManager connectionModel, string contactId, byte[] data, string topicName, Guid messageId)
        {
            _data = data;
            DataTransportManager = connectionModel;
            TopicName = topicName;
            MessageId = messageId;
            ContactId = contactId;
        }

        /// <summary>
        /// The unique id of the message
        /// </summary>
        public Guid MessageId { get; private set; }

        /// <summary>
        /// The topic that this message comes from 
        /// </summary>
        public string TopicName { get; private set; }

        /// <summary>
        /// The id of the contact that sent the message
        /// </summary>
        public string ContactId { get; private set; }

        /// <summary>
        /// A view of the data contained in the message
        /// </summary>
        public ReadOnlySpan<byte> Data { get => _data; }

        /// <summary>
        /// Get the data as an UTF-8 encoded string.
        /// This conversion is cached and only done once, so multiple calls have negligible overhead
        /// </summary>
        public string Utf8
        {
            get
            {
                lock (dataLock)
                {
                    // for now we only cache the converted string
                    if (string.IsNullOrEmpty(dataUtf8))
                    {
                        dataUtf8 = Encoding.UTF8.GetString(_data);
                    }
                    return dataUtf8;
                }
            }
        }

        /// <summary>
        /// The DataTransportManager instance
        /// </summary>
        public DataTransportManager DataTransportManager { get; private set; }

        /// <summary>
        /// Parse the message data, as if it were an UTF-8 encoded JSON string and try to convert it to the given type
        /// </summary>
        /// <typeparam name="T">The type to parse the message into</typeparam>
        /// <returns>The parsed result.</returns>
        public T ParseJson<T>()
        {
            // TODO maybe cache values, though that is a bit problematic, since there could both be value and reference types
            return JsonUtility.FromJson<T>(Utf8);
        }

        private byte[] _data;

        private string dataUtf8;

        private readonly object dataLock = new();
    }

    /// <summary>
    /// Codes indicating in what way a message was answered
    /// </summary>
    public enum MessageAnswerCode : int
    {
        Accept, Deny, NoResponse
    }

    /// <summary>
    /// This class allows message handlers to signal acceptance or denial of a given message.
    /// It is also possible to send data back on top to allow for direct communication, if needed
    /// </summary>
    public class MessageAnswer
    {

        /// <summary>
        /// The id of the contact that sent the message
        /// </summary>
        public string ContactId { get; private set; }
        /// <summary>
        /// The type id of the message
        /// </summary>
        public string MessageTypeId { get; private set; }
        /// <summary>
        /// The id of the message
        /// </summary>
        public Guid MessageId { get; private set; }

        /// <summary>
        /// Whether this answer has already been processed, that is accepted or denied
        /// </summary>
        public bool Processed { get; private set; } = false;

        private readonly DataTransportManager manager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manager">The manager that is sending a message</param>
        /// <param name="contactId">The id of the contact that sent the message</param>
        /// <param name="messageTypeId">The type id of the message</param>
        /// <param name="messageId">The id of the message</param>
        public MessageAnswer(DataTransportManager manager, string contactId, string messageTypeId, Guid messageId)
        {
            ContactId = contactId;
            MessageTypeId = messageTypeId;
            MessageId = messageId;

            this.manager = manager;
        }

        /// <summary>
        /// Accept the received message
        /// </summary>
        /// <param name="message">Additional information about the acceptance</param>
        public void Accept(string message = "")
        {
            Accept(Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Accept the received message
        /// </summary>
        /// <typeparam name="T">The type of the message answer data</typeparam>
        /// <param name="msg">Data to send as a direct answer</param>
        public void Accept<T>(T msg)
        {
            Accept(JsonUtility.ToJson(msg));
        }
        /// <summary>
        /// Accept the received message
        /// </summary>
        /// <param name="data">Additional information about the acceptance</param>
        public void Accept(byte[] data)
        {
            Process(MessageAnswerCode.Accept, data);
        }

        /// <summary>
        /// Deny the received message
        /// </summary>
        /// <param name="message">Additional information about the denial</param>
        public void Deny(string message = "")
        {
            Deny(Encoding.UTF8.GetBytes(message));
        }
        /// Deny the received message
        /// </summary>
        /// <typeparam name="T">The type of the message answer data</typeparam>
        /// <param name="msg">Data to send as a direct answer</param>
        public void Deny<T>(T msg)
        {
            Deny(JsonUtility.ToJson(msg));
        }

        /// <summary>
        /// Deny the received message
        /// </summary>
        /// <param name="data">Data to send as a direct answer</param>
        public void Deny(byte[] data)
        {
            Process(MessageAnswerCode.Deny, data);

        }

        private void Process(MessageAnswerCode status, byte[] data)
        {
            if (Processed)
            {
                return;
            }
            manager.SendMessageAnswer(ContactId, status, MessageTypeId, MessageId, data);

            Processed = true;
        }
    }

    /// <summary>
    /// This class can be used to send and receive data over Rainbow data channels
    /// </summary>
    public class DataTransportManager : MonoBehaviour
    {
        /// <summary>
        /// Global instance
        /// </summary>
        public static DataTransportManager Instance { get; private set; }

        #region events

        /// <summary>
        /// Handler for ready events of a DataTransportManager
        /// </summary>
        /// <param name="manager">The manager</param>
        /// <param name="contact">The contact that is ready for data exchange</param>
        /// <param name="channel">The data channel that is ready. Usually, this shouldn't be manually written to</param>
        public delegate void ReadyHandler(DataTransportManager manager, Contact contact, DataChannel channel);
        /// <summary>
        /// Event to be called when the DataTransportManager is ready to send data to and receive data from a contact
        /// </summary>
        public static event ReadyHandler OnReady;

        /// <summary>
        /// Handler for close events of a DataTransportManager
        /// </summary>
        /// <param name="manager">The manager</param>
        /// <param name="contact">The contact whose data channel was closed</param>
        public delegate void CloseHandler(DataTransportManager manager, Contact contact);
        /// <summary>
        /// Event to be called when the data channel of a contact closed
        /// </summary>
        public static event CloseHandler OnClose;

        #endregion

        #region public

        /// <summary>
        /// If enabled, messages sent to a contact without an active data channel are queued and sent when the channel becomes available.
        /// Note: Due to some issues with the data channels not closing correctly, this is currently recommended to be set to false
        /// </summary>
        public bool EnableMessageQueue = false;

        /// <summary>
        /// Handler for message answers.
        /// If a contact did answer, the message was either accepted or denied. In that case, the contact that answered is specified in the data object.
        /// If a timeout occurred, no contact responded. This is indicated with the answer code. In that case, the data object will not contain information about a contact, nor any data.
        /// 
        /// Returning true indicates, that the answer was consumed and removes this message, otherwise it will still wait for additional answers.
        /// </summary>
        /// <param name="answerCode">Code specifying the type of answer</param>
        /// <param name="data">Data that was send as an answer</param>
        /// <returns>True, if this handler consumed the message, false otherwise</returns>
        public delegate bool MessageAnswerHandler(MessageAnswerCode answerCode, MessageData data);

        /// <summary>
        /// Creates a callback that can be uses as a message answer handler, that accepts the answer and stores the results in a Task, so it can be used in an async context.
        /// </summary>
        /// <param name="task">The task for waiting for an answer</param>
        /// <param name="cancellationToken">Cancellation token to stop the given task</param>
        /// <returns>A new MessageAnswerHandler that signals the arrival of an answer via the created Task object</returns>
        public static MessageAnswerHandler WrapAnswerHandler(out Task<(MessageAnswerCode, MessageData)> task, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<(MessageAnswerCode, MessageData)> source = new();
            task = source.Task;

            using var registration = cancellationToken.Register(() =>
            {
                // this callback will be executed when token is cancelled
                source.TrySetCanceled(cancellationToken);
            });

            return (answerCode, data) =>
            {
                source.TrySetResult((answerCode, data));
                return true;
            };
        }

        /// <summary>
        /// State that the data transport manager's connection can be in
        /// </summary>
        public enum ConnectionState
        {
            INITIAL, CONNECTING, CONNECTED, CLOSING, ERROR, NONE
        }

        /// <summary>
        /// If a message is send before a data channel is active, it won't be sent. These messages are queued and then send out later
        /// </summary>
        [Tooltip("The maximum number of messages queued for sending")]
        public int MaxMessageQueueLength = 100;

        /// <summary>
        /// Callback for generic messages.
        /// Receivers/Senders can control their own data representation. For compatibility, UTF-8 encoded JSON data is reasonable
        /// </summary>
        /// <param name="data">The received message data</param>
        /// <param name="answer">The answer object</param>
        public delegate void HandleMessage(MessageData data, MessageAnswer answer);

        /// <summary>
        /// Callback for generic outgoing messages.
        /// A client does not receive its own messages by default. This is to prevent loops.
        /// </summary>
        /// <param name="data">The data that is sent</param>
        /// <param name="contactId">The contact to which the data is sent</param>
        public delegate void OutgoingHandleMessage(MessageData data, string contactId);

        /// <summary>
        /// Register a message handler for the message published on the given topic
        /// </summary>
        /// <param name="topicName">The topic name</param>
        /// <param name="handler">The handler that is called when a new message arrives</param>
        public static void RegisterMessageHandler(string topicName, HandleMessage handler)
        {
            lock (messageHandlerLock)
            {
                if (messageHandlers.TryGetValue(topicName, out HandleMessage current))
                {
                    messageHandlers[topicName] = current + handler;
                }
                else
                {
                    messageHandlers[topicName] = handler;
                }
            }
        }

        /// <summary>
        /// Removes a message handler for the message published on the given topic
        /// </summary>
        /// <param name="topicName">The topic name</param>
        /// <param name="handler">The handler that was registered before</param>
        /// <returns>True, if the handler was registered before and removed, false otherwise</returns>
        public static void RemoveMessageHandler(string topicName, HandleMessage handler)
        {
            lock (messageHandlerLock)
            {
                if (messageHandlers.TryGetValue(topicName, out HandleMessage current))
                {
                    // only needs to be removed if the key was there
                    messageHandlers[topicName] = current - handler;
                }
            }
        }

        /// <summary>
        /// Register a message handler that receives messages from all topics
        /// </summary>
        /// <param name="handler">The handler that is called when a new message arrives</param>
        public static void RegisterMessageHandler(HandleMessage handler)
        {
            allHandlers += handler;
        }

        /// <summary>
        /// Remove a message handler that was registered for all topics
        /// </summary>
        /// <param name="handler"></param>
        public static void RemoveMessageHandler(HandleMessage handler)
        {
            allHandlers -= handler;
        }

        /// <summary>
        /// Register an outgoing message handler for the message published on the given topic.
        /// Clients will not receive messages published by themselves. This is to prevent loops in message handling. It also allows to create a more symmetric interface.
        /// Outgoing messages can still be subscribed to, for various reasons, such as logging or debugging.
        /// </summary>
        /// <param name="topicName">The topic name</param>
        /// <param name="handler">The handler that is called when a new message is sent</param>
        public static void RegisterOutgoingMessageHandler(string topicName, OutgoingHandleMessage handler)
        {
            lock (outMessageHandlerLock)
            {
                if (outMessageHandlers.TryGetValue(topicName, out OutgoingHandleMessage current))
                {
                    outMessageHandlers[topicName] = current + handler;
                }
                else
                {
                    outMessageHandlers[topicName] = handler;
                }
                hasOutgoing = true;
            }

        }

        /// <summary>
        /// Removes an outgoing message handler for the message published on the given topic
        /// </summary>
        /// <param name="topicName">The topic name</param>
        /// <param name="handler">The handler that was registered before</param>
        /// <returns>True, if the handler was registered before and removed, false otherwise</returns>
        public static void RemoveOutgoingMessageHandler(string topicName, OutgoingHandleMessage handler)
        {
            lock (outMessageHandlerLock)
            {
                if (outMessageHandlers.TryGetValue(topicName, out OutgoingHandleMessage current))
                {
                    // only needs to be removed if the key was there
                    outMessageHandlers[topicName] = current - handler;
                }

                if (outMessageHandlers.Any() && allOutHandlers != null)
                {
                    hasOutgoing = false;
                }
            }
        }

        /// <summary>
        /// Register an outgoing message handler for the message published on all topics.
        /// Clients will not receive messages published by themselves. This is to prevent loops in message handling. It also allows to create a more symmetric interface.
        /// Outgoing messages can still be subscribed to, for various reasons, such as logging or debugging.
        /// </summary>
        /// <param name="handler">The handler that is called when a new message arrives</param>
        public static void RegisterOutgoingMessageHandler(OutgoingHandleMessage handler)
        {
            lock (outMessageHandlerLock)
            {
                allOutHandlers += handler;
                hasOutgoing = true;
            }
        }

        /// <summary>
        /// Remove an outgoing message handler that was registered for all topics
        /// </summary>
        /// <param name="handler"></param>
        public static void RemoveOutgoingMessageHandler(OutgoingHandleMessage handler)
        {

            lock (outMessageHandlerLock)
            {
                allOutHandlers -= handler;
                if (outMessageHandlers.Any() && allOutHandlers != null)
                {
                    hasOutgoing = false;
                }
            }
        }

        /// <summary>
        /// Get the ids of all contacts with an active data channel
        /// </summary>
        /// <returns>A list containing the contact ids</returns>
        public List<string> GetActiveDataChannelContactIds()
        {
            lock (dataChannelLock)
            {
                return dataChannels.Where(p => p.Value.State == ConnectionState.CONNECTED).Select(p => p.Key).ToList();
            }
        }

        /// <summary>
        /// Get the ids of all contacts with a non-errored data channel.
        /// This might also include channels that are not yet fully functional.
        /// </summary>
        /// <returns>A list containing the contact ids</returns>
        public List<string> GetDataChannelContactIds()
        {
            lock (dataChannelLock)
            {
                return dataChannels.Where(p => p.Value.State != ConnectionState.ERROR).Select(p => p.Key).ToList();
            }
        }

        /// <summary>
        /// Check, whether the contact with the given id has an active data channel.
        /// An active data channel is one that exists and is ready to send data
        /// </summary>
        /// <param name="contactId">The id of a contact</param>
        /// <returns>True, if the contact has an active data channel, false otherwise</returns>
        public bool HasActiveChannel(string contactId)
        {
            return GetActiveDataChannel(contactId) != null;
        }
        /// <summary>
        /// Check, whether the contact with the given id has a data channel.
        /// </summary>
        /// <param name="contactId">The id of a contact</param>
        /// <returns>True, if the contact has a data channel, false otherwise</returns>
        public bool HasDataChannel(string contactId)
        {
            return GetDataChannel(contactId) != null; ;
        }

        /// <summary>
        /// Get the state of the data channel for the given contact.
        /// </summary>
        /// <param name="contact">The contact</param>
        /// <returns>The state of the contact. If no data channel exists, ConnectionState.None is returned</returns>
        public ConnectionState GetDataChannelState(Contact contact)
        {
            return GetDataChannelState(contact.Id);
        }

        /// <summary>
        /// Get the state of the data channel for the given contact.
        /// </summary>
        /// <param name="contactId">The id of a contact</param>
        /// <returns>The state of the contact. If no data channel exists, ConnectionState.None is returned</returns>
        public ConnectionState GetDataChannelState(string contactId)
        {
            if (dataChannels.TryGetValue(contactId, out var entry))
            {
                return entry.State;
            }

            return ConnectionState.NONE;
        }
        /// <summary>
        /// Decodes a UTF-8/JSON message from its byte representation.
        /// This is a convenience method to restore data that was sent via SendMessageDataJson (or encoded to json manually).
        /// For more information about the decoding process, see JsonUtility.FromJson
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="data">The byte data</param>
        /// <returns>The decoded data or null, if the data was empty</returns>
        public static T DecodeMessageFromJsonBytes<T>(ReadOnlySpan<byte> data)
        {
            var jsonString = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(jsonString);
        }

        /// <summary>
        /// Encodes a given message to an UTF-8 encoded JSON string in its byte representation.
        /// This is a convenience method to send data via the SendMessageData methods.
        /// For more information about the encoding process, see JsonUtility.ToJson
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] EncodeMessageToJsonBytes<T>(T msg)
        {
            var json = JsonUtility.ToJson(msg);

            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Broadcasts generic data as a message to all current call participants.
        /// Senders and receivers must take care of the encoding and decoding process
        /// </summary>
        /// <param name="topicName">The message type's UUID</param>
        /// <param name="data">The message data</param>
        /// <param name="answerHandler"The answer handler to be called, when a recipient responds to this message</param>
        /// <param name="timeout">The maximum time to wait for a response</param>
        /// <param name="sendCallback">Callback that is called when the message is sent</param>
        /// <returns>True, if the contact exists and either the data channel is open or the message queue is not full. False otherwise</returns>
        public bool BroadcastData(string topicName, ReadOnlySpan<byte> data, MessageAnswerHandler answerHandler = null, float timeout = 120.0f, Action<string> sendCallback = null)
        {
            List<DataChannelEntry> targets = GetActiveDataChannels();

            bool allSent = true;
            bool anySent = false;

            var messageId = Guid.NewGuid();
            byte[] msgData = EncodeMessageData(topicName, data, messageId);

            List<OutgoingHandleMessage> outHandlers = new();
            lock (outMessageHandlerLock)
            {
                if (hasOutgoing)
                {
                    if (outMessageHandlers.TryGetValue(topicName, out var handler))
                    {
                        outHandlers.Add(handler);
                    }

                    if (allOutHandlers != null)
                    {
                        outHandlers.Add(allOutHandlers);
                    }
                }
            }

            var model = ConnectionModel.Instance;

            if (model != null && model.CurrentUser != null && outHandlers.Any())
            {
                MessageData outgoingData = new(this, model.CurrentUser.Id, data.ToArray(), topicName, messageId);
                foreach (var entry in targets)
                {
                    foreach (var h in outHandlers)
                    {
                        h.Invoke(outgoingData, entry.ContactId);
                    }
                }

            }

            foreach (var entry in targets)
            {
                bool sent = EnqueueMessage(entry.ContactId, entry.DataChannel, msgData, messageId, sendCallback);

                allSent &= sent;
                anySent |= sent;
            }

            if (anySent)
            {
                AddMessageAnswerHandler(topicName, messageId, answerHandler, timeout);
            }

            return allSent;
        }

        /// <summary>
        /// Broadcasts typed data as a message to all current call participants. The message type must support (de)serialization to and from JSON
        /// </summary>
        /// <typeparam name="T">The message data type</typeparam>
        /// <param name="topicName">The topic name</param>
        /// <param name="data">The data to send</param>
        /// <param name="answerHandler">The answer handler to be called, when a recipient responds to this message</param>
        /// <param name="timeout">The maximum time to wait for a response</param>
        /// <param name="sendCallback">Callback that is called when the message is sent</param>
        /// <returns></returns>
        public bool BroadcastTypedData<T>(string topicName, T data, MessageAnswerHandler answerHandler = null, float timeout = 120.0f, Action<string> sendCallback = null)
        {
            return BroadcastData(topicName, EncodeMessageToJsonBytes(data), answerHandler, timeout, sendCallback);
        }

        /// <summary>
        /// Send generic data as a message.
        /// Senders and receivers must take care of the encoding and decoding process
        /// </summary>
        /// <param name="contactId">The id of the contact to send data to</param>
        /// <param name="topicName">The topic name</param>
        /// <param name="data">The message data</param>
        /// <param name="answerHandler">The answer handler to be called, when a recipient responds to this message</param>
        /// <param name="timeout">The maximum time to wait for a response</param>
        /// <param name="sendCallback">Callback that is called when the message is sent</param>
        /// <returns>True, if the contact exists and either the data channel is open or the message queue is not full. False otherwise</returns>
        public bool SendData(string contactId, string topicName, ReadOnlySpan<byte> data, MessageAnswerHandler answerHandler = null, float timeout = 120.0f, Action<string> sendCallback = null)
        {
            if (contactId == null)
            {
                return false;
            }
            DataChannel channel = GetDataChannel(contactId);

            var messageId = Guid.NewGuid();
            byte[] msgData = EncodeMessageData(topicName, data, messageId);

            bool sent = EnqueueMessage(contactId, channel, msgData, messageId, sendCallback);

            List<OutgoingHandleMessage> outHandlers = new();
            lock (outMessageHandlerLock)
            {
                if (hasOutgoing)
                {
                    if (outMessageHandlers.TryGetValue(topicName, out var handler))
                    {
                        outHandlers.Add(handler);
                    }

                    if (allOutHandlers != null)
                    {
                        outHandlers.Add(allOutHandlers);
                    }
                }
            }

            if (outHandlers.Any())
            {
                MessageData outgoingData = new(this, ConnectionModel.Instance.CurrentUser.Id, data.ToArray(), topicName, messageId);
                foreach (var h in outHandlers)
                {
                    h.Invoke(outgoingData, contactId);
                }
            }

            if (!sent)
            {
                return false;
            }

            AddMessageAnswerHandler(topicName, messageId, answerHandler, timeout);

            return true;

        }

        /// <summary>
        /// Send typed data as a message. The message type must support (de)serialization to and from JSON
        /// </summary>
        /// <typeparam name="T">The message data type</typeparam>
        /// <param name="contactId">The id of the contact to send data to</param>
        /// <param name="topicName">The topic name</param>
        /// <param name="data">The data to send</param>
        /// <param name="answerHandler">The answer handler to be called, when a recipient responds to this message</param>
        /// <param name="timeout">The maximum time to wait for a response</param>
        /// <param name="sendCallback">Callback that is called when the message is sent</param>
        /// <returns>True, if the contact exists and either the data channel is open or the message queue is not full. False otherwise</returns>
        public bool SendTypedData<T>(string contactId, string topicName, T data, MessageAnswerHandler answerHandler = null, float timeout = 120.0f, Action<string> sendCallback = null)
        {
            return SendData(contactId, topicName, EncodeMessageToJsonBytes(data), answerHandler, timeout, sendCallback);
        }

        /// <summary>
        ///  Clear the current message queue
        /// </summary>
        public void ClearQueue()
        {
            messageQueueElements.Clear();
        }

        /// <summary>
        /// Clears the queue for a specified contact
        /// </summary>
        /// <param name="contactId">The id of the contact to clear the queue of</param>
        public void ClearQueue(string contactId)
        {
            messageQueueElements.Remove(contactId, out var _);
        }

        /// <summary>
        /// Manually remove a data channel from this. Be careful, if you call this, the data channel will not be usable any more unless opened anew.
        /// This operation will also clear that contact's message queue
        /// </summary>
        /// <param name="contactId">The id of the contact for which to remove the data channel for</param>
        /// <returns></returns>
        public bool RemoveDataChannel(string contactId)
        {
            ClearQueue(contactId);

            bool removed = false;
            lock (dataChannelLock)
            {
                removed = dataChannels.Remove(contactId, out var _);
            }
            if (removed)
            {
                OnClose?.Invoke(this, ConnectionModel.Instance.Contacts.GetContactFromContactId(contactId));
            }

            return removed;
        }

        #endregion // public

        #region internal
        private static readonly object messageHandlerLock = new();

        private static readonly Dictionary<string, HandleMessage> messageHandlers = new();
        private static HandleMessage allHandlers;

        private static readonly Dictionary<string, OutgoingHandleMessage> outMessageHandlers = new();
        private static OutgoingHandleMessage allOutHandlers;

        // used to allow skipping copying data for internal usage
        private static bool hasOutgoing = false;

        private static readonly object outMessageHandlerLock = new();

        // POD type to group message answer data
        private class MessageAnswerData
        {
            public MessageAnswerHandler handler;
            public float timeout = 120.0f;

            public string topic;
        }
        private static readonly ConcurrentDictionary<Guid, MessageAnswerData> answerHandlers = new();
        private enum MessageIdentifiers : int
        {
            PING, PONG, PAYLOAD_MESSAGE, ANSWER
        }

        private readonly ConcurrentDictionary<Contact, Coroutine> tryPingCoroutines = new();

        /// <summary>
        ///  Entries for current data channels
        /// </summary>
        private class DataChannelEntry
        {
            public readonly string ContactId;
            public readonly DataChannel DataChannel;
            public readonly ConnectionState State;

            public DataChannelEntry(string contactId, DataChannel dataChannel, ConnectionState state)
            {
                ContactId = contactId;
                DataChannel = dataChannel;
                State = state;
            }

            public DataChannelEntry(DataChannelEntry other)
            {
                ContactId = other.ContactId;
                DataChannel = other.DataChannel;
                State = other.State;
            }
        }

        /// <summary>
        /// Current data channels indexed by Contact.Id
        /// </summary>
        private readonly Dictionary<string, DataChannelEntry> dataChannels = new();

        private readonly object dataChannelLock = new();

        private class MessageQueueElement
        {
            public string contactId;
            public byte[] data;

            public Action<string> sendCallback;

            public Guid id;
        }
        private readonly ConcurrentDictionary<string, ConcurrentQueue<MessageQueueElement>> messageQueueElements = new();

        #endregion

        #region unity lifecycle
        void Awake()
        {
            // Only one instance allowed
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
            DontDestroyOnLoad(transform);

        }
        // Start is called before the first frame update
        void Start()
        {
            ConnectionModel.OnDataChannelMessage += OnDataChannelMessage;
            ConnectionModel.OnDataChannelOpened += OnDataChannelOpened;
            ConnectionModel.OnDataChannelClosed += OnDataChannelClosed;
            ConnectionModel.OnDataChannelError += OnDataChannelError;

            ConnectionModel.OnCallDisconnected += OnCallDisconnected;
            ConnectionModel.OnLogout += OnLogout;

        }

        void OnDestroy()
        {
            StopAllCoroutines();
            // It seems as though this might not always happen automatically...
            CloseAllDataChannels();
            ConnectionModel.OnDataChannelMessage -= OnDataChannelMessage;
            ConnectionModel.OnDataChannelOpened -= OnDataChannelOpened;
            ConnectionModel.OnDataChannelClosed -= OnDataChannelClosed;
            ConnectionModel.OnDataChannelError -= OnDataChannelError;

            ConnectionModel.OnCallDisconnected -= OnCallDisconnected;
            ConnectionModel.OnLogout -= OnLogout;

        }

        private void OnDataChannelError(ConnectionModel model, Contact contact, RTCError error)
        {
            lock (dataChannelLock)
            {
                if (dataChannels.Remove(contact.Id, out var channel))
                {
                    DataChannelEntry newEntry = new(channel.ContactId, channel.DataChannel, ConnectionState.ERROR);
                    dataChannels[contact.Id] = newEntry;
                }
            }
        }

        private void CloseAllDataChannels()
        {

            List<string> ids = new();
            lock (dataChannelLock)
            {
                foreach (string id in dataChannels.Keys)
                {
                    ids.Add(id);
                }
            }

            foreach (string id in ids)
            {
                CloseDataChannel(id);
            }

        }
        private void CloseDataChannel(string contactId)
        {
            lock (dataChannelLock)
            {
                if (!dataChannels.TryGetValue(contactId, out DataChannelEntry entry))
                {
                    return;
                }

                entry.DataChannel.Close();
                DataChannelEntry newState = new(entry.ContactId, entry.DataChannel, ConnectionState.CLOSING);
                dataChannels[contactId] = newState;
            }

        }

        private void OnLogout(ConnectionModel model)
        {
            // It seems as though this might not always happen automatically...
            // Though the .Close() method doesn't result in an onClose event...

            CloseAllDataChannels();

        }

        private void OnCallDisconnected(ConnectionModel model, Call call)
        {
            // It seems as though this might not always happen automatically...
            // Though the .Close() method doesn't result in an onClose event...
            CloseAllDataChannels();

        }

        void Update()
        {

            // handle message answer timeouts
            List<Guid> toRemove = new();
            foreach (KeyValuePair<Guid, MessageAnswerData> data in answerHandlers)
            {
                data.Value.timeout -= Time.deltaTime;

                if (data.Value.timeout < 0.0f)
                {
                    toRemove.Add(data.Key);
                }
            }

            // notify answer handlers, that a timeout occurred and remove them from the list
            foreach (var guid in toRemove)
            {
                if (answerHandlers.TryRemove(guid, out MessageAnswerData data))
                {
                    MessageData messageData = new(this, null, null, data.topic, guid);
                    data.handler(MessageAnswerCode.NoResponse, messageData);

                }
            }
            // process messages
            // TODO do not process messages in a queue, as this could block messages, when multiple clients are involved
            // currently, only one client receives messages, but if there are more, we could either just iterate through the queue or keep a queue per contact

            foreach (var (contactId, queue) in messageQueueElements)
            {
                if (!HasActiveChannel(contactId))
                {
                    continue;
                }

                while (queue.TryPeek(out MessageQueueElement e))
                {
                    DataChannelEntry entry = null;
                    lock (dataChannelLock)
                    {
                        if (!dataChannels.TryGetValue(e.contactId, out entry))
                        {
                            break;
                        }

                    }

                    DataChannel channel = entry.DataChannel;
                    ConnectionState state = entry.State;
                    if (channel == null || state != ConnectionState.CONNECTED)
                    {
                        // channel is not open, so we wait a frame and then check again
                        break;
                    }

                    if (queue.TryDequeue(out MessageQueueElement ed))
                    {
                        // e and ed are the same, since we only dequeue from here
                        // we only differentiate between the two so that possible concurrent inserts don't extend over the max size
                        Assert.AreEqual(e, ed);
                        channel.Send(ed.data);
                        ed.sendCallback?.Invoke(ed.contactId);
                    }
                }
            }
            // TODO add RemoveIf for message queue to remove queue, if no messages are left

        }

        #endregion

        #region callbacks

        private void OnDataChannelClosed(ConnectionModel model, Contact contact)
        {
            lock (dataChannelLock)
            {
                dataChannels.Remove(contact.Id);
            }
            OnClose?.Invoke(this, contact);
        }

        private void OnDataChannelOpened(ConnectionModel model, Contact contact)
        {
            var dataChannel = model.GetDataChannel(contact);
            DataChannelEntry entry = new(contact.Id, dataChannel, ConnectionState.CONNECTING);
            lock (dataChannelLock)
            {
                dataChannels[contact.Id] = entry;
            }
            StartPingCoroutine(contact, dataChannel);
        }

        /// <summary>
        /// Get the data channel for a contact if it exists
        /// </summary>
        /// <param name="contact">The contact</param>
        /// <returns>The data channel for the contact or null if it does not exist</returns>
        public DataChannel GetDataChannel(Contact contact)
        {
            return GetDataChannel(contact.Id);
        }

        /// <summary>
        /// Get the data channel for a contact if it exists
        /// </summary>
        /// <param name="contactId">The id of the contact</param>
        /// <returns>The data channel for the contact or null if it does not exist</returns>
        public DataChannel GetDataChannel(string contactId)
        {
            lock (dataChannelLock)
            {
                if (dataChannels.TryGetValue(contactId, out DataChannelEntry entry))
                {
                    return entry.DataChannel;
                }
            }

            return null;
        }

        private DataChannel GetActiveDataChannel(Contact c)
        {
            return GetActiveDataChannel(c.Id);
        }

        private DataChannel GetActiveDataChannel(string contactId)
        {
            lock (dataChannelLock)
            {
                if (dataChannels.TryGetValue(contactId, out DataChannelEntry entry))
                {
                    if (entry.State == ConnectionState.CONNECTED)
                    {
                        return entry.DataChannel;
                    }
                }
            }

            return null;
        }

        private void HandlePongLike(Contact contact)
        {
            // it might happen, that we receive a message from a data channel that has not yet gone through the ping-pong procedure from this side, so it isn't yet flagged as active.
            // Thus if we want to answer such a message by checking active data channels, it won't show up.
            // So when we receive data from a data channel, we will treat it as an implicit "ping-pong"
            // We could use a different/better handshake to make sure, both sides know they are ready at the same time, but for now it works

            // got pong equivalent response -> ready
            DataChannel dataChannel = null;
            lock (dataChannelLock)
            {
                if (dataChannels.TryGetValue(contact.Id, out DataChannelEntry entry))
                {
                    // TODO should we check for other special states or just consider a channel that we receive data from active?
                    if (entry.State != ConnectionState.CONNECTED)
                    {
                        DataChannelEntry newState = new(entry.ContactId, entry.DataChannel, ConnectionState.CONNECTED);
                        dataChannels[contact.Id] = newState;
                        dataChannel = entry.DataChannel;
                    }

                }
            }

            if (dataChannel != null)
            {
                StopPingCoroutine(contact);
                OnReady?.Invoke(this, contact, dataChannel);
            }

        }
        private void OnDataChannelMessage(ConnectionModel model, Contact contact, byte[] data)
        {
            // message starts with an int identifier
            // this might change when introducing a header
            if (data.Length < 4)
            {
                return;
            }

            int value = BitConverter.ToInt32(data);
            if (!Enum.IsDefined(typeof(MessageIdentifiers), value))
            {
                // this isn't a message we handle
                return;
            }

            ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>(data)[4..];
            MessageIdentifiers id = (MessageIdentifiers)value;

            if (id == MessageIdentifiers.PING)
            {
                // got ping, return pong
                GetDataChannel(contact).Send(BitConverter.GetBytes((int)MessageIdentifiers.PONG));

            }
            else if (id == MessageIdentifiers.PONG)
            {
                // got pong response -> ready
                HandlePongLike(contact);

            }
            else if (id == MessageIdentifiers.PAYLOAD_MESSAGE)
            {
                HandlePongLike(contact);
                HandlePayload(model, contact, payload);
            }
            else if (id == MessageIdentifiers.ANSWER)
            {
                // this shouldn't be needed
                HandlePongLike(contact);
                HandleAnswer(model, contact, payload);
            }
        }
        private void HandleAnswer(ConnectionModel model, Contact contact, ReadOnlySpan<byte> data)
        {

            // 4 (msg identifier) + 4 (answer) + 4 (topic name size) + (topic name size) (topic name) + 16 (message guid) + 4 (message length) + length (message)

            // msg identifier already handled

            if (data.Length < 4)
            {
                return;
            }

            int value = BitConverter.ToInt32(data);
            if (!Enum.IsDefined(typeof(MessageAnswerCode), value))
            {
                // this isn't a message we handle
                return;
            }

            MessageAnswerCode answerId = (MessageAnswerCode)value;
            data = data[4..];

            int topicNameLength = BitConverter.ToInt32(data);
            data = data[4..];

            if (data.Length < topicNameLength)
            {
                Debug.LogError($"[DataTransportManager] Received incomplete topic name. Expected size is {topicNameLength} bytes, got {data.Length}");
                return;
            }
            string topicName = Encoding.UTF8.GetString(data[..topicNameLength]);
            data = data[topicNameLength..];

            // read message  id
            if (data.Length < 16)
            {
                Debug.LogError($"[DataTransportManager] Received incomplete message id. Expected guid size is 16 bytes, got {data.Length}");
                return;
            }
            Guid messageId;
            try
            {
                messageId = new(data[..16]);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataTransportManager] Received erroneous message id. Parsing error: {e.Message}");
                return;
            }
            data = data[16..];

            // check if there is an answer request to avoid unnecessary work

            if (!answerHandlers.TryGetValue(messageId, out MessageAnswerData handlerData))
            {
                return;
            }

            // validate size
            if (data.Length < 4)
            {
                Debug.LogError($"[DataTransportManager] Received erroneous payload size. Expected payload size field with 4 bytes, got {data.Length}");
                return;
            }
            // move forward
            var payloadSize = BitConverter.ToInt32(data);

            data = data[4..];
            if (payloadSize != data.Length)
            {
                Debug.LogError($"[DataTransportManager] Received erroneous payload block. Expected payload size {payloadSize}[byte], got {data.Length}[byte]");

                return;

            }
            MessageData messageData = new(this, contact.Id, data.ToArray(), topicName, messageId);

            if (handlerData.handler(answerId, messageData))
            {
                answerHandlers.TryRemove(messageId, out MessageAnswerData _);
            }

        }
        private void HandlePayload(ConnectionModel model, Contact contact, ReadOnlySpan<byte> data)
        {

            // 4 (message identifier) + 4 (topic name size) + (topic name size) (message topic name) +  16 (message uuid) +  4 (payload size) + byteSize (data length)

            // read message id
            if (data.Length < 4)
            {
                Debug.LogError($"[DataTransportManager] Received incomplete topic name size. Expected size is 4 bytes, got {data.Length}");
                return;
            }
            int topicNameLength = BitConverter.ToInt32(data);
            data = data[4..];

            if (data.Length < topicNameLength)
            {
                Debug.LogError($"[DataTransportManager] Received incomplete topic name. Expected size is {topicNameLength} bytes, got {data.Length}");
                return;
            }
            string topicName = Encoding.UTF8.GetString(data[..topicNameLength]);
            data = data[topicNameLength..];

            if (data.Length < 16)
            {
                Debug.LogError($"[DataTransportManager] Received incomplete message id. Expected guid size is 16 bytes, got {data.Length}");
                return;
            }
            Guid messageId;
            try
            {
                messageId = new(data[..16]);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataTransportManager] Received erroneous message id. Parsing error: {e.Message}");
                return;
            }
            // left over data
            data = data[16..];
            // validate size
            if (data.Length < 4)
            {
                Debug.LogError($"[DataTransportManager] Received erroneous payload size. Expected payload size field with 4 bytes, got {data.Length}");
                return;
            }
            // move forward
            var payloadSize = BitConverter.ToInt32(data);

            data = data[4..];
            if (payloadSize != data.Length)
            {
                Debug.LogError($"[DataTransportManager] Received erroneous payload block. Expected payload size {payloadSize}[byte], got {data.Length}[byte]");

                return;

            }

            MessageData msgData = new(this, contact.Id, data.ToArray(), topicName, messageId);
            MessageAnswer answer = new(this, contact.Id, topicName, messageId);
            DispatchMessage(msgData, answer);
        }
        public void SendMessageAnswer(string contactId, MessageAnswerCode answer, string topicName, Guid messageId, byte[] data)
        {
            if (!dataChannels.TryGetValue(contactId, out DataChannelEntry entry))
            {
                return;
            }

            DataChannel channel = entry?.DataChannel;
            ConnectionState state = entry?.State ?? ConnectionState.INITIAL;

            // 4 (msg identifier) + 4 (answer) + 4 (topic name size) + (topic name size) (topic name) + 16 (message guid) + 4 (message length) + length (message)
            byte[] topicNameBytes = Encoding.UTF8.GetBytes(topicName);

            byte[] msgData = new byte[4 + 4 + (4 + topicNameBytes.Length) + 16 + (4 + data.Length)];
            int offset = 0;
            // msg identifier
            Buffer.BlockCopy(BitConverter.GetBytes((int)MessageIdentifiers.ANSWER), 0, msgData, offset, 4);
            offset += 4;

            // answer
            Buffer.BlockCopy(BitConverter.GetBytes((int)answer), 0, msgData, offset, 4);
            offset += 4;

            // topic name size
            Buffer.BlockCopy(BitConverter.GetBytes(topicNameBytes.Length), 0, msgData, offset, 4);
            offset += 4;
            // topic name
            Buffer.BlockCopy(topicNameBytes, 0, msgData, offset, topicNameBytes.Length);
            offset += topicNameBytes.Length;

            if (!messageId.TryWriteBytes(new Span<byte>(msgData)[offset..]))
            {
                Debug.LogError($"[DataTransportManager] Error trying to write message id to buffer: id = {messageId}");
                return;
            }
            offset += 16;

            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, msgData, offset, 4);
            offset += 4;

            data.CopyTo(new Span<byte>(msgData)[offset..]);

            if (channel == null || state != ConnectionState.CONNECTED)
            {
                MessageQueueElement e = new()
                {
                    contactId = contactId,
                    data = msgData,
                    sendCallback = null,
                    id = messageId,
                };

                messageQueueElements.AddOrUpdate(contactId, (key) =>
                {
                    // no key exists -> create new queue
                    ConcurrentQueue<MessageQueueElement> q = new();
                    q.Enqueue(e);
                    return q;
                }, (key, value) =>
                {
                    // key exists -> add to queue, if length constrain is satisfied
                    if (value.Count < MaxMessageQueueLength)
                    {
                        Debug.Log($"[DataTransportManager] Add data to queue with size {value.Count}");
                        value.Enqueue(e);
                    }
                    else
                    {
                        Debug.LogError("[DataTransportManager] Trying to send data without an active data channel and full queue");
                    }
                    return value;
                });
            }
            else
            {
                channel.Send(msgData);
            }
        }

        #endregion

        #region internal methods

        private static void DispatchMessage(MessageData data, MessageAnswer answer)
        {
            HandleMessage handler;

            allHandlers?.Invoke(data, answer);

            lock (messageHandlerLock)
            {
                if (!messageHandlers.TryGetValue(data.TopicName, out handler))
                {
                    return;
                }
            }

            handler?.Invoke(data, answer);

        }

        /// <summary>
        /// Keeps sending pings in 1 second intervals
        /// </summary>
        /// <param name="channel">Data channel to publish the ping messages on</param>
        /// <returns></returns>
        private IEnumerator TryPing(DataChannel channel)
        {
            while (true)
            {
                channel.Send(BitConverter.GetBytes((int)MessageIdentifiers.PING));
                yield return new WaitForSeconds(1.0f);
            }
        }

        private bool EnqueueMessage(string contactId, DataChannel channel, byte[] messageData, Guid messageId, Action<string> sendCallback)
        {
            // if the channel is currently null, add the channel to the queue
            if (channel == null)
            {
                if (!EnableMessageQueue)
                {
                    return false;
                }

                MessageQueueElement e = new()
                {
                    contactId = contactId,
                    data = messageData,
                    sendCallback = sendCallback,
                    id = messageId,
                };
                // enqueue to message queue

                bool sent = true;
                messageQueueElements.AddOrUpdate(contactId, (key) =>
                {
                    // no key exists -> create new queue
                    ConcurrentQueue<MessageQueueElement> q = new();
                    q.Enqueue(e);
                    return q;
                }, (key, value) =>
                {
                    // key exists -> add to queue, if length constrain is satisfied
                    if (value.Count < MaxMessageQueueLength)
                    {
                        Debug.Log($"[DataTransportManager] Add data to queue with size {value.Count}");
                        value.Enqueue(e);
                    }
                    else
                    {
                        Debug.LogError("[DataTransportManager] Trying to send data without an active data channel and full queue");
                        sent = false;
                    }
                    return value;
                });

                return sent;

            }
            else
            {
                // additional security -> data channel is there but not open anymore...
                try
                {
                    if (channel.ReadyState == Unity.WebRTC.RTCDataChannelState.Open)
                    {
                        channel.Send(messageData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    // it seems the data channel does not communicate being closed under some circumstances, for example if a client just closes
                    // it is probably the best to remove the channel from the list
                    // TODO check if newer versions fix this issue or if there is a better way
                    lock (dataChannelLock)
                    {
                        dataChannels.Remove(contactId);
                    }
                }
                sendCallback?.Invoke(contactId);

            }

            return true;

        }

        private void AddMessageAnswerHandler(string topicName, Guid messageId, MessageAnswerHandler answerHandler, float timeout)
        {
            // if there is a callback, add it, so answers can be processed
            if (answerHandler != null)
            {
                MessageAnswerData msgAnswer = new()
                {
                    handler = answerHandler,
                    timeout = timeout,
                    topic = topicName,
                };

                answerHandlers.TryAdd(messageId, msgAnswer);
            }

        }

        private byte[] EncodeMessageData(string topicName, ReadOnlySpan<byte> data, Guid messageID)
        {
            int byteSize = data.Length;

            byte[] topicNameData = Encoding.UTF8.GetBytes(topicName);

            // 4 (message identifier) + 4 (topic name size) + topic name size (message topic name) +  16 (message uuid) +  4 (payload size) + byteSize (data length)
            byte[] msgData = new byte[4 + (4 + topicNameData.Length) + 16 + (4 + byteSize)];

            int offset = 0;

            // message identifier
            Buffer.BlockCopy(BitConverter.GetBytes((int)MessageIdentifiers.PAYLOAD_MESSAGE), 0, msgData, offset, 4);
            offset += 4;

            // topic name size
            Buffer.BlockCopy(BitConverter.GetBytes(topicNameData.Length), 0, msgData, offset, 4);
            offset += 4;

            // topic name
            Buffer.BlockCopy(topicNameData, 0, msgData, offset, topicNameData.Length);
            offset += topicNameData.Length;

            if (!messageID.TryWriteBytes(new Span<byte>(msgData)[offset..]))
            {
                Debug.LogError($"[DataTransportManager] Error trying to write message id to buffer: id = {messageID}");
                return null;
            }
            offset += 16;

            Buffer.BlockCopy(BitConverter.GetBytes(byteSize), 0, msgData, offset, 4);
            offset += 4;

            data.CopyTo(new Span<byte>(msgData)[offset..]);

            return msgData;

        }

        private void StopPingCoroutine(Contact c)
        {
            if (tryPingCoroutines.TryRemove(c, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
            }
        }

        private void StartPingCoroutine(Contact c, DataChannel channel)
        {
            StopPingCoroutine(c);

            Coroutine coroutine = StartCoroutine(TryPing(channel));
            if (!tryPingCoroutines.TryAdd(c, coroutine))
            {
                // already exists
                StopCoroutine(coroutine);
            }
        }

        private List<DataChannelEntry> GetActiveDataChannels()
        {
            List<DataChannelEntry> entries = new();

            lock (dataChannelLock)
            {

                foreach (KeyValuePair<string, DataChannelEntry> pair in dataChannels)
                {
                    DataChannelEntry entry = pair.Value;

                    if (pair.Value.State != ConnectionState.CONNECTED)
                    {
                        continue;
                    }

                    entries.Add(new(entry));
                }
            }

            return entries;
        }

        #endregion
    }
} // end namespace Cortex