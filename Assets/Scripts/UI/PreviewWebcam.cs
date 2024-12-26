using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Simple Webcam selection with preview
    /// </summary>
    public class PreviewWebcam : BaseStartOrEnabled
    {
        public event Action<WebCamDevice> OnShareCamera;

        public bool skipFirstCamera = false;
        public bool skipFrontFacing = false;
        public bool skipBackFacing = false;
        #region ui

        [SerializeField]
        private Button buttonShare;

        public RawImage WebcamImage
        {
            get => m_webcamImage;
            private set => m_webcamImage = value;
        }
        // Backing field for property WebcamImage
        [SerializeField]
        private RawImage m_webcamImage;

        [SerializeField]
        private TMP_Dropdown dropdownWebcams;

        [SerializeField]

        private Button buttonClose;

        #endregion // ui

        void Awake()
        {
            if (buttonShare == null)
            {
                buttonShare = GameObjectUtils.FindGameObjectByName(transform, "ButtonShareVideo", true).GetComponent<Button>();
            }
            if (buttonClose == null)
            {
                buttonClose = GameObjectUtils.FindGameObjectByName(transform, "ButtonClose", true).GetComponent<Button>();
            }
            if (WebcamImage == null)
            {
                WebcamImage = GameObjectUtils.FindGameObjectByName(transform, "WebcamImage", true).GetComponent<RawImage>();
            }
            if (dropdownWebcams == null)
            {
                dropdownWebcams = GameObjectUtils.FindGameObjectByName(transform, "DropdownWebcams", true).GetComponent<TMP_Dropdown>();
            }
        }

        protected override void OnStartOrEnable()
        {
            buttonClose.onClick.AddListener(OnClickClose);
            buttonShare.onClick.AddListener(OnClickShare);
            dropdownWebcams.onValueChanged.AddListener(OnWebcamValueChanged);

            RefreshUi();
        }
        void OnDisable()
        {
            buttonClose.onClick.RemoveListener(OnClickClose);
            buttonShare.onClick.RemoveListener(OnClickShare);
            dropdownWebcams.onValueChanged.RemoveListener(OnWebcamValueChanged);

            StopUI();
        }

        private void OnClickShare()
        {
            string name = dropdownWebcams.options[dropdownWebcams.value].text;
            WebCamDevice[] devices = WebCamTexture.devices;

            foreach (var d in devices)
            {
                if (d.name == name)
                {
                    RemoveCurrentWebcamTexture();
                    gameObject.SetActive(false);
                    OnShareCamera?.Invoke(d);
                    return;
                }
            }
        }

        private void OnWebcamValueChanged(int index)
        {
            string name = dropdownWebcams.options[index].text;
            try
            {
                WebCamTexture tex = new(name);
                tex.Play();
                if (tex.isPlaying)
                {
                    tex.Stop();
                    // webcam works

                    SwitchWebcamTexture(name);

                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
        }

        private void OnClickClose()
        {
            gameObject.SetActive(false);
        }

        private void RemoveCurrentWebcamTexture()
        {
            Texture tex = WebcamImage.texture;
            WebcamImage.texture = null;
            WebCamTexture wTex = tex as WebCamTexture;
            if (wTex != null)
            {
                try
                {
                    wTex.Stop();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        private void SwitchWebcamTexture(string name)
        {
            RemoveCurrentWebcamTexture();
            WebCamTexture tex = new(name);
            tex.Play();

            WebcamImage.texture = tex;
            float aspect = (float)tex.width / (float)tex.height;
            if (WebcamImage.gameObject.TryGetComponent(out AspectRatioFitter f))
            {
                f.aspectRatio = aspect;
            }
            else if (WebcamImage.gameObject.TryGetComponent(out RectTransform tr))
            {
                Vector2 s = tr.sizeDelta;
                tr.sizeDelta = new Vector2(s.x, s.y / aspect);
            }
        }
        private void RefreshUi()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            List<WebCamDevice> activeDevices = new();

            int startIndex = skipFirstCamera ? 1 : 0;
            // find available ones
            for (int i = startIndex; i < devices.Length; i++)
            {
                WebCamDevice d = devices[i];
                if (d.isFrontFacing)
                {
                    if (skipFrontFacing)
                    {
                        continue;
                    }
                }
                else
                {
                    if (skipBackFacing)
                    {
                        continue;
                    }
                }
                try
                {
                    // there is an issue with cameras that allow multi access and also allow stop
                    // with the unity methods there doesn't seem to be a way to check for that
                    WebCamTexture tex = new(d.name);
                    tex.Play();
                    if (tex.isPlaying)
                    {
                        tex.Stop();
                        // webcam works
                        activeDevices.Add(d);

                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            // select first in the list
            if (activeDevices.Count > 0)
            {
                WebCamDevice firstDevice = activeDevices[0];

                SwitchWebcamTexture(firstDevice.name);
            }

            List<string> options = new();
            foreach (var d in activeDevices)
            {
                options.Add(d.name);
            }
            dropdownWebcams.ClearOptions();
            dropdownWebcams.AddOptions(options);
        }

        private void StopUI()
        {
            // stop the webcam from running
            RemoveCurrentWebcamTexture();
        }

    }
} // end namespace Cortex