using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using SimpleDataChannelRainbow;
using UnityEngine;

namespace Cortex
{
    public class ConnectionEventLog : MonoBehaviour
    {
        public CallState CallState;
        public bool LogMessages = true;

        public bool LogDataChannel = false;

        // Start is called before the first frame update
        void Start()
        {

            ConnectionModel.OnLogin += OnLogin;
            ConnectionModel.OnLogout += OnLogout;
            ConnectionModel.OnLoginFailed += OnLoginFailed;

            ConnectionModel.OnCallConnected += OnCallConnected;
            ConnectionModel.OnCallDisconnected += OnCallDisconnected;
            ConnectionModel.OnCallHangUp += OnCallHangUp;
            ConnectionModel.OnCallStatusUpdate += OnCallStatusUpdate;

            ConnectionModel.OnDataChannelOpened += OnDataChannelOpened;
            ConnectionModel.OnDataChannelClosed += OnDataChannelClosed;
            ConnectionModel.OnDataChannelMessage += OnDataChannelMessage;

            ConnectionModel.OnRemoteTrack += OnTrack;
            ConnectionModel.OnRemoteMediaPublicationUpdated += OnMediaPublicationUpdated;

            DataTransportManager.OnReady += DataTransportOnReady;
            DataTransportManager.OnClose += DataTransportOnClose;

            CallState.OnContactChanged += OnContactChanged;
        }

        private void OnContactChanged(Contact contact, CallState.ContactChangeType type)
        {
            Debug.Log($"[ConnectionEventLog] Contact changed: {Util.GetContactDisplayName(contact)}, {type}");
        }

        void OnDestroy()
        {
            ConnectionModel.OnLogin -= OnLogin;
            ConnectionModel.OnLogout -= OnLogout;
            ConnectionModel.OnLoginFailed -= OnLoginFailed;

            ConnectionModel.OnCallConnected -= OnCallConnected;
            ConnectionModel.OnCallDisconnected -= OnCallDisconnected;
            ConnectionModel.OnCallHangUp -= OnCallHangUp;
            ConnectionModel.OnCallStatusUpdate -= OnCallStatusUpdate;

            ConnectionModel.OnDataChannelOpened -= OnDataChannelOpened;
            ConnectionModel.OnDataChannelClosed -= OnDataChannelClosed;
            ConnectionModel.OnDataChannelMessage -= OnDataChannelMessage;

            ConnectionModel.OnRemoteTrack -= OnTrack;
            ConnectionModel.OnRemoteMediaPublicationUpdated -= OnMediaPublicationUpdated;

            DataTransportManager.OnReady -= DataTransportOnReady;
            DataTransportManager.OnClose -= DataTransportOnClose;

            CallState.OnContactChanged -= OnContactChanged;
        }

        private void OnMediaPublicationUpdated(ConnectionModel model, Rainbow.WebRTC.MediaPublicationEventArgs e)
        {
            Debug.Log($"[ConnectionEventLog] Received media publication event status = {e.Status} jid {e.MediaPublication.PublisherJid_im} media {e.MediaPublication.Media} me {model.RainbowInterface.RainbowApplication.GetContacts().GetCurrentContactJid()} isMe: {e.MediaPublication.PublisherJid_im == model.RainbowInterface.RainbowApplication.GetContacts().GetCurrentContactJid()}");
        }

        private void OnTrack(ConnectionModel model, string callId, Rainbow.WebRTC.MediaStreamTrackDescriptor descriptor)
        {
            Debug.Log($"[ConnectionEventLog] Received OnTrack. CallId={callId} Media={descriptor.Media} PublisherID={descriptor.PublisherId} selfId={model.RainbowInterface.RainbowApplication.GetContacts().GetCurrentContactId()}");
        }

        private void DataTransportOnClose(DataTransportManager manager, Contact c)
        {
            if (!LogDataChannel)
            {
                return;
            }
            Debug.Log($"[ConnectionEventLog] Data transport closed for contact: {c.DisplayName}");

        }

        private void DataTransportOnReady(DataTransportManager manager, Contact c, DataChannel dataChannel)
        {
            if (!LogDataChannel)
            {
                return;
            }
            Debug.Log($"[ConnectionEventLog] Data transport ready for sending data to contact: {c.DisplayName}");
        }

        private void OnDataChannelMessage(ConnectionModel model, Contact contact, byte[] msg)
        {
            if (!LogDataChannel)
            {
                return;
            }
            if (!LogMessages)
            {
                return;
            }

            var msgStr = System.Text.Encoding.UTF8.GetString(msg);
            Debug.Log($"[ConnectionManager] DataChannel of {contact.DisplayName} received message {msgStr}");
        }

        private void OnDataChannelClosed(ConnectionModel model, Contact contact)
        {
            if (!LogDataChannel)
            {
                return;
            }
            Debug.Log($"[ConnectionEventLog] Data channel closed for contact: {contact.DisplayName}");
        }

        private void OnDataChannelOpened(ConnectionModel model, Contact contact)
        {
            if (!LogDataChannel)
            {
                return;
            }
            Debug.Log($"[ConnectionEventLog] Data channel opened for contact: {contact.DisplayName}");

        }

        private void OnCallDisconnected(ConnectionModel controller, Call call)
        {
            Debug.Log($"[ConnectionEventLog] Disconnected from call with participants: {string.Join(", ", call.Participants.ConvertAll(x => x.DisplayName))}");
        }

        private void OnCallStatusUpdate(ConnectionModel controller, CallEventArgs args)
        {
            Debug.Log($"[ConnectionEventLog] Call status updated: {args.Call.CallStatus}, LocalMedias: {args.Call.LocalMedias} RemoteMedias: {args.Call.RemoteMedias}");
        }

        private void OnCallHangUp(ConnectionModel controller)
        {
            Debug.Log("[ConnectionEventLog] Disconnected call");
        }

        private void OnCallConnected(ConnectionModel controller, Call call)
        {
            Debug.Log($"[ConnectionEventLog] Connected to call with participants: {string.Join(", ", call.Participants.ConvertAll(x => x.DisplayName))}");
        }

        private void OnLoginFailed(ConnectionModel controller)
        {
            Debug.Log("[ConnectionEventLog] Login unsuccessful");
        }

        private void OnLogout(ConnectionModel controller)
        {
            Debug.Log("[ConnectionEventLog] Logout");
        }

        private void OnLogin(ConnectionModel r)
        {
            Debug.Log("[ConnectionEventLog] Login successful");
        }
    }
} // end namespace Cortex