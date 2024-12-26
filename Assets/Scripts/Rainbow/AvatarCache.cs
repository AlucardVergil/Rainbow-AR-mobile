using Rainbow.WebRTC.Unity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Cortex
{
    public class CachedAvatar
    {
        public string Id;
        public int Size;
        public Texture Texture;

    }

    class AvatarRequest
    {
        public string Id;
        public int Size;
        public List<Action<Texture>> actions;
        public AvatarRequest(string id, int size, Action<Texture> action)
        {
            Id = id;
            Size = size;

            actions = new()
        {
            action
        };
        }
    }

    public class AvatarCache
    {
        ConcurrentDictionary<string, CachedAvatar> ContactAvatars; // keys are: contactid + "_" + size of the avatar
        ConcurrentDictionary<string, AvatarRequest> AvatarRequests;
        RainbowInterface Rainbow;
        private bool terminated = false;
        private Task task;

        public AvatarCache(RainbowInterface rainbow)
        {
            Rainbow = rainbow;
            ContactAvatars = new ConcurrentDictionary<string, CachedAvatar>();
            AvatarRequests = new ConcurrentDictionary<string, AvatarRequest>();
        }

        private void StartAvatarCollection()
        {
            if (task != null)
            {
                task = Task.Factory.StartNew(CollectAvatars, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            }

        }
        private void CollectAvatars()
        {
            while (AvatarRequests.Count != 0)
            {
                string currentKey = AvatarRequests.Keys.First();
                AvatarRequest currentRq; // = AvatarRequests[currentKey];
                AvatarRequests.TryRemove(currentKey, out currentRq);
                if (terminated) return;
                Rainbow.RainbowApplication.GetContacts().GetAvatarFromContactId(currentRq.Id, currentRq.Size, callback =>
                    {

                        UnityExecutor.Execute(() =>
                        {
                            if (callback.Result.Success)
                            {
                                byte[] data = callback.Data;
                                Texture2D texture = new Texture2D(2, 2);
                                if (texture.LoadImage(data))
                                {
                                    ContactAvatars.TryAdd(currentKey, new CachedAvatar() { Texture = texture, Id = currentRq.Id, Size = currentRq.Size });

                                    foreach (var cb in currentRq.actions)
                                    {
                                        if (terminated) return;
                                        cb(texture);
                                    }

                                }
                                else
                                {
                                    Debug.LogError("invalid avatar retrieved for " + currentRq.Id);
                                    ContactAvatars.TryRemove(currentKey, out CachedAvatar prevValue);
                                    foreach (var cb in currentRq.actions)
                                    {
                                        if (terminated) return;
                                        cb(null);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var cb in currentRq.actions)
                                {
                                    if (terminated) return;
                                    cb(null);
                                }
                            }
                        });

                    });
            }
            task = null;
        }

        public void Terminate()
        {
            terminated = true;
            if (task != null)
            {
                task.Wait();
                task = null;
            }
        }
        public void GetContactAvatarTexture(string id, int size, Action<Texture> cb)
        {
            string key = $"{id}_{size}";

            if (ContactAvatars.ContainsKey(key))
            {
                Debug.Log($"found Avatar for {key} {ContactAvatars[key].Texture}");

                UnityExecutor.Execute(() => cb(ContactAvatars[key].Texture));
                return;
            }

            if (AvatarRequests.ContainsKey(key))
            {
                AvatarRequest rq = AvatarRequests[key];
                rq.actions.Add(cb);
            }
            else
            {
                AvatarRequest rq = new AvatarRequest(id, size, cb);
                AvatarRequests[key] = rq;
            }

            if (AvatarRequests.Count > 0)
            {
                StartAvatarCollection();
            }
            Rainbow.RainbowExecutor.Execute(() =>
            {
                Rainbow.RainbowApplication.GetContacts().GetAvatarFromContactId(id, size, callback =>
                {
                    UnityExecutor.Execute(() =>
                    {
                        if (callback.Result.Success)
                        {
                            byte[] data = callback.Data;
                            Texture2D texture = new Texture2D(2, 2);
                            if (texture.LoadImage(data))
                            {
                                ContactAvatars.TryAdd(key, new CachedAvatar() { Texture = texture, Id = id, Size = size });

                                cb(texture);

                            }
                            else
                            {
                                Debug.LogError("invalid avatar retrieved for " + id);
                                ContactAvatars.TryRemove(key, out CachedAvatar prevValue);
                                cb(null);
                            }
                        }
                        else
                        {
                            cb(null);
                        }
                    });
                });
            });
        }
    }

} // End namespace cortex