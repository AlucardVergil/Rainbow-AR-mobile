
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Class to keep update the call state based on events of the ConnectionModel
    /// </summary>
    public class CallStateHandler : MonoBehaviour
    {
        public CallState CallState;

        void Start()
        {
            ConnectionModel.OnCallConnected += OnCallConnected;
            ConnectionModel.OnCallDisconnected += OnCallDisconnected;

            ConnectionModel.OnLogout += OnLogout;
            ConnectionModel.OnLogin += OnLogin;

            ConnectionModel.OnConferenceParticipantsUpdate += OnConferenceParticipantsUpdate;

            var model = ConnectionModel.Instance;
            if (model != null)
            {
                CallState.UserContact = model.CurrentUser;

                var ri = model.RainbowInterface;
                if (ri != null)
                {
                    var call = ri.CurrentCall;
                    if (call != null)
                    {
                        // TODO we might need to handle conferences differently
                        OnCallConnected(model, call);
                    }
                }
            }
        }

        void OnDestroy()
        {
            ConnectionModel.OnCallConnected -= OnCallConnected;
            ConnectionModel.OnCallDisconnected -= OnCallDisconnected;

            ConnectionModel.OnLogout -= OnLogout;
            ConnectionModel.OnLogin -= OnLogin;

            ConnectionModel.OnConferenceParticipantsUpdate -= OnConferenceParticipantsUpdate;
        }

        private void OnConferenceParticipantsUpdate(ConnectionModel model, ConferenceParticipantsEventArgs args)
        {
            CallState.UpdateFromParticipants(args.Participants, model.Contacts);

            if (CallState.UserContact != null)
            {
                foreach (var p in args.Participants)
                {
                    if (p.Value.Id == CallState.UserContact.Id)
                    {
                        // connected to call
                        CallState.IsConference = true;
                        CallState.IsConferenceActive = true;
                        return;
                    }
                }

                if (CallState.IsConferenceActive)
                {
                    // not in call anymore
                    ClearState();
                }
            }
        }

        private void OnLogin(ConnectionModel model)
        {
            CallState.UserContact = model.CurrentUser;
        }

        private void ClearState()
        {
            CallState.Clear();
            CallState.IsConference = false;
            CallState.IsConferenceActive = false;
        }

        private void OnLogout(ConnectionModel model)
        {
            ClearState();
            // additionally reset user
            CallState.UserContact = null;
        }

        private void OnCallDisconnected(ConnectionModel model, Call call)
        {
            ClearState();
        }

        private void OnCallConnected(ConnectionModel model, Call call)
        {
            CallState.IsConferenceActive = true;
            CallState.IsInitiator = call.IsInitiator;

            foreach (var pc in call.Participants)
            {
                Contact c = model.Contacts.GetContactFromContactId(pc.UserId);
                if (c == null)
                {
                    Debug.LogWarning($"Invalid id: {pc}");
                    continue;
                }

                if (c == CallState.UserContact)
                {
                    continue;
                }

                CallState.AddContact(c);
            }
        }
    }
} // end namespace Cortex