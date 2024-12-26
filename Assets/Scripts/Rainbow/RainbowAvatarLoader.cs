using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Script to handle loading and caching Rainbow avatars for Contacts and Bubbles.
    /// </summary>
    public class RainbowAvatarLoader : MonoBehaviour
    {
        #region public events

        /// <summary>
        /// Represents a change in the avatar of either a Contact or a Bubble
        /// </summary>
        public enum AvatarChange
        {
            Updated, Removed
        }
        /// <summary>
        /// Event handler for changes to a Contact avatar
        /// </summary>
        /// <param name="c">The Contact for which the avatar was changed</param>
        /// <param name="change">The type of change that occurred</param>
        public delegate void PeerAvatarChangedHandler(Contact c, AvatarChange change);
        /// <summary>
        /// Called whenever the avatar of a Contact changes on the server.
        /// Before this event is called, the current contact information is fetched from the server to update the cache
        /// </summary>
        public event PeerAvatarChangedHandler OnPeerAvatarChanged;
        /// <summary>
        /// Event handler for changes to a Bubble avatar
        /// </summary>
        /// <param name="b">The Bubble for which the avatar was changed</param>
        /// <param name="change">The type of change that occurred</param>
        public delegate void BubbleAvatarChangedHandler(Bubble b, AvatarChange change);
        /// <summary>
        /// Called whenever the avatar of a Bubble changes on the server.
        /// Before this event is called, the current Bubble information is fetched from the server to update the cache. 
        /// At the moment, bubble avatars won't be removed, just updated.
        /// </summary>
        public event BubbleAvatarChangedHandler OnBubbleAvatarChanged;

        #endregion // public events

        #region public methods

        /// <summary>
        /// Update a RawImage to contain the avatar of the given contact, if an avatar exists. Optionally, this function sets alpha and active states depending on whether an avatar exists.
        /// </summary>
        /// <param name="contact">The contact to query the avatar for</param>
        /// <param name="size">The size of the avatar (The image will be square)</param>
        /// <param name="image">The image to update</param>
        /// <param name="overwrite">If true, will query a new avatar from the server, even if it is in the cache already</param>
        /// <param name="setActive">If true, the active state of image will be changed</param>
        /// <param name="setAlpha">If true, the alpha value of the image will be changed</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task object that finishes, when the update operation is finished</returns>
        public async Task UpdateAvatarImage(Contact contact, int size, RawImage image, bool overwrite = false, bool setActive = false, bool setAlpha = false, CancellationToken cancellationToken = default)
        {
            await UpdateAvatarImage(RequestAvatar(contact, size, overwrite, cancellationToken), image, setActive, setAlpha, cancellationToken);
        }

        /// <summary>
        /// Update a RawImage to contain the avatar of the given bubble, if an avatar exists. Optionally, this function sets alpha and active states depending on whether an avatar exists.
        /// </summary>
        /// <param name="bubble">The bubble to query the avatar for</param>
        /// <param name="size">The size of the avatar (The image will be square)</param>
        /// <param name="image">The image to update</param>
        /// <param name="overwrite">If true, will query a new avatar from the server, even if it is in the cache already</param>
        /// <param name="setActive">If true, the active state of image will be changed</param>
        /// <param name="setAlpha">If true, the alpha value of the image will be changed</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task object that finishes, when the update operation is finished</returns>
        public async Task UpdateAvatarImage(Bubble bubble, int size, RawImage image, bool overwrite = false, bool setActive = false, bool setAlpha = false, CancellationToken cancellationToken = default)
        {
            await UpdateAvatarImage(RequestAvatar(bubble, size, overwrite, cancellationToken), image, setActive, setAlpha, cancellationToken);
        }

        /// <summary>
        /// Requests a texture object of the avatar for the given contact.
        /// </summary>
        /// <param name="contact">The contact to query the avatar for</param>
        /// <param name="size">The size of the avatar (The image will be square)</param>
        /// <param name="overwrite">If true, will query a new avatar from the server, even if it is in the cache already</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The texture or null, if no avatar exists</returns>
        public async Task<Texture2D> RequestAvatar(Contact contact, int size, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contacts = ConnectionModel.Instance.Contacts;

            string dataSubDir = DataUtils.GetDataDirectory();
            string dataPath = Path.Combine(dataSubDir, "avatars", "contacts");

            // user has no avatar
            if (contact.LastAvatarUpdateDate == DateTime.MinValue)
            {
                return null;
            }

            Texture2D tex = await RequestAvatar(contact.Id, size, contactAvatars, contactLock, async (id, size, token) =>
            {
                string filePath = Path.Combine(dataPath, $"{contact.Id}_{size}.png");
                // try loading the file first
                Texture2D result = await LoadFromFile(filePath, contact.LastAvatarUpdateDate, cancellationToken);

                if (result != null)
                {
                    return result;
                }

                token.ThrowIfCancellationRequested();
                contacts.GetAvatarFromContactId(contact.Id, size, RainbowUtils.WrapSdkResult(out Task<byte[]> taskByte));
                byte[] data;
                try
                {
                    data = await taskByte;
                }
                catch (Exception e)
                {
                    // this only seems to happen, if there is no avatar
                    Debug.LogException(e);
                    return null;
                }

                result = await HandleTextureData(data, token);

                if (result != null)
                {
                    TaskCompletionSource<bool> writeTask = new();
                    // save to file
                    UnityExecutor.Execute(() =>
                    {
                        try
                        {
                            var imgData = result.EncodeToPNG();
                            Directory.CreateDirectory(dataPath);
                            File.WriteAllBytes(filePath, imgData);

                            writeTask.SetResult(true);
                        }
                        catch (Exception)
                        {
                            writeTask.SetResult(false);
                        }

                    });

                    var writeResult = await writeTask.Task;

                }

                return result;

            }, overwrite, cancellationToken);

            return tex;
        }

        /// <summary>
        /// Requests a texture object of the avatar for the given bubble.
        /// </summary>
        /// <param name="bubble">The bubble to query the avatar for</param>
        /// <param name="size">The size of the avatar (The image will be square)</param>
        /// <param name="overwrite">If true, will query a new avatar from the server, even if it is in the cache already</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The texture or null, if no avatar exists</returns>
        public async Task<Texture2D> RequestAvatar(Bubble bubble, int size, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bubbles = ConnectionModel.Instance.Bubbles;

            string dataSubDir = DataUtils.GetDataDirectory();
            string dataPath = Path.Combine(dataSubDir, "avatars", "bubbles");
            // bubble has no avatar
            if (bubble.LastAvatarUpdateDate == DateTime.MinValue)
            {
                return null;
            }

            return await RequestAvatar(bubble.Id, size, bubbleAvatars, bubbleLock, async (id, size, token) =>
            {
                // try loading the file first
                string filePath = Path.Combine(dataPath, $"{bubble.Id}_{size}.png");

                Texture2D result = await LoadFromFile(filePath, bubble.LastAvatarUpdateDate, cancellationToken);

                if (result != null)
                {
                    return result;
                }

                token.ThrowIfCancellationRequested();
                bubbles.GetAvatarFromBubbleId(bubble.Id, size, RainbowUtils.WrapSdkResult(out Task<byte[]> taskByte));
                byte[] data;
                try
                {
                    data = await taskByte;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }

                result = await HandleTextureData(data, token);

                if (result != null)
                {
                    TaskCompletionSource<bool> writeTask = new();
                    // save to file
                    UnityExecutor.Execute(() =>
                    {
                        try
                        {
                            var imgData = result.EncodeToPNG();
                            Directory.CreateDirectory(dataPath);
                            File.WriteAllBytes(filePath, imgData);
                            writeTask.SetResult(true);
                        }
                        catch (Exception)
                        {
                            writeTask.SetResult(false);
                        }
                    });

                    var writeResult = await writeTask.Task;
                }
                return result;

            }, overwrite, cancellationToken);
        }

        #endregion // public methods

        #region private fields and definitions

        private class AvatarKey
        {
            public string Id { get; private set; }
            public int Size { get; private set; }

            public AvatarKey(string id, int size)
            {
                Id = id;
                Size = size;
            }

            public class Comparator : IEqualityComparer<AvatarKey>
            {
                public bool Equals(AvatarKey x, AvatarKey y)
                {

                    return x.Id == y.Id && x.Size == y.Size;
                }

                public int GetHashCode(AvatarKey obj)
                {
                    return HashCode.Combine(obj.Id.GetHashCode(), obj.Size.GetHashCode());
                }
            }

        }

        private class AvatarValue
        {
            public Task<Texture2D> TextureTask;
        }

        private readonly Dictionary<string, Dictionary<int, AvatarValue>> contactAvatars = new();
        private readonly object contactLock = new();
        private readonly Dictionary<string, Dictionary<int, AvatarValue>> bubbleAvatars = new();
        private readonly object bubbleLock = new();

        #endregion // private fields and definitions

        #region private methods

        private async Task<Texture2D> LoadFromFile(string path, DateTime lastUpdate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(path);
                if (lastWriteTime >= lastUpdate)
                {
                    var rawData = await File.ReadAllBytesAsync(path, cancellationToken);

                    TaskCompletionSource<Texture2D> task = new();
                    // graphics related operations need to run on the unity thread
                    UnityExecutor.Execute(() =>
                    {
                        var result = new Texture2D(1, 1);
                        if (result.LoadImage(rawData))
                        {
                            task.SetResult(result);
                        }
                        else
                        {
                            task.SetResult(null);
                        }
                    });

                    return await task.Task;

                }
            }

            return null;
        }

        private async Task<Texture2D> HandleTextureData(byte[] result, CancellationToken cancellationToken)
        {
            TaskCompletionSource<Texture2D> task = new();

            using var reg = cancellationToken.Register(() =>
            {
                task.TrySetCanceled();
            });

            UnityExecutor.Execute(() =>
            {
                if (result != null)
                {
                    Texture2D texture = new(1, 1);

                    if (texture.LoadImage(result))
                    {

                        task.TrySetResult(texture);
                    }
                    else
                    {
                        task.TrySetException(new Exception($"[RainbowAvatarLoader] Invalid avatar data"));

                    }
                }
                else
                {
                    // if there is no avatar, we get null
                    task.TrySetResult(null);
                }
            });

            return await task.Task;
        }

        private async Task UpdateAvatarImage(Task<Texture2D> texTask, RawImage image, bool setActive, bool setAlpha, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // immediately show/hide
                UnityExecutor.Execute(() =>
                {
                    if (image == null)
                    {
                        return;
                    }
                    if (setActive)
                    {
                        image.gameObject.SetActive(false);
                    }
                    if (setAlpha)
                    {
                        var c = image.color;
                        c.a = 0.0f;
                        image.color = c;
                    }

                });
                Texture2D tex = await texTask;

                UnityExecutor.Execute(() =>
                {
                    // since the execution is delayed, the image might not be available anymore, so we need to check
                    if (image != null)
                    {
                        if (tex != null)
                        {
                            image.texture = tex;
                            if (setActive)
                            {
                                image.gameObject.SetActive(true);
                            }

                            if (setAlpha)
                            {
                                var c = image.color;
                                c.a = 1.0f;
                                image.color = c;
                            }
                        }
                        else
                        {
                            if (setActive)
                            {
                                image.gameObject.SetActive(false);
                            }
                            if (setAlpha)
                            {
                                var c = image.color;
                                c.a = 0.0f;
                                image.color = c;
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // cancel will be bubbled up
                throw;
            }
            catch (Exception e)
            {

                Debug.LogException(e);
                throw;
            }

        }

        private async Task<Texture2D> RequestAvatar(string id, int size, Dictionary<string, Dictionary<int, AvatarValue>> dict, object lockObject, Func<string, int, CancellationToken, Task<Texture2D>> loader, bool overwrite, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            Task<Texture2D> task = null;
            // check if request already exists
            lock (lockObject)
            {

                // if we overwrite we will always use a new task, otherwise we will take one if it already exists
                if (!overwrite)
                {
                    if (dict.TryGetValue(id, out Dictionary<int, AvatarValue> values))
                    {
                        if (values.TryGetValue(size, out var value))
                        {
                            task = value.TextureTask;
                        }
                    }
                }

                // no current task -> make a new one
                if (task == null)
                {
                    task = loader(id, size, cancellationToken);

                    // insert task into cache
                    if (dict.TryGetValue(id, out Dictionary<int, AvatarValue> values))
                    {
                        values[size] = new AvatarValue()
                        {
                            TextureTask = task
                        };
                    }
                    else
                    {
                        values = new()
                        {
                            [size] = new AvatarValue()
                            {
                                TextureTask = task
                            }
                        };
                        dict.Add(id, values);
                    }

                }

            }

            try
            {
                await task;
                return task.Result;
            }
            catch (OperationCanceledException)
            {
                // remove from list
                lock (lockObject)
                {
                    if (dict.TryGetValue(id, out Dictionary<int, AvatarValue> values))
                    {
                        values.Remove(size);
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            catch (Exception e)
            {
                // remove from list
                lock (lockObject)
                {
                    if (dict.TryGetValue(id, out Dictionary<int, AvatarValue> values))
                    {
                        values.Remove(size);
                    }
                }
                Debug.LogException(e);
                return null;
            }
        }

        private void OnLogout(ConnectionModel model)
        {
            // make sure that bubbles and contacts exist
            var bubbles = ConnectionModel.Instance.Bubbles;
            if (bubbles != null)
            {
                bubbles.BubbleAvatarUpdated -= OnBubbleAvatarUpdated;
            }

            var contacts = ConnectionModel.Instance.Contacts;
            if (contacts != null)
            {
                contacts.PeerAvatarChanged -= OnPeerAvatarUpdated;
                contacts.PeerAvatarDeleted -= OnPeerAvatarDeleted;
            }
        }

        private void OnLogin(ConnectionModel model)
        {
            // make sure that bubbles and contacts exist
            var bubbles = ConnectionModel.Instance.Bubbles;
            if (bubbles != null)
            {
                bubbles.BubbleAvatarUpdated += OnBubbleAvatarUpdated;
            }

            var contacts = ConnectionModel.Instance.Contacts;
            if (contacts != null)
            {
                contacts.PeerAvatarChanged += OnPeerAvatarUpdated;
                contacts.PeerAvatarDeleted += OnPeerAvatarDeleted;
            }
        }

        private async void OnPeerAvatarDeleted(object sender, PeerEventArgs e)
        {
            lock (contactLock)
            {
                contactAvatars.Remove(e.Peer.Id);
            }
            // to update the contact cache... apparently this doesn't happen automatically and the last avatar update is wrong
            ConnectionModel.Instance.Contacts.GetContactFromContactIdFromServer(e.Peer.Id, RainbowUtils.WrapSdkResult<Contact>(out var contactTask));
            var c = await contactTask;

            UnityExecutor.Execute(() =>
            {
                OnPeerAvatarChanged(c, AvatarChange.Removed);
            });

        }

        private async void OnPeerAvatarUpdated(object sender, PeerEventArgs e)
        {
            lock (contactLock)
            {
                contactAvatars.Remove(e.Peer.Id);
            }
            // to update the contact cache... apparently this doesn't happen automatically and the last avatar update is wrong
            ConnectionModel.Instance.Contacts.GetContactFromContactIdFromServer(e.Peer.Id, RainbowUtils.WrapSdkResult<Contact>(out var contactTask));
            var c = await contactTask;

            UnityExecutor.Execute(() =>
            {
                OnPeerAvatarChanged(c, AvatarChange.Updated);
            });
        }

        private async void OnBubbleAvatarUpdated(object sender, BubbleAvatarEventArgs e)
        {
            lock (bubbleLock)
            {
                bubbleAvatars.Remove(e.BubbleId);
            }
            // to update the bubble cache... apparently this doesn't happen automatically and the last avatar update is wrong
            ConnectionModel.Instance.Bubbles.GetBubbleById(e.BubbleId, RainbowUtils.WrapSdkResult<Bubble>(out var bubbleTask));
            var b = await bubbleTask;

            UnityExecutor.Execute(() =>
            {
                OnBubbleAvatarChanged(b, AvatarChange.Updated);
            });
        }

        #endregion // private methods

        #region unity lifecycle
        void Start()
        {
            ConnectionModel.OnLogin += OnLogin;
            ConnectionModel.OnLogout += OnLogout;
        }

        void OnDestroy()
        {
            ConnectionModel.OnLogin -= OnLogin;
            ConnectionModel.OnLogout -= OnLogout;
        }

        #endregion // unity lifecycle

    }
} // end namespace Cortex