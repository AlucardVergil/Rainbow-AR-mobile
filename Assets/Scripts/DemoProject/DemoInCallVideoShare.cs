using Cortex;
using Rainbow.Model;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Basic script to handle video and sharing functionality during a call
/// </summary>
public class DemoInCallVideoShare : BaseStartOrEnabled
{
    [SerializeField]
    private InCallMenu InCallMenu;

    [SerializeField]
    private Camera m_shareCamera;

    public Camera ShareCamera
    {
        get => m_shareCamera;
        set
        {
            m_shareCamera = value;

            UpdateShareCamera();
        }
    }

    [SerializeField]
    private PreviewWebcam PreviewWebcam;

    private ConnectionModel model;

    protected override void OnStartOrEnable()
    {
        // callbacks
        InCallMenu.OnRequestShareVideo += OnRequestShareVideo;
        InCallMenu.OnRequestShare += OnRequestShare;
        InCallMenu.OnStopShare += OnStopShare;

        PreviewWebcam.OnShareCamera += OnShareCamera;
        PreviewWebcam.gameObject.SetActive(false);

        model = ConnectionModel.Instance;

        Assert.IsNotNull(model);
    }

    void OnDisable()
    {
        InCallMenu.OnRequestShareVideo -= OnRequestShareVideo;
        InCallMenu.OnRequestShare -= OnRequestShare;
        InCallMenu.OnStopShare -= OnStopShare;

        PreviewWebcam.OnShareCamera -= OnShareCamera;
        PreviewWebcam.gameObject.SetActive(false);

        ShareCamera.gameObject.SetActive(false);
    }

    private void OnShareCamera(WebCamDevice device)
    {
        // will be called when sharing the given camera is requested
        var ri = model.RainbowInterface;

        // attach the camera and enable video sharing
        ri.WebCamVideoDevice = device;
        ri.CameraVideo = null;
        // webcam provides aspect ratio by itself...
        ri.AddMediaToCurrentCall(Call.Media.VIDEO);
    }

    private void OnStopShare()
    {
        ShareCamera.gameObject.SetActive(false);
    }

    private void OnRequestShare()
    {
        // will be called when sharing is requested
        ShareCamera.gameObject.SetActive(true);

        var ri = model.RainbowInterface;

        // we share the special camera with the simple 3D scene
        ri.WebCamSharingDevice = null;
        ri.CameraSharing = ShareCamera;
        ri.PublishSharingHeight = (int)(ConnectionModel.Instance.RainbowInterface.PublishSharingWidth / ShareCamera.aspect);
        ri.AddMediaToCurrentCall(Call.Media.SHARING);
    }

    private void UpdateShareCamera()
    {
        var ri = model.RainbowInterface;

        // remove old
        ri.RemoveMediaFromCurrentCall(Call.Media.SHARING);

        // set new
        OnRequestShare();
    }

    private void OnRequestShareVideo()
    {
        PreviewWebcam.gameObject.SetActive(true);
    }
}
