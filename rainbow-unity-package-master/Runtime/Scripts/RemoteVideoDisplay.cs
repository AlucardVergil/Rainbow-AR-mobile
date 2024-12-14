using Rainbow.WebRTC.Abstractions;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;


namespace Rainbow.WebRTC.Unity
{
    public class RemoteVideoDisplay
    {
        private RawImage image;
        private RainbowVideoStreamTrack track;
        private bool active = true;
        public delegate void ActiveChangedDelegate(bool active);
        public delegate void TextureChangedDelegate(Texture texture);

        public event ActiveChangedDelegate ActiveChanged;
        public event TextureChangedDelegate TextureChanged;
        public RawImage Image
        {
            get => image;
            set
            {
                bool initialValue = active;
                try
                {
                    if (value == null)
                    {
                        image = null;
                        return;
                    }
                    if (image != value)
                    {
                        image = value;
                    }
                }
                finally
                {
                    if (active != initialValue)
                    {
                        UnityExecutor.Execute(() =>
                        {
                            ActiveChanged?.Invoke(active);
                        });
                    }
                }
            }
        }
        public IVideoStreamTrack Track
        {
            get => track;

            set
            {
                if (track != null)
                {
                    clearEvents(this.track);
                }
                track = value as RainbowVideoStreamTrack;

                if (track != null)
                {
                    subscribeToEvents(track);
                    // setImageColor(Color.white);
                }
                if (track == null)
                {
                    Active = false;
                }
            }
        }


        public bool Active
        {
            get => active;
            set
            {
                bool previousValue = active;
                try
                {
                    if (value && image != null && track != null)
                    {
                        active = value;
                    }
                    else
                    {
                        active = false;
                    }
                }
                finally
                {
                    if (active != previousValue)
                    {
                        ActiveChanged?.Invoke(active);


                        if (active && track != null)
                        {
                            VideoStreamTrack realTrack = track.Instance as VideoStreamTrack;
                            if (realTrack != null && realTrack.Texture != null)
                            {
                                RainbowVideoStreamTrack_OnVideoReceived(realTrack.Texture);
                            }
                        }
                    }
                }
            }
        }
        private void subscribeToEvents(RainbowVideoStreamTrack track)
        {
            if (track == null)
                return;

            VideoStreamTrack realTrack = track.Instance as VideoStreamTrack;

            if (realTrack == null)
                return;

            realTrack.OnVideoReceived += RainbowVideoStreamTrack_OnVideoReceived;
            track.TrackRemoved += RainbowVideoStreamTrack_OnTrackRemoved;

            if (realTrack.Texture != null)
            {
                RainbowVideoStreamTrack_OnVideoReceived(realTrack.Texture);
            }

        }

        private void clearEvents(RainbowVideoStreamTrack track)
        {
            if (track == null)
                return;
            track.TrackRemoved -= RainbowVideoStreamTrack_OnTrackRemoved;

            VideoStreamTrack realTrack = track.Instance as VideoStreamTrack;
            if (realTrack == null)
                return;

            realTrack.OnVideoReceived -= RainbowVideoStreamTrack_OnVideoReceived;
        }

        public RemoteVideoDisplay(IVideoStreamTrack track, RawImage image, bool active = true)
        {
            Track = track;
            Image = image;
            Active = active;
        }

        public void Dispose()
        {
            if (image != null)
            {
                image = null;
            }
            if (track != null)
            {
                clearEvents(track);
                track = null;
            }
        }

        private void setImageColor(Color color)
        {
            if (image != null)
            {
                UnityExecutor.Execute(() =>
                {
                    image.color = color;
                });
            }
        }
        internal void RainbowVideoStreamTrack_OnTrackRemoved()
        {
            Active = false;
            // setImageColor(Color.black);

            if (track != null)
            {
                clearEvents(track);
                track = null;
            }
        }

        internal void RainbowVideoStreamTrack_OnVideoReceived(Texture texture)
        {
            //if (texture != null)
            //{
            //    Debug.Log($"Current Texture dimension is {texture.width} {texture.height}");
            //}

            if (!active)
            {
                return;
            }
            if (image == null)
            {
                this.TextureChanged?.Invoke(texture);
                return;
            }
            if (texture != image.texture)
            {
                image.texture = texture;
                this.TextureChanged?.Invoke(texture);
            }
            setImageColor(Color.white);
        }

    }
}
