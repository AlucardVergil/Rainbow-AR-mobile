using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Simple UI script to handle multiple dynamic Users with their videos enabled/disabled
    /// </summary>
    public class ParticipantVideoWindow : BaseStartOrEnabled
    {
        public CallState CallState;

        public MultiRowContainer Content;

        public ContactInitialsAvatar ContactInitialsPrefab;

        public RainbowAvatarLoader RainbowAvatarLoader;

        /// <summary>
        /// Sets a filter for contacts. Only those contacts that pass this and other registered filters will be displayed.
        /// This function will clear the current sets of filters and add the given one.
        /// </summary>
        /// <param name="filter">The filter function</param>
        public void SetFilter(Predicate<Contact> filter)
        {
            filterContacts.Clear();
            filterContacts.Add(filter);
            UpdateVideos();
        }
        /// <summary>
        /// Adds a filter for contacts. Only those contacts that pass this and other registered filters will be displayed.
        /// </summary>
        /// <param name="filter">The filter function</param>
        public void AddFilter(Predicate<Contact> filter)
        {
            filterContacts.Add(filter);
            UpdateVideos();
        }

        private readonly List<Predicate<Contact>> filterContacts = new();

        class VideoEntry
        {
            public Contact Contact;
            public VideoManager Manager;

            public RawImage Video;
            public RawImage Share;

            public GameObject Container;
            public GameObject Name;
        }
        private readonly Dictionary<string, VideoEntry> videoEntries = new();

        private readonly Queue<RawImage> reusableImages = new();

        void Awake()
        {
            if (RainbowAvatarLoader == null)
            {
                RainbowAvatarLoader = FindFirstObjectByType<RainbowAvatarLoader>();
            }
        }

        protected override void OnStartOrEnable()
        {
            UpdateVideos();

            CallState.OnContactChanged += OnContactChanged;
        }

        void OnDisable()
        {
            CallState.OnContactChanged -= OnContactChanged;

        }
        private void OnContactChanged(Contact contact, CallState.ContactChangeType type)
        {
            // TODO only change this one contact
            UpdateVideos();
        }

        public void UpdateVideos()
        {
            Dictionary<string, Contact> toDisplay = new();
            foreach (KeyValuePair<string, Contact> pair in CallState.ConnectedContacts)
            {
                bool valid = true;

                foreach (var f in filterContacts)
                {
                    valid &= f(pair.Value);
                    if (valid == false)
                    {
                        break;
                    }
                }
                if (valid)
                {
                    toDisplay.Add(pair.Key, pair.Value);
                }
            }

            // generate add/update/remove list
            List<Contact> addList = new();
            List<Contact> removeList = new();
            List<Contact> updateList = new();

            foreach (KeyValuePair<string, Contact> pair in toDisplay)
            {
                if (videoEntries.ContainsKey(pair.Key))
                {
                    // contained in both -> update
                    updateList.Add(pair.Value);
                }
                else
                {
                    // new entry
                    addList.Add(pair.Value);
                }
            }

            // check for removals
            foreach (KeyValuePair<string, VideoEntry> pair in videoEntries)
            {
                // the case of the key being in both was already handled
                if (!toDisplay.ContainsKey(pair.Key))
                {
                    removeList.Add(pair.Value.Contact);
                }
            }

            // update data structures

            foreach (var contact in addList)
            {
                // create a new object
                GameObject entry = new("EntryContainer", typeof(RectTransform));
                Content.Add(entry.GetComponent<RectTransform>());

                GameObject entryContent = new("EntryContent", typeof(RectTransform));
                entryContent.GetComponent<RectTransform>().SetParent(entry.transform, false);
                {
                    var fitter = entryContent.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = 1.0f;
                }
                {
                    var entryLayout = entry.AddComponent<LayoutElement>();
                    entryLayout.preferredWidth = 200;
                    entryLayout.preferredHeight = 200;
                    entryLayout.minHeight = 50;
                    entryLayout.minWidth = 50;
                }

                RawImage camImage;
                if (reusableImages.Any())
                {
                    camImage = reusableImages.Dequeue();
                    camImage.gameObject.SetActive(true);
                }
                else
                {
                    camImage = new GameObject("Camera", typeof(RectTransform)).AddComponent<RawImage>();
                    camImage.AddComponent<AspectRatioFitter>();

                }

                // this container will take up the upper 80% of the element
                RectTransform camContainer = new GameObject("Container").AddComponent<RectTransform>();
                camContainer.SetParent(entryContent.transform, false);
                camContainer.anchorMin = new Vector2(0.0f, 0.2f);
                camContainer.anchorMax = new Vector2(1.0f, 1.0f);
                camContainer.offsetMin = new Vector2(5.0f, 5.0f);
                camContainer.offsetMax = new Vector2(-5.0f, -5.0f);

                ContactInitialsAvatar contactInitialsAvatar = Instantiate(ContactInitialsPrefab);
                contactInitialsAvatar.transform.SetParent(camContainer, false);

                contactInitialsAvatar.Contact = contact;

                RectTransform camTransform = camImage.gameObject.GetComponent<RectTransform>();
                var ratioFitter = camImage.GetComponent<AspectRatioFitter>();
                ratioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                camImage.gameObject.transform.SetParent(camContainer, false);

                GameObject textObject = new("Name", typeof(RectTransform));
                var text = textObject.AddComponent<TextMeshProUGUI>();
                textObject.transform.SetParent(entryContent.transform, false);
                text.color = Color.black;
                text.alignment = TextAlignmentOptions.Center;

                // text will take up the lower 20% of the element
                var textTransform = textObject.GetComponent<RectTransform>();
                textTransform.anchorMin = new Vector2(0.0f, 0.0f);
                textTransform.anchorMax = new Vector2(1.0f, 0.2f);
                textTransform.offsetMin = new Vector2(5.0f, 5.0f);
                textTransform.offsetMax = new Vector2(-5.0f, -5.0f);
                text.enableAutoSizing = true;

                text.text = Util.GetContactDisplayName(contact);
                text.fontSizeMin = 0.0f;
                text.fontSizeMax = 100f;

                VideoManager manager = new GameObject($"VideoManager-{Util.GetContactDisplayName(contact)}").AddComponent<VideoManager>();
                manager.VideoImage = camImage;
                manager.SetUserId(contact.Id);
                manager.transform.SetParent(transform, false);
                VideoEntry videoEntry = new()
                {
                    Contact = contact,
                    Container = entry,
                    Video = camImage,
                    Name = textObject,
                    Manager = manager,
                };

                manager.OnRemoteVideoActiveChanged += (m, active) =>
                {
                    camImage.gameObject.SetActive(active);
                    contactInitialsAvatar.gameObject.SetActive(!active);
                };

                videoEntries.Add(contact.Id, videoEntry);

                UpdateContactAvatar(contactInitialsAvatar);
            }

            foreach (var contact in removeList)
            {
                // this is guaranteed to be in here
                videoEntries.Remove(contact.Id, out VideoEntry entry);
                if (entry.Video != null)
                {
                    entry.Video.transform.SetParent(null);
                    entry.Video.texture = null;
                    Content.Remove(entry.Container.GetComponent<RectTransform>());
                    entry.Video.gameObject.SetActive(false);
                    reusableImages.Enqueue(entry.Video);
                }
                Destroy(entry.Container);
                Destroy(entry.Manager.gameObject);
            }
        }

        async void UpdateContactAvatar(ContactInitialsAvatar avatar)
        {
            if (RainbowAvatarLoader == null)
            {
                return;
            }

            Texture2D result = await RainbowAvatarLoader.RequestAvatar(avatar.Contact, 64);
            if (result != null)
            {
                UnityExecutor.Execute(() =>
                {

                    if (avatar != null)
                    {
                        avatar.AvatarImage = result;
                    }
                });
            }
        }
    }

} // end namespace Cortex