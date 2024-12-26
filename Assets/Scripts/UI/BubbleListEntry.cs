using System;
using Cortex.ColorExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Simple entry for a bubble that is shown in a list
    /// </summary>
    public class BubbleListEntry : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Will be called when this entry is clicked
        /// </summary>
        public event Action<BubbleListEntry> OnClick;

        [SerializeField]
        private TMP_Text TextObject;

        [SerializeField]
        private RawImage AvatarObject;

        [SerializeField]
        private Image Activity;

        /// <summary>
        /// The text displayed on this element
        /// </summary>
        public TMP_Text Text { get => TextObject; }
        /// <summary>
        /// The bubble avatar
        /// </summary>
        public RawImage Avatar { get => AvatarObject; }

        private Image Background;
        private Color normalBgColor;
        private Color darkBgColor;

        private bool _active = false;

        /// <summary>
        /// Gets/Sets the activity status of the bubble. True, when a bubble has a running conference, false if not
        /// </summary>
        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                Activity.gameObject.SetActive(_active);
            }
        }

        private bool _selected;

        /// <summary>
        /// Gets/Sets whether this bubble is selected
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

        void Awake()
        {
            if (TextObject == null)
            {
                TextObject = GameObjectUtils.FindGameObjectByName(transform, "BubbleName", true).GetComponent<TMP_Text>();
            }

            if (AvatarObject == null)
            {
                AvatarObject = GameObjectUtils.FindGameObjectByName(transform, "Avatar", true).GetComponent<RawImage>();
            }

            Background = GetComponent<Image>();
            normalBgColor = Background.color;
            darkBgColor = Background.color.Darken();

            Active = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
            eventData.Use();
        }
    }
} // end namespace Cortex