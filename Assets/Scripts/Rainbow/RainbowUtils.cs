using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rainbow;
using Rainbow.Model;

namespace Cortex
{
    /// <summary>
    /// Collection of helper methods to interact with rainbow and asynchronous calls
    /// </summary>
    public class RainbowUtils
    {
        /// <summary>
        /// Creates a task for retrieving the members of a bubble so it can be used in an async context
        /// </summary>
        /// <param name="app">The rainbow application</param>
        /// <param name="b">The bubble to query</param>
        /// <returns>A task that, when completed, contains all members of a bubble. If an error occurs, it will throw an exception</returns>
        public static Task<List<BubbleMember>> RetrieveBubbleMembers(Rainbow.Application app, Bubble b, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Bubbles bubbles = app.GetBubbles();

            if (b == null)
            {
                return Task.FromResult(new List<BubbleMember>());
            }

            bubbles.GetAllMembers(b.Id, WrapSdkResult(out Task<List<BubbleMember>> resultTask, cancellationToken));

            return resultTask;
        }

        /// <summary>
        /// Creates a task for retrieving a contact for an id so it can be used in an async context
        /// </summary>
        /// <param name="app">The rainbow application</param>
        /// <param name="contactId">The id of the contact</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task that, when completed, contains the queried contact. If an error occurs, it will throw an exception</returns>
        public static Task<Contact> RetrieveContact(Rainbow.Application app, string contactId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Try to retrieve the contact from the cache
            Contact c = app.GetContacts().GetContactFromContactId(contactId);

            TaskCompletionSource<Contact> promiseMember = new();
            using var reg = cancellationToken.Register(() =>
            {
                promiseMember.TrySetCanceled();
            });
            // contact not found in cache
            if (c == null)
            {
                // query from server
                app.GetContacts().GetContactFromContactIdFromServer(contactId, WrapSdkResult(out Task<Contact> resultTask, cancellationToken));
                return resultTask;
            }
            else
            {
                // contact found in cache -> directly set result
                return Task.FromResult(c);
            }
        }
        /// <summary>
        /// Creates a task for retrieving a contact for a jid so it can be used in an async context
        /// </summary>
        /// <param name="app">The rainbow application</param>
        /// <param name="contactJid">The jid of the contact</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task that, when completed, contains the queried contact. If an error occurs, it will throw an exception</returns>
        public static Task<Contact> RetrieveContactByJid(Rainbow.Application app, string contactJid, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Try to retrieve the contact from the cache
            Contact c = app.GetContacts().GetContactFromContactJid(contactJid);

            TaskCompletionSource<Contact> promiseMember = new();
            using var reg = cancellationToken.Register(() =>
            {
                promiseMember.TrySetCanceled();
            });
            // contact not found in cache
            if (c == null)
            {
                // query from server
                app.GetContacts().GetContactFromContactJidFromServer(contactJid, WrapSdkResult(out Task<Contact> resultTask, cancellationToken));
                return resultTask;
            }
            else
            {
                // contact found in cache -> directly set result
                return Task.FromResult(c);
            }
        }

        /// <summary>
        /// Generates a callback that wraps a rainbow SdkResult so that it can be used as a task
        /// </summary>
        /// <typeparam name="T">The result type</typeparam>
        /// <param name="task">A task for the SdkResult. Will contain the data of the result, if it was successful. If the cancellation token was activated, the task will be cancelled. In all other cases, the task will throw an exception.</param>
        /// <param name="cancellationToken">A cancellation token to request cancelling the current operation</param>
        /// <returns>A callback for wrapping SdkResults</returns>
        public static Action<SdkResult<T>> WrapSdkResult<T>(out Task<T> task, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<T> taskSource = new();

            using var reg = cancellationToken.Register(() =>
            {
                taskSource.TrySetCanceled();
            });
            task = taskSource.Task;

            return (result) =>
            {
                if (result.Result.Success)
                {
                    taskSource.TrySetResult(result.Data);
                }
                else
                {
                    if (result.Result.Type == SdkError.SdkErrorType.IncorrectUse)
                    {
                        taskSource.TrySetException(new Exception($"Incorrect use code {result.Result.IncorrectUseError.ErrorCode}: {result.Result.IncorrectUseError.ErrorMsg}"));
                    }
                    else if (result.Result.Type == SdkError.SdkErrorType.Exception)
                    {
                        taskSource.TrySetException(result.Result.ExceptionError);
                    }
                    else
                    {
                        taskSource.TrySetException(new Exception($"Unsuccessful request: HTTP status code {result.Result.HttpStatusCode}"));

                    }
                }
            };
        }

        /// <summary>
        /// Waits for all connected data channels to be active.
        /// This will ignore data channels with an error state.
        /// </summary>
        /// <param name="transportManager">The transport manager managing the activity state of the data channels</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task representing the act of waiting for the data channels</returns>
        public async static Task WaitForAllDataChannelsActive(DataTransportManager transportManager, CancellationToken cancellationToken = default)
        {
            // wait for open data channels to become active
            bool inactive = true;
            while (inactive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(60);
                var channelIds = transportManager.GetDataChannelContactIds();
                inactive = false;
                foreach (var id in channelIds)
                {
                    var state = transportManager.GetDataChannelState(id);
                    // check if the data channel is anything but connected or error
                    if (state != DataTransportManager.ConnectionState.CONNECTED && state != DataTransportManager.ConnectionState.ERROR)
                    {
                        inactive = true;
                        break;
                    }
                }
            }
        }
    }
} // end namespace Cortex