using Rainbow.WebRTC.Abstractions;
using UnityWebRTC = Unity.WebRTC;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.UI;

namespace Rainbow.WebRTC.Unity
{
    public class UnityWebRTCFactory : IWebRTCFactory
    {
        private Microsoft.Extensions.Logging.ILogger log = null;
        private static RTCIceServer[] SavedIceServers = null;
        private string loggerPrefix;

        public static RTCIceServer[] GetIceServers()
        {
            return SavedIceServers;
        }

        public UnityWebRTCFactory()
        {
            UnityExecutor.Initialize();
        }


        public TextureMediaDevice CreateTextureDevice(Texture? texture)
        {
            TextureMediaDevice result = null;
            UnityExecutor.ExecuteSync(() =>
            {
                result = new TextureMediaDevice(texture, loggerPrefix);

            });
            return result;
        }

        public WebCamMediaDevice CreateWebCamDevice(WebCamDevice? webcamDevice, int w, int h)
        {
            WebCamMediaDevice result = null;
            UnityExecutor.ExecuteSync(() =>
            {
                result = new WebCamMediaDevice(webcamDevice, w, h, loggerPrefix);

            });
            return result;
        }

        public CameraMediaDevice CreateCameraDevice(Camera camera, int w, int h)
        {
            CameraMediaDevice result = null;
            UnityExecutor.ExecuteSync(() =>
            {
                result = new CameraMediaDevice(camera, w, h, loggerPrefix);

            });
            return result;
        }

        static public void OutputAudio(IAudioStreamTrack track, AudioSource outputAudioSource)
        {
            UnityExecutor.ExecuteSync(() =>
            {
                RainbowAudioStreamTrack audioTrack = track as RainbowAudioStreamTrack;
                outputAudioSource.SetTrack(audioTrack.Instance as AudioStreamTrack);
                outputAudioSource.loop = true;
            });
        }

        static public void OutputVideoTrack(IVideoStreamTrack videoStreamTrack, RawImage outputImage)
        {
            UnityExecutor.ExecuteSync(() =>
            {
                var track = videoStreamTrack as RainbowVideoStreamTrack;
                var realTrack = track.Instance as VideoStreamTrack;
                outputImage.texture = realTrack.Texture;
                realTrack.OnVideoReceived += tex => outputImage.texture = tex;
            });
        }

        public AudioMediaDevice CreateAudioMediaDevice(AudioSource audioSource)
        {
            return new AudioMediaDevice(audioSource, loggerPrefix);
        }
        public IAudioStreamTrack? CreateAudioTrack(IMediaDevice mediaDevice)
        {
            Microsoft.Extensions.Logging.ILogger log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
            RainbowAudioStreamTrack result = null;
            UnityExecutor.ExecuteSync(() =>
            {
                if (mediaDevice is AudioMediaDevice audioDevice)
                {
                    result = audioDevice.Track as RainbowAudioStreamTrack;
                }

            });
            return result;

        }

        public IAudioStreamTrack CreateEmptyAudioTrack()
        {
            RainbowAudioStreamTrack result = null;

            UnityExecutor.ExecuteSync(() =>
            {
                var component = new GameObject();

                AudioSource audioSource = component.AddComponent<AudioSource>();
                result = RainbowAudioStreamTrack.Wrap(new AudioStreamTrack(audioSource), loggerPrefix);
            });
            return result;
        }

        public IVideoStreamTrack CreateEmptyVideoTrack()
        {
            RainbowVideoStreamTrack result = null;

            UnityExecutor.ExecuteSync(() =>
            {
                var format = UnityWebRTC.WebRTC.GetSupportedGraphicsFormat(SystemInfo.graphicsDeviceType);
                var tex = new RenderTexture(800, 600, 0, format);
                tex.Create();
                result = RainbowVideoStreamTrack.Wrap(new VideoStreamTrack(tex), loggerPrefix, true);
            });
            return result;
        }

        public IPeerConnection CreatePeerConnection(Configuration configuration)
        {
            PeerConnection result = new PeerConnection(configuration, loggerPrefix);

            if (SavedIceServers == null)
                SavedIceServers = Helpers.RTCConfiguration(configuration).iceServers;
            return result;
        }


        public IVideoStreamTrack? CreateVideoTrack(IMediaDevice mediaDevice)
        {
            if (mediaDevice is MediaDevice videoDevice)
            {
                return videoDevice.Track as RainbowVideoStreamTrack;
            }
            return null;
        }

        public void Initialize()
        {
        }

        public void SetLoggerPrefix(string loggerPrefix)
        {
            this.loggerPrefix = loggerPrefix;
            log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
        }
    }
}
