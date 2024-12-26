using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cortex;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// This is a helper class to send data larger than the usual limit of WebRTC messages. 
    /// The general usage for client A requesting client B to send a large file is as follows:
    /// Client A: Send message to data request endpoint
    /// Client B: Receive message. Create a network file with LargerFileNetworkTransporter. Send back the generated file id as an answer or new message to client A
    /// Client A: Receive the file id. RequestNetworkFile with the id.
    /// Client B: RemoveNetworkFile
    /// </summary>
    public class LargerFileNetworkTransporter : MonoBehaviour
    {

        #region editor

        [Header("Network")]
        [SerializeField]
        [Range(1000, 32000)]
        private int ChunkSize = 16000;

        [SerializeField]
        [Range(1, 20)]
        private int MaxGuidGenerateRetries = 10;

        #endregion // editor

        #region public interface

        /// <summary>
        /// Callback that is called whenever the registered file was downloaded.
        /// This can be used to safely remove a file after a download was finished by one or any number of contacts.
        /// Note, that this will only be called if a sending operation was started to begin with.
        /// </summary>
        /// <param name="transporter">The file transporter</param>
        /// <param name="fileId">The id of the file that finished sending</param>
        /// <param name="contactId">The id of the contact the file was sent to</param>
        /// <param name="successful">True, if the whole file was sent, false if it was cancelled</param>
        public delegate void CallbackFileSent(LargerFileNetworkTransporter transporter, string fileId, string contactId, bool successful);

        /// <summary>
        /// Create a network file that can be queried for using the returned file id.
        /// </summary>
        /// <param name="data">The data to be made available</param>
        /// <param name="contactId">The contact that is allowed to query the data. If null, every contact can request the file</param>
        /// <param name="callbackFileSent">Will be called, if the data transfer finished after being initiated. This happens, when either the transfer is complete, or the transfer was cancelled in the middle</param>
        /// <returns>The generated file id</returns>
        public string CreateNetworkFile(byte[] data, string contactId = null, CallbackFileSent callbackFileSent = null)
        {
            Predicate<string> verify;
            if (string.IsNullOrEmpty(contactId))
            {
                verify = (s) => true;
            }
            else
            {
                verify = (s) => contactId == s;
            }
            return CreateNetworkFile(data, verify, callbackFileSent);
        }

        /// <summary>
        /// Create a network file that can be queried for using the returned file id.
        /// </summary>
        /// <param name="data">The data to be made available</param>
        /// <param name="verifyUser">A predicate to check, whether a given contact is allowed to request the file</param>
        /// <param name="callbackFileSent">Will be called, if the data transfer finished after being initiated. This happens, when either the transfer is complete, or the transfer was cancelled in the middle</param>
        /// <returns>The generated file id</returns>
        public string CreateNetworkFile(byte[] data, Predicate<string> verifyUser, CallbackFileSent callbackFileSent = null)
        {
            int attempts = 0;

            // try to get an id
            // this should never happen in practice since GUIDs should be virtually collision-free
            do
            {
                var guid = Guid.NewGuid().ToString();
                attempts++;
                lock (outLock)
                {
                    if (currentOutgoingTransfers.ContainsKey(guid))
                    {
                        continue;
                    }

                    // found valid id -> create entry
                    OutgoingEntry entry = new()
                    {
                        Verifier = verifyUser,
                        ChunkSize = ChunkSize,
                        Data = data,
                        Callback = callbackFileSent,
                    };
                    currentOutgoingTransfers.Add(guid, entry);
                    return guid;

                }

            } while (attempts <= MaxGuidGenerateRetries);

            return null;
        }

        /// <summary>
        /// Remove a network file if it exists
        /// </summary>
        /// <param name="fileId">The id of the network file</param>
        public void RemoveNetworkFile(string fileId)
        {
            lock (outLock)
            {
                currentOutgoingTransfers.Remove(fileId);
            }
        }

        /// <summary>
        /// Request the transfer of a network file from a contact
        /// </summary>
        /// <param name="contactId">The contact to retrieve the file from</param>
        /// <param name="fileId">The id of the network file</param>
        /// <param name="messageAnswerTimeout">How long this client should wait for a network answer before considering the other side unavailable</param>
        /// <param name="cancellationToken">Token to cancel the operation at any time</param>
        /// <returns>The requested data or null if it does not exist</returns>
        /// <exception cref="TimeoutException">Thrown if the messageAnswerTimeout was exceeded</exception>
        public async Task<byte[]> RequestNetworkFile(string contactId, string fileId, float messageAnswerTimeout = 30.0f, CancellationToken cancellationToken = default)
        {
            // details about the procedure can be found below
            DateTime start = DateTime.UtcNow;

            cancellationToken.ThrowIfCancellationRequested();

            DataTransportManager.Instance.SendTypedData(contactId, TOPIC_REQUEST_FILE_TRANSFER, new MsgValue<string>(fileId), DataTransportManager.WrapAnswerHandler(out var requestStartTask, cancellationToken), timeout: messageAnswerTimeout);

            var (codeRequest, answerRequest) = await requestStartTask;

            if (codeRequest == MessageAnswerCode.Deny)
            {
                return null;
            }
            else if (codeRequest == MessageAnswerCode.NoResponse)
            {
                throw new TimeoutException($"Initiate network file transfer exceeded timeout: {messageAnswerTimeout}s");
            }

            // before we continue, check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // information about the sending operation
            var msgTransferInfo = answerRequest.ParseJson<MsgTransferInfo>();

            TaskCompletionSource<bool> taskTransferFinished;
            // insert entry into incoming
            lock (inLock)
            {
                IncomingEntry entry = new()
                {
                    contactId = contactId,
                    data = new byte[msgTransferInfo.totalSize],
                    offset = 0,
                };

                currentIncomingTransfers.Add(fileId, entry);
                taskTransferFinished = entry.taskSource;
            }

            // enable cancellation

            using var reg = cancellationToken.Register(() =>
            {
                taskTransferFinished.TrySetCanceled(cancellationToken);
            });

            DataTransportManager.Instance.SendTypedData(contactId, TOPIC_START_TRANSFER, new MsgValue<string>(fileId), DataTransportManager.WrapAnswerHandler(out var startTransferTask, cancellationToken), timeout: messageAnswerTimeout);

            var (codeStart, answerStart) = await startTransferTask;

            if (codeRequest == MessageAnswerCode.Deny)
            {
                return null;
            }
            else if (codeRequest == MessageAnswerCode.NoResponse)
            {
                throw new TimeoutException($"Initiate network file transfer start exceeded timeout: {messageAnswerTimeout}s");
            }

            try
            {
                await taskTransferFinished.Task;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {

                // clean up in case of error
                lock (inLock)
                {
                    currentIncomingTransfers.Remove(fileId);
                }
                // send cancellation info
                DataTransportManager.Instance.SendTypedData(contactId, TOPIC_CANCEL_TRANSFER, new MsgValue<string>(fileId));
                cancellationToken.ThrowIfCancellationRequested();
            }

            Debug.Log($"[LargerFileNetworkTransporter] Finished transfer of {fileId}");

            // task has finished without exception
            lock (inLock)
            {
                currentIncomingTransfers.Remove(fileId, out IncomingEntry entry);
                return entry.data;
            }

        }

        #endregion // public interface

        #region private state and structures

        // Store information about data streaming in
        private class IncomingEntry
        {
            public string contactId;
            public byte[] data;
            public int offset;

            public TaskCompletionSource<bool> taskSource = new();
            // lock data just in case data comes in faster than we process, which shouldn't be the case, but we try to make sure
            public readonly object dataLock = new();
        }
        private readonly Dictionary<string, IncomingEntry> currentIncomingTransfers = new();
        private readonly object inLock = new();

        // store information about a network file that can be sent out
        private class OutgoingEntry
        {
            public Predicate<string> Verifier;
            public int ChunkSize;
            public byte[] Data;
            public CallbackFileSent Callback;
        }
        private readonly Dictionary<string, OutgoingEntry> currentOutgoingTransfers = new();
        // used to cancel ongoing transfer operations
        // key is fileId+contactId
        private readonly ConcurrentDictionary<string, CancellationTokenSource> cancelTokens = new();

        private readonly object outLock = new();

        [Serializable]
        private struct MsgTransferInfo
        {
            public int totalSize;
            public int chunkSize;
        }

        // we use "private" topics to pass around data
        private static readonly string TOPIC_REQUEST_FILE_TRANSFER = "lft/request_transfer";
        private static readonly string TOPIC_START_TRANSFER = "lft/start_transfer";
        private static readonly string TOPIC_CANCEL_TRANSFER = "lft/cancel_transfer";
        private static readonly string TOPIC_DATA_PACKAGE = "lft/data";

        #endregion // private state and structures

        #region unity-lifecycle

        void Start()
        {
            DataTransportManager.RegisterMessageHandler(TOPIC_REQUEST_FILE_TRANSFER, OnRequestFileTransfer);
            DataTransportManager.RegisterMessageHandler(TOPIC_START_TRANSFER, OnStartTransfer);
            DataTransportManager.RegisterMessageHandler(TOPIC_CANCEL_TRANSFER, OnCancelTransfer);
            DataTransportManager.RegisterMessageHandler(TOPIC_DATA_PACKAGE, OnData);
        }
        void OnDestroy()
        {
            DataTransportManager.RemoveMessageHandler(TOPIC_REQUEST_FILE_TRANSFER, OnRequestFileTransfer);
            DataTransportManager.RemoveMessageHandler(TOPIC_START_TRANSFER, OnStartTransfer);
            DataTransportManager.RemoveMessageHandler(TOPIC_CANCEL_TRANSFER, OnCancelTransfer);
            DataTransportManager.RemoveMessageHandler(TOPIC_DATA_PACKAGE, OnData);

        }
        #endregion // unity-lifecycle

        #region internal methods

        // **************** DETAILS **************
        // A basic overview of how the transfer operation works
        // Before a transfer can be made, client A will need to register a network file and send client B the file id.
        // Both client's LargerFileNetworkTransporter will then communicate in the following way
        // B (RequestNetworkFile) -> A: Send fileId to TOPIC_REQUEST_FILE_TRANSFER
        // A (OnRequestFileTransfer) -> B: If the file isn't available or the contact is not allowed to, deny the message. Otherwise accept with a MsgTransferInfo to signal the size of the data and messages
        // B (RequestNetworkFile, after A's answer) -> A: Prepare input buffer with size. Send TOPIC_START_TRANSFER with fileId
        // A (OnStartTransfer) n times -> B: Send data chunk on TOPIC_DATA_PACKAGE and wait for answer after every chunk. Denying an answer stops the sending
        // B (OnData) n times -> A: Write data into buffer. If there are wrong ids or something similar, the message is denied, otherwise accepted. If the data filling is completed, the task for the data is signalled.
        // B may cancel the request at any time, which sends a TOPIC_CANCEL_TRANSFER message that stops the package being sent

        private void OnData(MessageData data, MessageAnswer answer)
        {
            // decipher data

            var bytes = data.Data;
            ReadOnlySpan<byte> readData = bytes;
            // [4 (length id) + length id (id) + chunk size]
            if (readData.Length < 4)
            {
                Debug.Log($"[LargerFileNetworkTransporter] Received invalid message");
                answer.Deny();
                return;
            }
            int idLength = BitConverter.ToInt32(readData);
            readData = readData[4..];

            if (readData.Length < idLength)
            {
                Debug.LogError($"[LargerFileNetworkTransporter] Received incomplete topic name. Expected size is {idLength} bytes, got {readData.Length}");
                answer.Deny();
                return;
            }

            string fileId = Encoding.UTF8.GetString(readData[..idLength]);
            readData = readData[idLength..];

            Debug.Log($"[LargerFileNetworkTransporter] Received data for file {fileId} of total size {bytes.Length} with id length {idLength}");

            IncomingEntry entry;
            lock (inLock)
            {
                // file does not exist (anymore)
                if (!currentIncomingTransfers.TryGetValue(fileId, out entry))
                {
                    Debug.Log($"[LargerFileNetworkTransporter] file {fileId} does not exist");

                    answer.Deny();
                    return;
                }

                // got data for the file from the wrong contact...
                if (entry.contactId != data.ContactId)
                {
                    Debug.Log($"[LargerFileNetworkTransporter] Invalid contact: {entry.contactId} != {data.ContactId}");

                    answer.Deny();
                    return;
                }
            }

            lock (entry.dataLock)
            {

                int byteOffset = entry.offset;
                int byteOffsetEnd = byteOffset + readData.Length;
                byteOffsetEnd = Math.Min(byteOffsetEnd, entry.data.Length);

                int counter = 0;

                for (int i = byteOffset; i < byteOffsetEnd; i++)
                {
                    entry.data[i] = readData[counter];
                    counter++;
                }

                entry.offset = byteOffsetEnd;

                Debug.Log($"[LargerFileNetworkTransporter] Wrote chunk of {fileId} into [{byteOffset}, {byteOffsetEnd})");

                // signal that we got the chunk

                answer.Accept();

                // finished
                if (entry.offset == entry.data.Length)
                {
                    Debug.Log($"[LargerFileNetworkTransporter] Finished file transfer of {fileId}");

                    entry.taskSource.TrySetResult(true);
                }
            }

        }

        private void OnCancelTransfer(MessageData data, MessageAnswer answer)
        {
            var msg = data.ParseJson<MsgValue<string>>();
            string fileId = msg.value;
            string tokenName = fileId + data.ContactId;

            if (cancelTokens.TryGetValue(tokenName, out CancellationTokenSource source))
            {
                Debug.Log($"[LargerFileNetworkTransporter] Request cancellation of: {fileId}");
                source.Cancel();
            }
        }

        private async void OnStartTransfer(MessageData data, MessageAnswer answer)
        {

            var msg = data.ParseJson<MsgValue<string>>();
            var fileId = msg.value;

            byte[] fileBytes;
            int localChunkSize;
            CallbackFileSent callback;
            // get data for sending, afterwards it is safe to delete the file
            lock (outLock)
            {
                // no file with that id
                if (!currentOutgoingTransfers.TryGetValue(fileId, out OutgoingEntry entry))
                {
                    answer.Deny();
                    return;
                }

                // file was not made public for the given contact
                if (!entry.Verifier(data.ContactId))
                {
                    answer.Deny();
                    return;
                }

                fileBytes = entry.Data;
                localChunkSize = entry.ChunkSize;
                callback = entry.Callback;
            }

            answer.Accept();

            // the total number of chunks to send
            int numChunks = (fileBytes.Length + localChunkSize - 1) / localChunkSize;

            // this allows us to cancel this operation from the outside
            CancellationTokenSource cancelSource = new();
            string tokenName = fileId + data.ContactId;

            cancelTokens.TryAdd(tokenName, cancelSource);
            CancellationToken token = cancelSource.Token;

            // prepare buffer
            // [4 (length id) + length id (id) + chunk size]
            byte[] idBytes = Encoding.UTF8.GetBytes(fileId);

            byte[] buffer = new byte[(4 + idBytes.Length) + localChunkSize];
            Buffer.BlockCopy(BitConverter.GetBytes(idBytes.Length), 0, buffer, 0, 4);
            Buffer.BlockCopy(idBytes, 0, buffer, 4, idBytes.Length);

            // data starts after meta data
            int dataOffset = 4 + idBytes.Length;

            bool success = true;
            for (int i = 0; i < numChunks; i++)
            {
                if (token.IsCancellationRequested)
                {
                    success = false;
                    break;
                }

                // copy part of packet into buffer and send
                int byteOffset = i * localChunkSize;
                int endOffset = byteOffset + localChunkSize;
                endOffset = Math.Min(endOffset, fileBytes.Length);
                int counter = dataOffset;
                for (int j = byteOffset; j < endOffset; j++)
                {
                    buffer[counter] = fileBytes[j];
                    counter++;
                }
                for (int j = counter; j < buffer.Length; j++)
                {
                    buffer[j] = 0;
                }

                DataTransportManager.Instance.SendData(data.ContactId, TOPIC_DATA_PACKAGE, buffer, DataTransportManager.WrapAnswerHandler(out var SendChunkTask));
                // we wait for the sending to be accepted
                var (code, _) = await SendChunkTask;

                if (code != MessageAnswerCode.Accept)
                {
                    // client did not accept the data package, so we stop
                    success = false;
                    break;
                }
            }

            // we are finished, so we can remove the cancellation
            cancelTokens.TryRemove(tokenName, out _);

            callback?.Invoke(this, fileId, data.ContactId, success);
        }

        private void OnRequestFileTransfer(MessageData data, MessageAnswer answer)
        {
            var msg = data.ParseJson<MsgValue<string>>();
            string fileId = msg.value;
            lock (outLock)
            {
                // file does not exist
                if (!currentOutgoingTransfers.TryGetValue(fileId, out var outgoingEntry))
                {
                    answer.Deny();
                    return;
                }
                // file was not made public for the given contact
                if (!outgoingEntry.Verifier(data.ContactId))
                {
                    answer.Deny();
                    return;
                }

                // send meta data so that a data buffer can be prepared
                MsgTransferInfo msgLargeDataInfo = new()
                {
                    chunkSize = outgoingEntry.ChunkSize,
                    totalSize = outgoingEntry.Data.Length,
                };

                Debug.Log($"[LargerFileNetworkTransporter] Accepting file transfer of {fileId}");

                answer.Accept(msgLargeDataInfo);
            }

        }

        #endregion // internal methods

    }
} // end namespace Cortex