using Rainbow;
using Rainbow.Model;
using UnityEngine;

namespace Cortex
{
    public class DebugLogNetworkMessages : MonoBehaviour
    {

        [SerializeField]
        private bool LogIncomingMessage = true;
        [SerializeField]
        private bool LogIncomingMessageData = true;
        [SerializeField]
        private bool LogOutgoingMessage = true;
        [SerializeField]
        private bool LogOutgoingMessageData = true;
        void Start()
        {
            if (LogIncomingMessage)
            {
                DataTransportManager.RegisterMessageHandler(AllMessageHandler);
            }

            if (LogOutgoingMessage)
            {
                DataTransportManager.RegisterOutgoingMessageHandler(AllOutMessageHandler);
            }
        }

        void OnDestroy()
        {
            if (LogIncomingMessage)
            {
                DataTransportManager.RemoveMessageHandler(AllMessageHandler);
            }

            if (LogOutgoingMessage)
            {
                DataTransportManager.RemoveOutgoingMessageHandler(AllOutMessageHandler);
            }
        }

        private void AllOutMessageHandler(MessageData data, string contactId)
        {
            Contact contact = ConnectionModel.Instance.Contacts.GetContactFromContactId(contactId);

            if (LogOutgoingMessageData)
            {
                Debug.Log($"[DebugLogNetworkMessages] Outgoing - Topic: {data.TopicName}, Contact: {Util.GetContactDisplayName(contact)}, Message (Utf-8):\n{data.Utf8}");
            }
            else
            {
                Debug.Log($"[DebugLogNetworkMessages] Outgoing - Topic: {data.TopicName}, Contact: {Util.GetContactDisplayName(contact)}, Message length: {data.Data.Length}");
            }
        }

        private void AllMessageHandler(MessageData data, MessageAnswer answer)
        {
            Contact contact = ConnectionModel.Instance.Contacts.GetContactFromContactId(data.ContactId);

            if (LogIncomingMessageData)
            {
                Debug.Log($"[DebugLogNetworkMessages] Incoming - Topic: {data.TopicName}, Contact: {Util.GetContactDisplayName(contact)}, Message (Utf-8):\n{data.Utf8}");
            }
            else
            {
                Debug.Log($"[DebugLogNetworkMessages] Incoming - Topic: {data.TopicName}, Contact: {Util.GetContactDisplayName(contact)}, Message length: {data.Data.Length}");
            }
        }

    }
} // end namespace Cortex