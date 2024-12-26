using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rainbow.Model;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// This class is a Key-Value store that is replicated over a Rainbow data channel.
    /// </summary>
    public class DistributedStorage : MonoBehaviour
    {
        // We implement a simple leader based system based on "Leader Election Algorithm using Suppression" from https://thesis.library.caltech.edu/3236/6/05ch5.pdf
        // This seems to be a variant of the bully algorithm
        // Since our network is small and possibly should not change too much, this should suffice
        // Other algorithms, such as Raft could be implemented, but since we have a lot of quickly changing messages and changing network topology, this variant seems easier

        // This implementation basically consists of two state machines: Leader determination and state synchronization
        // The leader is determined by the methods mentioned above, with the id augmented by the current maximum data "timestamp". That way, clients with a higher id will still yield to older clients with more up to date data.

        // Leader determination: The states and transitions are basically exactly as explained above
        // State synchronization:
        // When in Announce state: 
        //      Keep track of the storage generations of other clients. If a client has a lower generation: send the current one with the full state.
        //      If all clients are up to date -> send updates and update generation
        // When in Listen state:
        //      Start by sending out the current storage generation to notify the leader that this client exists
        //      If we receive a state from the leader -> update state and generation and acknowledge update
        //      If we receive an update from the leader -> update state and generation and acknowledge update
        //      We might switch to another state in case the leader doesn't send a heartbeat message

        #region unity inspector

        [Header("Options")]
        [Tooltip("If true, the storage will start up immediately, otherwise not")]
        [SerializeField]
        private bool Autostart = true;

        [Header("Network")]
        [Tooltip("Maximum timeout for suppression period [s]")]
        [Min(0.0f)]
        [SerializeField]
        private float TimeoutMaxSuppress = 0.4f;

        [Tooltip("Timeout for announcements [s]")]
        [Min(0.0f)]
        [SerializeField]
        private float TimeoutAnnounce = 0.1f;

        [Tooltip("Timeout for listen [s]")]
        [Min(0.0f)]
        [SerializeField]
        private float TimeoutListen = 1.0f;

        [Tooltip("Timeout for listen [s]")]
        [Min(0.0f)]
        [SerializeField]
        private float TimeoutSendCurrentGeneration = 1.0f;

        [Tooltip("Timeout for synchronization [s]")]
        [Min(0.0f)]
        [SerializeField]
        private float TimeoutSynchState = 0.05f;

        #endregion unity inspector

        #region events

        /// <summary>
        /// Type of change than can occur with Key-Value pairs
        /// </summary>
        public enum ChangeType
        {
            Set, Remove,
        }
        /// <summary>
        /// Handler for change events.
        /// </summary>
        /// <param name="state">The current local state of the Key-Value store</param>
        /// <param name="key">The key that was changed</param>
        /// <param name="value">The value that was changed. In case of removal, this is the value that was stored.</param>
        /// <param name="type">The type of change</param>
        public delegate void OnChangeHandler(IReadOnlyDictionary<string, string> state, string key, string value, ChangeType type);

        /// <summary>
        /// Event that is fired whenever a value of the local storage changes. If previous storage is overridden, the previous values get removed and then the new ones are added. In general, there is no guarantee of order for these operations. Updates from a single client are ordered with respect of each other, but only while they are being broadcast, after application this ordering is lost.
        /// </summary>
        public event OnChangeHandler OnChange;

        // connection states
        public enum State
        {
            Init, Announce, Listen, Suppress
        }

        /// <summary>
        /// Handler for state change events.
        /// </summary>
        /// <param name="from">The previous state</param>
        /// <param name="to">The new state</param>
        public delegate void OnStateChangeHandler(State from, State to);

        /// <summary>
        /// Will be called whenever the state of this storage client changes. 
        /// Can be used for logging or waiting for announce or listener states
        /// </summary>
        public event OnStateChangeHandler OnStateChange;

        #endregion events

        #region public
        /// <summary>
        /// A read-only view of the current internal Key-Value storage
        /// </summary>
        public IReadOnlyDictionary<string, string> Storage { get => internalStorage; }

        /// <summary>
        /// Starts the storage. This will first Stop to ensure previous values are removed.
        /// A storage will only start, if a Rainbow connection exists and the user is logged in
        /// </summary>
        /// <returns>True, if the storage was started, false otherwise. This happens, when the user is not logged in</returns>
        public bool Begin()
        {
            Stop();
            var model = ConnectionModel.Instance;
            if (model == null || model.CurrentUser == null)
            {
                return false;
            }

            actions.Enqueue(() =>
            {
                id = model.CurrentUser.Id;
                startTime = DateTime.UtcNow.Ticks;
                currentLeader = id;

                SetTimer(UnityEngine.Random.Range(0.0f, TimeoutMaxSuppress), OnTimerSuppress);
                ChangeState(State.Suppress);

            });
            return true;
        }

        /// <summary>
        /// Stops storage. Can be restarted by calling Begin()
        /// </summary>
        public void Stop()
        {
            actions.Enqueue(() =>
            {

                if (currentTimer != null)
                {
                    StopCoroutine(currentTimer);
                    currentTimer = null;
                }
                if (currentTimerAnnouncing != null)
                {
                    StopCoroutine(currentTimerAnnouncing);
                    currentTimerAnnouncing = null;
                }
                if (currentTimerListening != null)
                {
                    StopCoroutine(currentTimerListening);
                    currentTimerListening = null;
                }

                id = null;
                startTime = DateTime.MaxValue.Ticks;

                log.Clear();
                // signal all callbacks a failure
                foreach (var c in logCallbacks)
                {
                    c.result?.Invoke(false);
                }
                logCallbacks.Clear();
                waitForAck.Clear();
                accLength.Clear();

                List<string> keys = internalStorage.Keys.ToList();
                IReadOnlyDictionary<string, string> ro = internalStorage;
                foreach (var key in keys)
                {
                    internalStorage.Remove(key, out var value);
                    OnChange?.Invoke(ro, key, value, ChangeType.Remove);
                }

                storageGeneration = 0;
                currentLeader = id;

                ChangeState(State.Init);
            });

        }

        /// <summary>
        /// Calls the given callback for every value in the local storage.
        /// This is a convenience method to initialize a new subscriber with the same interface as with live-messages. This will call the handler with a Set operation for each Key-Value pair.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="handler">The handler to call for each Key-Value pair</param>
        public void CallbackCurrentState(OnChangeHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            IReadOnlyDictionary<string, string> ro = internalStorage;
            foreach (var (k, v) in internalStorage)
            {
                handler(ro, k, v, ChangeType.Set);
            }
        }

        /// <summary>
        /// Request the storage to set the given value. Optionally, you can specify whether the value should be overridden, if it is already there.
        /// Operations are only ordered with regards to a single client. Changes might not be immediate, as the storage leader handles state changes, which might occur on another client than this one.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="key">The key to set</param>
        /// <param name="value">The value to set for the key</param>
        /// <param name="overwrite">If true, this will override the currently set value, if it exists. If false, if there already is a value, it will not be changed</param>
        public void RequestSet(string key, string value, bool overwrite = true)
        {

            RequestAction(key, value, LogAction.Set, overwrite);
        }
        /// <summary>
        /// Request the storage to remove the given key. 
        /// Operations are only ordered with regards to a single client. Changes might not be immediate, as the storage leader handles state changes, which might occur on another client than this one.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="key">The key to remove</param>
        public void RequestRemove(string key)
        {
            RequestAction(key, "", LogAction.Remove, true);
        }

        /// <summary>
        /// Request the storage to remove the given key, if it equals the given value. If it does not or the key does not exists, nothing happens.
        /// Operations are only ordered with regards to a single client. Changes might not be immediate, as the storage leader handles state changes, which might occur on another client than this one.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <param name="value">The value to check for</param>
        public void RequestRemoveIf(string key, string value)
        {
            RequestAction(key, value, LogAction.Remove, false);
        }

        /// <summary>
        /// Request the storage to set the given value. Optionally, you can specify whether the value should be overridden, if it is already there.
        /// Operations are only ordered with regards to a single client. Changes might not be immediate, as the storage leader handles state changes, which might occur on another client than this one.
        /// The task signifies the application of the operation to the storage. If true, the value was applied, if false not. Any of these values signals the request being processed. In the case of connection issues, this task will time out.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="key">The key to set</param>
        /// <param name="value">The value to set for the key</param>
        /// <param name="overwrite">If true, this will override the currently set value, if it exists. If false, if there already is a value, it will not be changed</param>
        /// <param name="timeout">The timeout for getting an answer to this request in [s]</param>
        /// <param name="cancellationToken">A token to cancel the task. Important: This will not cancel the request, as it may already be on a different network client, but only the task waiting for an answer</param>
        /// <returns>A task representing the successful processing of the request</returns>
        public async Task<bool> RequestSetChecked(string key, string value, bool overwrite = true, float timeout = 10.0f, CancellationToken cancellationToken = default)
        {

            return await RequestActionChecked(key, value, LogAction.Set, overwrite, timeout, cancellationToken);
        }

        /// <summary>
        /// Request the storage to remove the given key. 
        /// Operations are only ordered with regards to a single client. Changes might not be immediate, as the storage leader handles state changes, which might occur on another client than this one.
        /// The task signifies the application of the operation to the storage. If true, the value was applied, if false not. Any of these values signals the request being processed. In the case of connection issues, this task will time out.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <param name="timeout">The timeout for getting an answer to this request in [s]</param>
        /// <param name="cancellationToken">A token to cancel the task. Important: This will not cancel the request, as it may already be on a different network client, but only the task waiting for an answer</param>
        /// <returns>A task representing the successful processing of the request</returns>
        public async Task<bool> RequestRemoveChecked(string key, float timeout = 10.0f, CancellationToken cancellationToken = default)
        {
            return await RequestActionChecked(key, "", LogAction.Remove, true, timeout, cancellationToken);
        }
        /// <summary>
        /// Request the storage to remove the given key, if it equals the given value. If it does not or the key does not exists, nothing happens.
        /// Operations are only ordered with regards to a single client. Changes might not be immediate, as the storage leader handles state changes, which might occur on another client than this one.
        /// The task signifies the application of the operation to the storage. If true, the value was applied, if false not. Any of these values signals the request being processed. In the case of connection issues, this task will time out.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <param name="value">The value to check for</param>
        /// <param name="timeout">The timeout for getting an answer to this request in [s]</param>
        /// <param name="cancellationToken">A token to cancel the task. Important: This will not cancel the request, as it may already be on a different network client, but only the task waiting for an answer</param>
        /// <returns>A task representing the successful processing of the request</returns>
        public async Task<bool> RequestRemoveIfChecked(string key, string value, float timeout = 10.0f, CancellationToken cancellationToken = default)
        {
            return await RequestActionChecked(key, value, LogAction.Remove, false, timeout, cancellationToken);
        }

        /// <summary>
        /// Checks if other active data channels agree on who the current distributed storage leader is
        /// </summary>
        /// <param name="timeout">The timeout for the requests</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True, if all clients agree on who the leader is, false if not</returns>
        public async Task<bool> CheckLeaderAgreement(float timeout = 5.0f, CancellationToken cancellationToken = default)
        {
            var dm = DataTransportManager.Instance;

            var contacts = dm.GetActiveDataChannelContactIds();

            // maybe add synch here, but as strings are immutable this shouldn't be an issue
            var selfLeader = currentLeader;
            List<Task<(MessageAnswerCode code, MessageData data)>> tasks = new();
            foreach (var c in contacts)
            {
                dm.SendTypedData(c, TOPIC_CHECK_LEADER, new MsgEmpty(), answerHandler: DataTransportManager.WrapAnswerHandler(out var task), timeout: timeout);
                tasks.Add(task);
            }

            try
            {
                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay((int)(timeout * 1000.0f), cancellationToken));
            }
            catch (Exception)
            {
                return false;
            }

            List<MessageData> answers = tasks.Where(t => t.IsCompletedSuccessfully).Select(t => t.Result).Where(result => result.code == MessageAnswerCode.Accept).Select(result => result.data).ToList();

            // check if all agree
            foreach (var d in answers)
            {

                var answer = d.ParseJson<MsgValue<string>>();
                if (answer.value != selfLeader)
                {
                    return false;
                }
            }

            return true;

        }

        #endregion public

        #region messages

        // message for leader announcement
        private struct MsgAnnounce
        {
            public long Generation;
            public long StartTime;
        }

        // message for sending out a complete state
        private struct MsgState
        {
            public long Generation;
            public string State;
        }

        // message for sending out state updates
        private struct MsgUpdate
        {
            public long GenerationStart;
            public long GenerationEnd;

            public string Updates;
        }

        // message for sending out a requested state change
        private struct MsgBroadcast
        {
            public string Key;
            public string Value;
            public LogAction Action;

            public bool overwrite;
        }
        #endregion messages

        #region internal

        private readonly ConcurrentDictionary<string, string> internalStorage = new();

        private State currentState = State.Init;

        private static string TOPIC_CHECK_LEADER = "ds/query_l";

        // sub state machine for listener
        private enum ListenerState
        {
            None, Init, Listening
        }

        private ListenerState listenerState = ListenerState.None;
        private enum LogAction
        {
            Set, Remove
        }
        private string id;
        private long startTime;
        private long storageGeneration = 0;

        private string currentLeader;
        private class LogEntry
        {
            public LogAction Action;
            public string key;
            public string value;
            public bool overwrite;
        }
        private readonly List<LogEntry> log = new();

        private class LogCallback
        {
            public bool overwrite;
            public Action<bool> result;
        }
        private readonly List<LogCallback> logCallbacks = new();

        // TODO buffer requests when not in Announce or Listen state

        private readonly HashSet<string> waitForAck = new();
        private readonly Dictionary<string, long> accLength = new();

        private readonly ConcurrentQueue<Action> actions = new();

        private Coroutine currentTimer;
        private Coroutine currentTimerListening;
        private Coroutine currentTimerAnnouncing;

        #endregion internal

        #region unity lifecycle
        void Start()
        {

            DataTransportManager.RegisterMessageHandler("ds/a", OnReceiveAnnounce);
            DataTransportManager.RegisterMessageHandler("ds/ack", OnReceiveListenerInit);
            DataTransportManager.RegisterMessageHandler("ds/up", OnReceiveMsgUpdate);
            DataTransportManager.RegisterMessageHandler("ds/state", OnReceiveMsgState);
            DataTransportManager.RegisterMessageHandler("ds/b", OnReceiveMsgBroadcast);
            DataTransportManager.RegisterMessageHandler(TOPIC_CHECK_LEADER, OnReceiveMsgCheckLeader);

            ConnectionModel.OnLogin += OnLogin;

            DataTransportManager.OnClose += OnDataChannelClose;

            startTime = DateTime.MaxValue.Ticks;
            if (Autostart)
            {
                var model = ConnectionModel.Instance;
                if (model != null && model.CurrentUser != null)
                {
                    Begin();
                }
            }
        }

        void OnDestroy()
        {
            DataTransportManager.RemoveMessageHandler("ds/a", OnReceiveAnnounce);
            DataTransportManager.RemoveMessageHandler("ds/ack", OnReceiveListenerInit);
            DataTransportManager.RemoveMessageHandler("ds/up", OnReceiveMsgUpdate);
            DataTransportManager.RemoveMessageHandler("ds/state", OnReceiveMsgState);
            DataTransportManager.RemoveMessageHandler("ds/b", OnReceiveMsgBroadcast);
            DataTransportManager.RemoveMessageHandler(TOPIC_CHECK_LEADER, OnReceiveMsgCheckLeader);

            ConnectionModel.OnLogin -= OnLogin;

            DataTransportManager.OnClose -= OnDataChannelClose;

        }

        private void OnReceiveMsgCheckLeader(MessageData data, MessageAnswer answer)
        {
            answer.Accept(new MsgValue<string>(currentLeader));
        }

        void Update()
        {
            while (actions.TryDequeue(out Action action))
            {
                action();
            }
        }

        #endregion unity lifecycle

        #region internal methods
        private void OnDataChannelClose(DataTransportManager manager, Contact contact)
        {
            waitForAck.Remove(contact.Id);
            accLength.Remove(contact.Id);
        }

        private void OnReceiveMsgBroadcast(MessageData data, MessageAnswer answer)
        {
            var msg = data.ParseJson<MsgBroadcast>();

            actions.Enqueue(() =>
            {
                // if not leader, ignore
                if (currentState != State.Announce)
                {
                    answer.Deny();
                    return;
                }

                log.Add(new LogEntry()
                {
                    Action = msg.Action,
                    key = msg.Key,
                    value = msg.Value,
                    overwrite = msg.overwrite,
                });

                logCallbacks.Add(new LogCallback()
                {
                    overwrite = msg.overwrite,
                    result = r => answer.Accept(new MsgValue<bool>(r)),
                });
            });
        }

        private void RequestAction(string key, string value, LogAction logAction, bool overwrite)
        {
            actions.Enqueue(() =>
           {
               //    Debug.Log($"[DistributedStorage] Request action: Key: {key}, value: {value}, action : {logAction}, overwrite: {overwrite}");

               if (currentState == State.Announce)
               {
                   // we are leader
                   log.Add(new LogEntry()
                   {
                       Action = logAction,
                       key = key,
                       value = value,
                       overwrite = overwrite,
                   });
                   logCallbacks.Add(new LogCallback()
                   {
                       overwrite = overwrite,
                       result = null,
                   });
               }
               else
               {

                   if (currentLeader == null)
                   {
                       return;
                   }
                   MsgBroadcast msg = new()
                   {
                       Key = key,
                       Value = value,
                       Action = logAction,
                       overwrite = overwrite
                   };

                   DataTransportManager.Instance.SendTypedData(currentLeader, "ds/b", msg);
               }
           });
        }

        private async Task<bool> RequestActionChecked(string key, string value, LogAction logAction, bool overwrite, float timeout, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<bool> returnTask = new();

            using var registration = cancellationToken.Register(() =>
            {
                // this callback will be executed when token is cancelled
                returnTask.TrySetCanceled(cancellationToken);
            });
            actions.Enqueue(() =>
                {
                    if (currentState == State.Announce)
                    {
                        // we are leader
                        log.Add(new LogEntry()
                        {
                            Action = logAction,
                            key = key,
                            value = value,
                            overwrite = overwrite,

                        });
                        logCallbacks.Add(new LogCallback()
                        {
                            overwrite = overwrite,
                            result = r => returnTask.TrySetResult(r),
                        });
                    }
                    else if (currentState == State.Listen)
                    {

                        if (currentLeader == null)
                        {
                            returnTask.TrySetResult(false);
                            return;
                        }
                        MsgBroadcast msg = new()
                        {
                            Key = key,
                            Value = value,
                            Action = logAction,
                            overwrite = overwrite
                        };

                        DataTransportManager.Instance.SendTypedData(currentLeader, "ds/b", msg, timeout: timeout, answerHandler: (code, data) =>
                        {
                            if (code == MessageAnswerCode.Accept)
                            {
                                returnTask.TrySetResult(data.ParseJson<MsgValue<bool>>().value);
                            }
                            else
                            {
                                returnTask.TrySetResult(false);
                            }
                            return true;
                        });
                    }
                    else
                    {
                        returnTask.TrySetResult(false);
                    }
                });

            return await returnTask.Task;
        }

        private void OnReceiveMsgState(MessageData data, MessageAnswer answer)
        {
            var msg = data.ParseJson<MsgState>();

            actions.Enqueue(() =>
            {
                if (currentState != State.Listen)
                {
                    answer.Deny();
                    return;
                }
                if (data.ContactId != currentLeader)
                {
                    answer.Deny();
                    return;
                }

                // this is probably not the most efficient version, but it keeps the interface consistent with updates

                Dictionary<string, string> newState = new(JsonConvert.DeserializeObject<Dictionary<string, string>>(msg.State));

                // first send out removal of all old keys to clean up
                List<(string key, string value)> removed = new();
                foreach (var (k, v) in internalStorage)
                {
                    if (!newState.ContainsKey(k))
                    {
                        removed.Add((k, v));
                    }
                }
                IReadOnlyDictionary<string, string> readOnly = internalStorage;
                foreach (var (key, value) in removed)
                {
                    internalStorage.Remove(key, out var val);
                    OnChange?.Invoke(readOnly, key, value, ChangeType.Remove);
                }
                // all other values are set/updated in the new storage

                internalStorage.Clear();

                foreach (var (key, value) in newState)
                {
                    internalStorage.TryAdd(key, value);
                    OnChange?.Invoke(readOnly, key, value, ChangeType.Set);
                }

                storageGeneration = msg.Generation;

                MsgValue<long> msgAcc = new(storageGeneration);
                DataTransportManager.Instance.SendTypedData(data.ContactId, "ds/ack", msgAcc);

                // we can stop sending initial updates
                listenerState = ListenerState.Listening;
            });
        }

        private void OnReceiveMsgUpdate(MessageData data, MessageAnswer answer)
        {
            var msg = data.ParseJson<MsgUpdate>();

            actions.Enqueue(() =>
            {
                if (currentState != State.Listen)
                {
                    answer.Deny();
                    return;
                }

                if (data.ContactId != currentLeader)
                {
                    answer.Deny();
                    return;
                }

                List<LogEntry> logEntries = JsonConvert.DeserializeObject<List<LogEntry>>(msg.Updates);

                foreach (var e in logEntries)
                {
                    ApplyLogEntry(e);
                }
                storageGeneration = msg.GenerationEnd;

                MsgValue<long> msgAcc = new(storageGeneration);

                // answer.Accept(new MsgValue<long>(storageGeneration));
                DataTransportManager.Instance.SendTypedData(data.ContactId, "ds/ack", msgAcc);

                // we can stop sending initial updates
                listenerState = ListenerState.Listening;
            });
        }

        private bool ApplyLogEntry(LogEntry e)
        {
            // This checks the type of action to be taken and then applies values and calls callbacks
            if (e.Action == LogAction.Set)
            {
                // just set the value if overwrite is set, otherwise try adding it
                if (e.overwrite)
                {
                    internalStorage[e.key] = e.value;
                    OnChange?.Invoke(internalStorage, e.key, e.value, ChangeType.Set);
                    return true;
                }
                else
                {
                    bool result = internalStorage.TryAdd(e.key, e.value);

                    if (result)
                    {
                        OnChange?.Invoke(internalStorage, e.key, e.value, ChangeType.Set);
                    }

                    return result;
                }
            }
            else if (e.Action == LogAction.Remove)
            {
                // overwrite for removes signifies not checking the current value for equality with the given one
                if (e.overwrite)
                {
                    bool result = internalStorage.Remove(e.key, out string val);

                    if (result)
                    {
                        OnChange(internalStorage, e.key, val, ChangeType.Remove);

                    }

                    return result;
                }
                else
                {
                    bool result = internalStorage.TryGetValue(e.key, out string val);
                    if (!result)
                    {
                        // value does not exist
                        return false;
                    }
                    if (val == e.value)
                    {
                        // we can remove, values are equal
                        if (internalStorage.Remove(e.key, out var _))
                        {
                            OnChange(internalStorage, e.key, val, ChangeType.Remove);
                            return true;
                        }
                    }
                    return false;

                }

            }

            return false;
        }

        private void OnReceiveListenerInit(MessageData data, MessageAnswer answer)
        {
            // got a message with the current generation of a client
            var msg = data.ParseJson<MsgValue<long>>();

            actions.Enqueue(() =>
            {
                if (currentState != State.Announce)
                {
                    // we are not a leader
                    return;
                }

                accLength[data.ContactId] = msg.value;
                waitForAck.Remove(data.ContactId);
                answer.Accept();
            });

        }

        private void OnLogin(ConnectionModel model)
        {
            actions.Enqueue(() =>
            {
                if (Autostart && currentState == State.Init)
                {
                    Begin();
                }
            });
        }

        private void OnReceiveAnnounce(MessageData data, MessageAnswer answer)
        {
            string contactId = data.ContactId;
            var msg = data.ParseJson<MsgAnnounce>();
            actions.Enqueue(() =>
            {
                // establish order between clients: generation -> starttime -> contact id
                int compare = msg.Generation.CompareTo(storageGeneration);
                if (compare == 0)
                {
                    compare = startTime.CompareTo(msg.StartTime);
                }
                if (compare == 0)
                {
                    compare = contactId.CompareTo(currentLeader);
                }

                if (currentState == State.Announce)
                {
                    if (compare > 0)
                    {
                        // other instance becomes leader
                        currentLeader = contactId;
                        SetTimer(TimeoutListen, OnTimerListen);
                        ChangeState(State.Listen);
                    }

                }
                else if (currentState == State.Listen)
                {
                    if (compare == 0)
                    {
                        // same leader as before, reset listen timer
                        SetTimer(TimeoutListen, OnTimerListen);
                    }
                    else if (compare > 0)
                    {
                        // leader changed
                        currentLeader = contactId;
                        SetTimer(TimeoutListen, OnTimerListen);
                    }
                }
                else if (currentState == State.Suppress)
                {
                    if (compare > 0)
                    {
                        // got leader msg
                        currentLeader = contactId;
                        SetTimer(TimeoutListen, OnTimerListen);
                        ChangeState(State.Listen);
                    }
                }
            });
        }

        void SetTimer(float t, Action action)
        {
            if (currentTimer != null)
            {
                StopCoroutine(currentTimer);
                currentTimer = null;
            }
            currentTimer = StartCoroutine(StartTimer(t, action));
        }

        IEnumerator StartTimer(float t, Action action)
        {
            yield return new WaitForSeconds(t);

            actions.Enqueue(action);
        }

        void SetTimerListening(float t, Action action)
        {
            if (currentTimerListening != null)
            {
                StopCoroutine(currentTimerListening);
                currentTimerListening = null;
            }
            currentTimerListening = StartCoroutine(StartTimerListening(t, action));
        }

        IEnumerator StartTimerListening(float t, Action action)
        {
            yield return new WaitForSeconds(t);

            actions.Enqueue(action);
        }

        void SendAnnounce()
        {
            DataTransportManager.Instance.BroadcastTypedData("ds/a", new MsgAnnounce() { Generation = storageGeneration, StartTime = startTime });

        }

        void OnTimerAnnounce()
        {
            if (currentState != State.Announce)
            {
                // should not happen
                return;
            }

            SendAnnounce();
            SetTimer(TimeoutAnnounce, OnTimerAnnounce);
        }
        private void OnTimerListen()
        {
            if (currentState != State.Listen)
            {
                // should not happen
                return;
            }
            // listen timer expired without an announcement -> try again
            currentLeader = id;
            SetTimer(UnityEngine.Random.Range(0.0f, TimeoutMaxSuppress), OnTimerSuppress);
            ChangeState(State.Suppress);
        }

        private void OnSynchState()
        {

            if (currentState != State.Announce)
            {
                return;
            }

            // gather all registered clients
            List<string> toUpdate = new();
            List<string> keys = new(accLength.Keys);

            foreach (var kv in accLength)
            {
                if (kv.Value != storageGeneration)
                {
                    toUpdate.Add(kv.Key);
                }
            }

            if (toUpdate.Count() == 0 && log.Any())
            {

                JsonSerializerSettings settings = new()
                {
                    Formatting = Formatting.None
                };

                MsgUpdate msg = new()
                {
                    GenerationStart = storageGeneration,
                    GenerationEnd = storageGeneration + log.Count,
                    Updates = JsonConvert.SerializeObject(log, settings)
                };
                storageGeneration += log.Count;

                // handle callbacks as well
                foreach (var (e, c) in log.Zip(logCallbacks, (e, c) => (e, c)))
                {
                    var success = ApplyLogEntry(e);
                    c.result?.Invoke(success);
                }
                log.Clear();
                logCallbacks.Clear();

                foreach (var c in keys)
                {
                    DataTransportManager.Instance.SendTypedData(c, "ds/up", msg);
                    waitForAck.Add(c);

                }

            }
            else
            {

                JsonSerializerSettings settings = new()
                {
                    Formatting = Formatting.None
                };
                MsgState msg = new()
                {
                    Generation = storageGeneration,
                    State = JsonConvert.SerializeObject(internalStorage, settings)
                };

                // update all first
                foreach (var id in toUpdate)
                {
                    if (waitForAck.Contains(id))
                    {
                        continue;
                    }
                    DataTransportManager.Instance.SendTypedData(id, "ds/state", msg);
                    waitForAck.Add(id);

                }
            }

            if (currentTimerAnnouncing != null)
            {
                StopCoroutine(currentTimerAnnouncing);
                currentTimerAnnouncing = null;
            }
            currentTimerAnnouncing = StartCoroutine(StartTimer(TimeoutSynchState, OnSynchState));

        }

        private async void OnTimerListening()
        {
            // we ask for the current state after a timeout
            if (listenerState != ListenerState.Init)
            {
                return;
            }

            MsgValue<long> msg = new()
            {
                value = storageGeneration
            };
            DataTransportManager.Instance.SendTypedData(currentLeader, "ds/ack", msg, answerHandler: DataTransportManager.WrapAnswerHandler(out var answer));
            var (code, data) = await answer;
            if (code == MessageAnswerCode.Accept)
            {
                actions.Enqueue(() =>
                {
                    if (listenerState != ListenerState.Init)
                    {
                        listenerState = ListenerState.Listening;
                    }
                });
            }
            SetTimerListening(TimeoutSendCurrentGeneration, OnTimerListening);
        }

        void ChangeState(State newState)
        {
            State from = currentState;
            currentState = newState;
            if (from != newState)
            {

                // Debug.Log($"[DistributedStorage] Changed state: {from} -> {to}");
                if (from == State.Listen)
                {
                    listenerState = ListenerState.None;

                }

                if (currentState == State.Listen)
                {
                    SetTimerListening(TimeoutSendCurrentGeneration, OnTimerListening);
                    listenerState = ListenerState.Init;

                }
                else if (currentState == State.Announce)
                {
                    accLength.Clear();
                    waitForAck.Clear();
                    if (currentTimerAnnouncing != null)
                    {
                        StopCoroutine(currentTimerAnnouncing);
                        currentTimerAnnouncing = null;
                    }
                    currentTimerAnnouncing = StartCoroutine(StartTimer(TimeoutSynchState, OnSynchState));
                }

                OnStateChange?.Invoke(from, currentState);
            }
        }

        void OnTimerSuppress()
        {
            if (currentState != State.Suppress)
            {
                // should not happen
                return;
            }

            SendAnnounce();
            SetTimer(TimeoutAnnounce, OnTimerAnnounce);
            ChangeState(State.Announce);
        }

        #endregion internal methods

    }
} // end namespace Cortex