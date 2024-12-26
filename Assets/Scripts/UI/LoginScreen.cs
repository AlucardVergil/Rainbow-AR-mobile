using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Basic Rainbow login screen
    /// </summary>
    public class LoginScreen : BaseStartOrEnabled
    {
        #region ui-elements
        [SerializeField]
        private TMP_InputField fieldHostname;
        [SerializeField]
        private TMP_InputField fieldUser;
        [SerializeField]
        private TMP_InputField fieldPassword;
        [SerializeField]
        private Button buttonLogin;
        [SerializeField]
        private Button buttonResetHost;
        [SerializeField]
        private Button buttonQuit;

        #endregion // ui-elements

        #region internal

        #endregion // internal

        void Awake()
        {
            if (fieldHostname == null)
            {
                fieldHostname = GameObjectUtils.FindGameObjectByName(transform, "InputHostname", true).GetComponent<TMP_InputField>();
            }

            if (fieldUser == null)
            {
                fieldUser = GameObjectUtils.FindGameObjectByName(transform, "InputUserName", true).GetComponent<TMP_InputField>();
            }

            if (fieldPassword == null)
            {
                fieldPassword = GameObjectUtils.FindGameObjectByName(transform, "InputPassword", true).GetComponent<TMP_InputField>();
            }

            if (buttonLogin == null)
            {
                buttonLogin = GameObjectUtils.FindGameObjectByName(transform, "ButtonLogin", true).GetComponent<Button>();
            }

            if (buttonResetHost == null)
            {
                buttonResetHost = GameObjectUtils.FindGameObjectByName(transform, "ButtonResetHost", true).GetComponent<Button>();
            }

            if (buttonQuit == null)
            {
                buttonQuit = GameObjectUtils.FindGameObjectByName(transform, "ButtonQuit", true).GetComponent<Button>();
            }

            Assert.IsNotNull(fieldHostname);
            Assert.IsNotNull(fieldUser);
            Assert.IsNotNull(fieldPassword);
            Assert.IsNotNull(buttonLogin);
            Assert.IsNotNull(buttonResetHost);
        }

        protected override void OnStartOrEnable()
        {
            buttonResetHost.onClick.AddListener(OnClickHostResetInternal);
            buttonLogin.onClick.AddListener(OnClickLoginInternal);
            buttonQuit.onClick.AddListener(OnClickQuit);

            // restore previous login details

            string storedHostname = PlayerPrefs.GetString("host_name", "");
            if (storedHostname != "")
            {
                fieldHostname.text = Crypt.AesDecrypt(storedHostname);
            }
            else
            {
                fieldHostname.text = "openrainbow.com";
            }

            // TODO maybe add salt for better encryption
            string storedName = PlayerPrefs.GetString("login_name", "");
            if (storedName != "")
            {
                fieldUser.text = Crypt.AesDecrypt(storedName);
            }

            string storedPassword = PlayerPrefs.GetString("login_password", "");
            if (storedPassword != "")
            {
                fieldPassword.text = Crypt.AesDecrypt(storedPassword);
            }
        }

        void OnDisable()
        {
            buttonResetHost.onClick.RemoveListener(OnClickHostResetInternal);
            buttonLogin.onClick.RemoveListener(OnClickLoginInternal);
            buttonQuit.onClick.RemoveListener(OnClickQuit);
        }

        private void OnClickQuit()
        {
            Application.Quit();
        }

        #region  ui-callbacks
        private void OnClickLoginInternal()
        {
            PlayerPrefs.SetString("host_name", Crypt.AesEncrypt(fieldHostname.text));
            PlayerPrefs.SetString("login_name", Crypt.AesEncrypt(fieldUser.text));
            PlayerPrefs.SetString("login_password", Crypt.AesEncrypt(fieldPassword.text));
            ConnectionModel.Instance.RequestLogin(fieldUser.text, fieldPassword.text, fieldHostname.text);
        }

        #endregion // ui-callbacks

        private void OnClickHostResetInternal()
        {
            fieldHostname.text = "openrainbow.com";
        }
    }
} // end namespace Cortex