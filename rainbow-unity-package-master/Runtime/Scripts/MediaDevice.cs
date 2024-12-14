using Rainbow.WebRTC.Abstractions;
using System;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

namespace Rainbow.WebRTC.Unity
{
    public class VideoDeviceOptionsNames
    {
        public const string MaxFramerate = "maxFramerate";
        public const string MaxBitrate = "maxBitrate";
        public const string MinBitrate = "minBitrate";
        public const string ScaleResolutionDownBy = "scaleResolutionDownBy";

        internal static bool IsSupportedOption(string option)
        {
            switch( option)
            {
                case VideoDeviceOptionsNames.MaxBitrate:
                case VideoDeviceOptionsNames.MinBitrate:
                case VideoDeviceOptionsNames.MaxFramerate:
                case VideoDeviceOptionsNames.ScaleResolutionDownBy: 
                    return true;
            }
            return false;
        }
    }


    public abstract class MediaDevice : IMediaDevice
    {
        internal abstract IMediaStreamTrack Track { get; }

        public abstract bool isFlippedX { get; }
        public abstract void Dispose();
    }

    public abstract class VideoDevice : MediaDevice
    {
        internal Dictionary<string, object> options;
        public void SetOptions(Dictionary<string, object> options)
        {
            this.options=options;
        }
    }

    public class TextureMediaDevice : VideoDevice
    {
        private String? loggerPrefix;
        Texture texture;
        RainbowVideoStreamTrack track;

        public TextureMediaDevice(Texture texture, String? loggerPrefix)
        {
            this.texture = texture;
            this.track = null;
            this.loggerPrefix = loggerPrefix;
        }
        public override bool isFlippedX { 
            get {
                if (this.texture as RenderTexture == null)
                    return true;
                return false;
            }
        }
        internal override IMediaStreamTrack Track
        {
            get
            {
                if (track != null)
                {
                    return track;
                }
                RainbowVideoStreamTrack result = null;
                UnityExecutor.ExecuteSync(() =>
                {
                    result = RainbowVideoStreamTrack.Wrap(new VideoStreamTrack( texture ), loggerPrefix);
                    result.SetOptions(options);
                });
                track = result;
                return result;
            }
        }

        public override void Dispose()
        {
            track = null;
            texture = null;
        }
    }
    public class WebCamMediaDevice : VideoDevice
    {
        private String? loggerPrefix;
        WebCamDevice? webcamDevice;
        WebCamTexture texture;
        RainbowVideoStreamTrack track;
        int width;
        int height;

        public override bool isFlippedX
        {
            get
            {                
                return true;                
            }
        }

        public WebCamMediaDevice(WebCamDevice? webcamDevice, int width, int height, String? loggerPrefix)
        {
            this.webcamDevice = webcamDevice;
            this.width = width;
            this.height = height;
            track = null;
            this.loggerPrefix = loggerPrefix;
        }

        internal override IMediaStreamTrack Track
        {
            get
            {
                if (track != null) {
                    return track;
                }

                RainbowVideoStreamTrack result = null;
                UnityExecutor.ExecuteSync(() =>
                {
                    texture = new WebCamTexture(webcamDevice?.name, width, height);
                    texture.Play();
                    result = RainbowVideoStreamTrack.Wrap(new VideoStreamTrack(texture), loggerPrefix);
                    result.SetOptions(options);
                });
                track = result;
                return result;
            }
        }

        public override void Dispose()
        {            
            if (texture != null)
            {
                UnityExecutor.Execute(() =>
                {                    
                    texture.Stop();
                    texture = null;
                });
            }  
            track = null;
            webcamDevice = null;
        }
    }
    public class CameraMediaDevice : VideoDevice
    {
        private String? loggerPrefix;
        Camera camera;
        RainbowVideoStreamTrack track;
        int width;
        int height;

        public CameraMediaDevice(Camera camera, int width, int height, String? loggerPrefix ) { 
            this.camera = camera;
            this.width = width;
            this.height = height;
            this.track = null;
            this.loggerPrefix = loggerPrefix;
        }
        public override bool isFlippedX
        {
            get
            {
                return false;
            }
        }

        internal override IMediaStreamTrack Track { get
            {
                if( track != null )
                {
                    return track;
                }
                RainbowVideoStreamTrack result = null;
                UnityExecutor.ExecuteSync(() =>
                {
                    result = RainbowVideoStreamTrack.Wrap(camera.CaptureStreamTrack(width, height),loggerPrefix);
                    result.SetOptions(options);
                });
                track = result;
                return result;
            }
        } 

        public override void Dispose()
        {
            if( camera != null)
            {
                UnityExecutor.Execute(() =>
                {
                    camera.targetTexture = null;
                });
            }
            track = null;
            camera = null;
        }
    }
    public class AudioMediaDevice : MediaDevice
    {
        AudioSource audioSource;
        String? loggerPrefix;

        internal AudioMediaDevice(AudioSource audioSource, String ?loggerPrefix)
        {
            this.audioSource = audioSource;
            this.loggerPrefix = loggerPrefix; 
        }
        public override void Dispose()
        {
            audioSource = null;
        }
        public override bool isFlippedX
        {
            get
            {
                return false;
            }
        }

        internal override IMediaStreamTrack Track
        {
            get
            {
                return RainbowAudioStreamTrack.Wrap(new AudioStreamTrack(audioSource),loggerPrefix);
            }
        }
    }
}
