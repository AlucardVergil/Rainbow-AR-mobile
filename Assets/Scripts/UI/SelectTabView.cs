using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    public class SelectTabView : BaseStartOrEnabled
    {
        [SerializeField]
        private GameObject m_ContactsView;
        [SerializeField]
        private GameObject m_BubbleView;

        public GameObject ContactsView { get => m_ContactsView; }
        public GameObject BubbleView { get => m_BubbleView; }

        [SerializeField]
        private Button buttonContacts;
        [SerializeField]
        private Button buttonBubbles;

        void Awake()
        {
            buttonContacts = GameObjectUtils.FindGameObjectByName(transform, "ButtonContacts", true).GetComponent<Button>();
            buttonBubbles = GameObjectUtils.FindGameObjectByName(transform, "ButtonBubbles", true).GetComponent<Button>();
        }

        protected override void OnStartOrEnable()
        {
            buttonContacts.onClick.AddListener(OnClickContacts);
            buttonBubbles.onClick.AddListener(OnClickBubbles);

            // Select bubbles first
            OnClickBubbles();
        }

        void OnDisable()
        {
            buttonContacts.onClick.RemoveListener(OnClickContacts);
            buttonBubbles.onClick.RemoveListener(OnClickBubbles);
        }

        private void OnClickBubbles()
        {
            buttonContacts.interactable = true;
            buttonBubbles.interactable = false;

            ContactsView.SetActive(false);
            BubbleView.SetActive(true);
        }

        private void OnClickContacts()
        {
            buttonContacts.interactable = false;
            buttonBubbles.interactable = true;

            ContactsView.SetActive(true);
            BubbleView.SetActive(false);
        }

    }
} // end namespace Cortex