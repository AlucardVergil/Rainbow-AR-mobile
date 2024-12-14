using Rainbow.WebRTC.Abstractions;
using System;
using UnityWebRTC = Unity.WebRTC;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rainbow.WebRTC.Unity
{
    internal enum TriState
    {
        Yes, Unknown, No
    }
    internal class RainbowVideoStreamTrack : RainbowMediaStreamTrack, IVideoStreamTrack
    {
        internal Dictionary<string, Object> options;
        static internal new RainbowVideoStreamTrack Wrap(UnityWebRTC.MediaStreamTrack track, String ?loggerPrefix, bool IsEmptyTrack=false)
        {
            if (track == null)
            {
                return null;
            }
            RainbowVideoStreamTrack result = new RainbowVideoStreamTrack()
            {
                isEmpty = IsEmptyTrack,
                log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix),
                wrapped = track
            };
             
            return result;
        }

        internal void SetOptions(Dictionary<string,Object> options)
        {
            this.options = options;
            if( options != null )
            log.LogError("Setoption to RainbowVideoStreamTrack: " + String.Join(" ", options.Keys));
        }
    }


    internal class RainbowAudioStreamTrack : RainbowMediaStreamTrack, IAudioStreamTrack, IMediaStreamTrack
    {
        static internal new RainbowAudioStreamTrack Wrap(UnityWebRTC.MediaStreamTrack track, String ?loggerPrefix) 
        {
            if (track == null)
            {
                return null;
            }
            RainbowAudioStreamTrack result = new RainbowAudioStreamTrack();            
            result.log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
             
            result.wrapped = track;
            return result;
        }
        public override void Mute()
        {
            log.LogDebug("Mute Audio");
            if (Muted)
                return;

            Muted = true;
            UnityWebRTC.AudioStreamTrack t = wrapped as UnityWebRTC.AudioStreamTrack;
            if ( t.Source != null)
            {
                UnityExecutor.Execute(() =>
                {
                    t.Source.mute = true;
                });
            } else
            {
                log.LogError("no source");
            }
        }

        public override void Unmute()
        {
            log.LogDebug("UnMute Audio");
            if (!Muted)
                return;


            Muted = false;
            UnityWebRTC.AudioStreamTrack t = wrapped as UnityWebRTC.AudioStreamTrack;
            if(t == null)
            {
                log.LogError("Critical error: rainbowaudiostreamtrack is not wrapping an AudioStreamTrack");
                return;
            }
            if (t.Source != null)
            {
                UnityExecutor.Execute(() =>
                {
                    t.Source.mute = false;
                });
            }
            else
            {
                log.LogError("no source");
            }
        }

    }

    internal abstract class RainbowMediaStreamTrack : IMediaStreamTrack
    {
        protected Microsoft.Extensions.Logging.ILogger log;
        protected UnityWebRTC.MediaStreamTrack wrapped;

        public string Id => wrapped.Id;
        internal UnityWebRTC.MediaStreamTrack Instance => wrapped;
        private bool muted;
        internal bool isEmpty;
        internal delegate void TrackRemovedDelegate();
        internal event TrackRemovedDelegate TrackRemoved;

        internal void RaiseTrackRemoved()
        {
            try
            {
                this.TrackRemoved?.Invoke();
            } 
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }
        static internal RainbowMediaStreamTrack Wrap(UnityWebRTC.MediaStreamTrack track, String? loggerPrefix )
        {
            if (track == null)
            {
                return null;
            }

            if(track.Kind == UnityWebRTC.TrackKind.Audio)
            {
                var result = new RainbowAudioStreamTrack();
                result.log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
                result.muted = false;
                result.wrapped = track;
                return result;
            }
            var resultVideo = new RainbowVideoStreamTrack();
            resultVideo.log = Abstractions.LogFactory.CreateWebRTCLogger(loggerPrefix);
            resultVideo.muted = false;
            resultVideo.wrapped = track;
            return resultVideo;
        }

        public Abstractions.TrackKind Kind {
            get => Helpers.TrackKind(wrapped.Kind); 
        }

        public ReadyState ReadyState => Helpers.ReadyState(wrapped.ReadyState);

        public bool Muted { get => muted; internal set { muted = value; } }

        public void Dispose()
        {
            wrapped.Dispose();
        }

        public void Stop()
        {
            wrapped.Stop();
        }

        public virtual void Mute()
        {
            log.LogDebug("Mute base");
            // Muted = true;            
         }

        public virtual void Unmute()
        {
            log.LogDebug("UnMute base");
            // Muted = false;
        }

    }
}
