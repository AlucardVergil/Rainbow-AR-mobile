using UnityEngine;

namespace Cortex.Template
{
    /// <summary>
    /// Basic state handler for the different connection states
    /// </summary>
    public class DemoHandleUI : BaseStartOrEnabled
    {
        [SerializeField]
        private LoginScreen LoginScreen;
        [SerializeField]
        private GameObject LoggingIn;
        [SerializeField]
        private LoggedInScreen LoggedInScreen;

        [SerializeField]
        private GameObject InCall;
        [SerializeField]
        private CallState CallState;

        protected override void OnStartOrEnable()
        {
            UpdateUI();

            ConnectionModel.OnConnectionStateChanged += OnConnectionStateChanged;

            CallState.OnConferenceStateChanged += OnConferenceStateChanged;
        }

        void OnDisable()
        {
            ConnectionModel.OnConnectionStateChanged -= OnConnectionStateChanged;

            CallState.OnConferenceStateChanged -= OnConferenceStateChanged;
        }

        private void OnConferenceStateChanged(bool isActive)
        {
            UpdateUI();
        }

        private void OnConnectionStateChanged(ConnectionModel.ConnectionState state, ConnectionModel.ConnectionState previousState)
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            // very basic state handling

            if (ConnectionModel.Instance.State == ConnectionModel.ConnectionState.Initial)
            {
                LoginScreen.gameObject.SetActive(true);
                LoggingIn.SetActive(false);
                LoggedInScreen.gameObject.SetActive(false);
                InCall.SetActive(false);
            }
            else if (ConnectionModel.Instance.State == ConnectionModel.ConnectionState.RequestLogin)
            {
                LoginScreen.gameObject.SetActive(false);
                LoggingIn.SetActive(true);
                LoggedInScreen.gameObject.SetActive(false);
                InCall.SetActive(false);
            }
            else if (ConnectionModel.Instance.State == ConnectionModel.ConnectionState.LoggedIn)
            {
                // while you can do this with the Rainbow interface, you need to take care of different types of calls, like P2P and bubble conferences
                // The CallStateHandler will group those together under one "conference" interface in the CallState object
                if (CallState.IsConferenceActive)
                {
                    LoginScreen.gameObject.SetActive(false);
                    LoggingIn.SetActive(false);
                    LoggedInScreen.gameObject.SetActive(false);
                    InCall.SetActive(true);
                }
                else
                {
                    LoginScreen.gameObject.SetActive(false);
                    LoggingIn.SetActive(false);
                    LoggedInScreen.gameObject.SetActive(true);
                    InCall.SetActive(false);
                }
            }
        }
    }
} // end namespace Cortex.Template

