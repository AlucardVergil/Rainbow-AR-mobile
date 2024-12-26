using System;
using Rainbow;
using Rainbow.Model;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Base screen when logged in
    /// </summary>
    public class LoggedInScreen : BaseStartOrEnabled
    {
        public RainbowAvatarLoader AvatarLoader;

        #region ui-elements
        [SerializeField]
        private TMP_Text textUsername;
        [SerializeField]
        private Button buttonLogout;

        [SerializeField]
        private RawImage avatar;

        #endregion // ui-elements

        #region unity lifecycle

        void Awake()
        {
            if (textUsername == null)
            {
                textUsername = GameObjectUtils.FindGameObjectByName(transform, "Username", true).GetComponent<TMP_Text>();
            }
            if (buttonLogout == null)
            {
                buttonLogout = GameObjectUtils.FindGameObjectByName(transform, "ButtonLogout", true).GetComponent<Button>();
            }
            if (avatar == null)
            {
                avatar = GameObjectUtils.FindGameObjectByName(transform, "Avatar", true).GetComponent<RawImage>();
            }

            Assert.IsNotNull(textUsername);
            Assert.IsNotNull(buttonLogout);

            if (AvatarLoader == null)
            {
                AvatarLoader = FindFirstObjectByType<RainbowAvatarLoader>();
            }
        }

        protected override void OnStartOrEnable()
        {
            buttonLogout.onClick.AddListener(OnClickLogout);

            var user = ConnectionModel.Instance.Contacts.GetCurrentContact();
            if (user != null)
            {
                textUsername.text = Util.GetContactDisplayName(user);

                UpdateUserAvatar(user);
            }

            AvatarLoader.OnPeerAvatarChanged += OnPeerChanged;
        }

        void OnDisable()
        {
            buttonLogout.onClick.RemoveListener(OnClickLogout);

            avatarCancel.Reset();

            AvatarLoader.OnPeerAvatarChanged -= OnPeerChanged;
        }

        #endregion // unity lifecycle

        #region  internal

        readonly ResetCancellationToken avatarCancel = new();

        #endregion // internal

        private void OnPeerChanged(Contact c, RainbowAvatarLoader.AvatarChange change)
        {
            if (c == ConnectionModel.Instance.Contacts.GetCurrentContact())
            {
                UpdateUserAvatar(c);
            }
        }
        async void UpdateUserAvatar(Contact user)
        {
            avatarCancel.Reset();
            var token = avatarCancel.Token;
            try
            {
                await AvatarLoader.UpdateAvatarImage(user, 128, avatar, setAlpha: true, cancellationToken: token);
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

        private void OnClickLogout()
        {
            ConnectionModel.Instance.RequestLogout();
        }
    }
} // end namespace Cortex