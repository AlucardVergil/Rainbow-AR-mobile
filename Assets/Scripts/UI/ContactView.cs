using System;
using Rainbow.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    public class ContactView : BaseStartOrEnabled
    {
        public event Action<ContactView, Contact> OnClickCall;
        public event Action<ContactView, Contact> OnClickHangUp;

        public RainbowAvatarLoader AvatarLoader;

        private Contact contact;
        public Contact CurrentContact
        {
            get { return contact; }
            set
            {
                contact = value;
                UpdateUi();
            }
        }

        private SpriteChangeButton callButton;
        private RawImage Avatar;
        private TMP_Text textDisplayName;
        private Button hangUpButton;

        private readonly ResetCancellationToken cancelLoad = new();
        void Awake()
        {
            callButton = GameObjectUtils.FindGameObjectByName(transform, "ButtonCall", true).GetComponent<SpriteChangeButton>();
            hangUpButton = GameObjectUtils.FindGameObjectByName(transform, "ButtonHangUp", true).GetComponent<Button>();
            textDisplayName = GameObjectUtils.FindGameObjectByName(transform, "ContactDisplayName", true).GetComponent<TMP_Text>();
            Avatar = GameObjectUtils.FindGameObjectByName(transform, "ContactAvatar", true).GetComponent<RawImage>();

            if (AvatarLoader == null)
            {
                AvatarLoader = FindFirstObjectByType<RainbowAvatarLoader>();
            }
        }

        protected override void OnStartOrEnable()
        {
            callButton.OnClick += OnClickCallInternal;
            hangUpButton.onClick.AddListener(OnHangUpInternal);
            callButton.Button.interactable = true;
            ConnectionModel.OnCallDisconnected += OnCallDisconnected;
            AvatarLoader.OnPeerAvatarChanged += OnPeerAvatarChanged;
        }
        void OnDisable()
        {
            ConnectionModel.OnCallDisconnected -= OnCallDisconnected;
            callButton.OnClick -= OnClickCallInternal;
            hangUpButton.onClick.RemoveListener(OnHangUpInternal);
            AvatarLoader.OnPeerAvatarChanged -= OnPeerAvatarChanged;
        }

        private void OnPeerAvatarChanged(Contact c, RainbowAvatarLoader.AvatarChange change)
        {
            if (c == CurrentContact)
            {
                UpdateImage();
            }
        }

        private void OnHangUpInternal()
        {
            OnClickHangUp?.Invoke(this, CurrentContact);
        }

        private void OnCallDisconnected(ConnectionModel model, Call call)
        {
            callButton.Button.interactable = true;
            hangUpButton.interactable = false;
        }

        private void OnClickCallInternal()
        {
            if (callButton.Switched)
            {
                // button was put into switched state -> we called
                OnClickCall?.Invoke(this, CurrentContact);
            }
            else
            {
                // hangup
                OnClickHangUp?.Invoke(this, CurrentContact);
                callButton.Button.interactable = false;
            }
        }

        void UpdateUi()
        {
            if (contact == null)
            {
                return;
            }

            textDisplayName.text = contact.DisplayName;

            UpdateImage();
        }

        async void UpdateImage()
        {
            cancelLoad.Reset();
            var token = cancelLoad.Token;

            try
            {
                await AvatarLoader.UpdateAvatarImage(contact, 128, Avatar, setAlpha: true, cancellationToken: token);

            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == token)
            {
                // canceled -> nothing to do
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
} // end namespace Cortex