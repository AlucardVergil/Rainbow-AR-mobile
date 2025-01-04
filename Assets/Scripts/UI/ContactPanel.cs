using System;
using Rainbow.Model;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Script for a panel showing the Contacts of a Rainbow account when logged in
    /// </summary>
    public class ContactPanel : BaseStartOrEnabled
    {
        #region ui-elements

        [SerializeField]
        private ContactList contactList;

        [SerializeField]
        private ContactView contactView;
        #endregion // ui-elements


        // vagelis
        public GameObject contactsGameobject;


        #region unity-lifecycle

        protected override void OnStartOrEnable()
        {
            // vagelis

            contactsGameobject.SetActive(true);

            // end vagelis


            contactList.OnSelectionChanged += OnContactSelectedChanged;

            contactView.OnClickCall += OnClickCall;
            contactView.OnClickHangUp += OnClickHangUp;

            contactView.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            contactList.OnSelectionChanged -= OnContactSelectedChanged;

            contactView.OnClickCall -= OnClickCall;
            contactView.OnClickHangUp -= OnClickHangUp;

            contactView.gameObject.SetActive(false);
        }

        private async void OnClickHangUp(ContactView view, Contact contact)
        {
            try
            {
                await ConnectionModel.Instance.HangupCall();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnClickCall(ContactView view, Contact contact)
        {
            ConnectionModel.Instance.P2PCallContact(contact.Id, 60.0f);
        }

        private void OnContactSelectedChanged(ContactList list, Contact contact)
        {
            contactView.CurrentContact = contact;
            contactView.gameObject.SetActive(contact != null);
        }

        #endregion // unity-lifecycle
    }
} // end namespace Cortex