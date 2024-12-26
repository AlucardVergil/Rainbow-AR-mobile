using Rainbow;
using Rainbow.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Script representing an element with Initials and an Avatar for a Rainbow contact
    /// </summary>
    public class ContactInitialsAvatar : MonoBehaviour
    {
        /// <summary>
        /// Gets/Sets the background color
        /// </summary>
        public Color BackgroundColor
        {
            get => image.color;
            set
            {
                image.color = value;
            }
        }

        /// <summary>
        /// Gets/Sets the initials text color
        /// </summary>
        public Color TextColor
        {
            get => text.color;
            set
            {
                text.color = value;
            }
        }

        /// <summary>
        /// Gets/Sets the current contact
        /// </summary>
        public Contact Contact
        {
            get => _contact;
            set
            {
                _contact = value;

                string initials = Util.GetContactInitials(_contact);
                text.text = initials;
            }
        }

        /// <summary>
        /// Gets/Sets the texture of the avatar image
        /// </summary>
        public Texture AvatarImage
        {
            get => avatar.texture;
            set
            {
                avatar.texture = value;

                avatar.transform.parent.gameObject.SetActive(avatar.texture != null);
            }
        }

        private Contact _contact;

        [SerializeField]
        private Image image;
        [SerializeField]
        private TMP_Text text;

        [SerializeField]
        private RawImage avatar;
        void Awake()
        {
            if (image == null)
            {
                image = GameObjectUtils.FindGameObjectByName(transform, "Background", true).GetComponent<Image>();
            }
            if (text == null)
            {
                text = GameObjectUtils.FindGameObjectByName(transform, "Initials", true).GetComponent<TMP_Text>();
            }
            if (avatar == null)
            {
                avatar = GameObjectUtils.FindGameObjectByName(transform, "Avatar", true).GetComponent<RawImage>();
            }
        }

    }
} // end namespace Cortex