using System;
using Cortex.ColorExtensionMethods;
using Rainbow;
using Rainbow.Model;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// UI element to display a contact in a list or something similar
    /// </summary>
    public class ContactEntry : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Fired when this entry is clicked
        /// </summary>
        public event Action<ContactEntry> OnClick;

        private Contact contact;

        /// <summary>
        /// Gets/Sets the contact associated with this entry
        /// </summary>
        public Contact Contact
        {
            get
            {
                return contact;
            }

            set
            {
                contact = value;
                ContactInitialsAvatar.Contact = contact;
                displayName.text = Util.GetContactDisplayName(contact);
            }
        }

        /// <summary>
        /// Combined avatar and initials of the contact
        /// </summary>
        public ContactInitialsAvatar ContactInitialsAvatar
        {
            get => m_contactInitialsAvatar;
            private set => m_contactInitialsAvatar = value;
        }

        // Backing field for property ContactInitialsAvatar
        [SerializeField]
        private ContactInitialsAvatar m_contactInitialsAvatar;

        [SerializeField]
        private TMP_Text displayName;

        [SerializeField]
        private Image status;

        private Image Background;
        private Color normalBgColor;
        private Color darkBgColor;
        private bool _selected;

        /// <summary>
        /// Gets/Sets whether the current entry is selected or not
        /// </summary>
        public bool Selected
        {
            get => _selected; set
            {
                _selected = value;

                if (_selected)
                {
                    Background.color = darkBgColor;
                }
                else
                {
                    Background.color = normalBgColor;
                }
            }
        }



        // Vagelis
        GameObject contactGameobject;



        void Awake()
        {
            if (displayName == null)
            {
                displayName = GameObjectUtils.FindGameObjectByName(transform, "DisplayName", true).GetComponent<TMP_Text>();
            }

            if (status == null)
            {
                status = GameObjectUtils.FindGameObjectByName(transform, "Status", true).GetComponent<Image>();
            }

            if (ContactInitialsAvatar == null)
            {
                ContactInitialsAvatar = GameObjectUtils.FindGameObjectByName(transform, "ContactInitialsAvatar", true).GetComponent<ContactInitialsAvatar>();
            }

            Background = GetComponent<Image>();
            normalBgColor = Background.color;
            darkBgColor = Background.color.Darken();
        }

        /// <summary>
        /// Sets the presence level of the contact
        /// </summary>
        /// <param name="presenceLevel">The presence level. This must be a value from Rainbow.Model.PresenceLevel</param>
        public void SetPresenceLevel(string presenceLevel)
        {
            if (presenceLevel == PresenceLevel.Online)
            {
                status.color = Color.green;
            }
            else if (presenceLevel == PresenceLevel.Offline)
            {
                status.color = Color.gray;
            }
            else if (presenceLevel == PresenceLevel.Away)
            {
                status.color = Color.yellow;
            }
            else if (presenceLevel == PresenceLevel.Busy)
            {
                status.color = Color.red;
            }
        }



        // Vagelis
        private void Start()
        {
            contactGameobject = GameObject.FindGameObjectWithTag("Contacts");
        }



        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
            eventData.Use();

            // Vagelis
            contactGameobject.SetActive(false);
        }
    }
} // end namespace Cortex