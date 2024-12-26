using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rainbow;
using Rainbow.Model;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Helper class to request the connection model to open a data channel for any contact that joins a call
    /// </summary>
    public class DataChannelRequester : MonoBehaviour
    {
        [SerializeField]
        private CallState CallState;

        /// <summary>
        /// States for a data channel request
        /// </summary>
        public enum RequestState
        {
            /// <summary>
            /// The data channel was requested
            /// </summary>
            Requested,
            /// <summary>
            /// The data channel has been opened
            /// </summary>
            Opened,
            /// <summary>
            /// The data channel was not opened due to an error
            /// </summary>
            Error
        }
        /// <summary>
        /// Handler for OnRequestChange events. This will be called after setting the state in the current requests.
        /// Note, that after the request finished, either by the channel being opened or not, the request will be removed from the current requests
        /// </summary>
        /// <param name="contactId">The id of the contact for which a request is being processed</param>
        /// <param name="state">The state of the request</param>
        public delegate void RequestStateChangeHandler(string contactId, RequestState state);

        public event RequestStateChangeHandler OnRequestStateChange;

        /// <summary>
        /// A concurrent view of the current requests
        /// </summary>
        public IReadOnlyDictionary<string, RequestState> CurrentRequests
        {
            get => requests;
        }
        private readonly ConcurrentDictionary<string, RequestState> requests = new();

        public async Task WaitForRequestsToFinish(float timeout = 10.0f, CancellationToken cancellationToken = default)
        {
            var waitTask = Task.Run(async () =>
            {
                while (CurrentRequests.Count > 0)
                {
                    await Task.Delay(30);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay((int)(timeout * 1000.0f))))
            {
                throw new TimeoutException();
            }
        }
        void Start()
        {
            CallState.OnContactChanged += OnContactChanged;
        }

        private void OnContactChanged(Contact contact, CallState.ContactChangeType type)
        {
            // It seems that you can't request a data channel at the same time from two sides...
            // also the SimpleDataChannelService does not seem to be thread-safe

            // since we deal with multiple users without clear initiators (in a bubble), we use the convention: 
            // the contact with the lesser id establishes a data channel with the ones that have a larger one
            if (CallState.UserContact.Id.CompareTo(contact.Id) < 0)
            {
                if (type == CallState.ContactChangeType.Added)
                {
                    requests[contact.Id] = RequestState.Requested;
                    OnRequestStateChange?.Invoke(contact.Id, RequestState.Requested);

                    ConnectionModel.Instance.RequestDataChannel(contact, (success, contact, channel) =>
                    {
                        if (success)
                        {
                            requests[contact.Id] = RequestState.Opened;
                            OnRequestStateChange?.Invoke(contact.Id, RequestState.Opened);
                        }
                        else
                        {
                            requests[contact.Id] = RequestState.Error;
                            OnRequestStateChange?.Invoke(contact.Id, RequestState.Error);

                        }

                        // finished request
                        requests.Remove(contact.Id, out _);

                    });
                }
                else if (type == CallState.ContactChangeType.Removed)
                {
                    // Debug.Log($"[DataChannelRequester] Stopping channel for {Util.GetContactDisplayName(contact)}");

                    // TODO re-enable when closing works correctly
                    // ConnectionModel.Instance.RequestCloseDataChannel(contact);
                }
            }
        }

        void OnDestroy()
        {
            CallState.OnContactChanged -= OnContactChanged;
        }

    }
} // end namespace Cortex
