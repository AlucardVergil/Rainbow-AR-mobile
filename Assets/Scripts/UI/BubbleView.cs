using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using Rainbow.WebRTC.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Basic view of a all bubbles of a user and details for a selected one, including members
    /// </summary>
    public class BubbleView : BaseStartOrEnabled
    {
        [Header("Options")]
        [Range(1, 512)]
        public int BubbleAvatarResolution = 64;
        // This script currently does a bit much and could be broken up into more manageable tasks
        public RainbowAvatarLoader AvatarLoader;

        public BubbleListEntry ListEntryTemplate;
        public RectTransform ContentPanel;

        public BubbleDetails BubbleDetails;
        private List<Bubble> curBubbles;

        private Bubble selectedBubble;
        private readonly object bubbleLock = new();

        private readonly Dictionary<string, BubbleListEntry> bubbleEntries = new();
        private readonly Dictionary<string, ResetCancellationToken> bubbleEntryTokens = new();

        // vagelis
        public GameObject bubblesGameobject;
        public GameObject rainbowGameobject;


        override protected void OnStartOrEnable()
        {
            // vagelis


            bubblesGameobject.SetActive(true);



            // end vagelis

            if (AvatarLoader == null)
            {
                AvatarLoader = FindFirstObjectByType<RainbowAvatarLoader>();
            }

            // bubbleDetailAvatar = GameObjectUtils.FindGameObjectByName(BubbleDetails.transform, "BubbleImage", true).GetComponent<RawImage>();

            Conferences conferences = ConnectionModel.Instance.Conferences;
            Bubbles bubbles = ConnectionModel.Instance.Bubbles;

            if (conferences != null)
            {
                // when someone starts a call, we need to update the selected bubble, in case we are allowed to join
                // this event does NOT trigger, when the conference is stopped. In that case, only the ConferenceRemoved event is raised
                conferences.ConferenceUpdated += OnConferenceUpdated;
                conferences.ConferenceRemoved += OnConferenceRemoved;
            }

            // we catch the bubble avatar changed event, which is triggered after the specific bubble has been refreshed
            AvatarLoader.OnBubbleAvatarChanged += OnBubbleAvatarUpdated;

            UpdateUi();

            SelectBubble(null);
        }

        private void OnConferenceUpdated(object sender, ConferenceEventArgs e)
        {
            Conferences conferences = ConnectionModel.Instance.Conferences;
            Bubble b = conferences.GetBubbleByConferenceIdFromCache(e.Conference.Id);

            if (bubbleEntries.TryGetValue(b.Id, out var entry))
            {
                UnityExecutor.Execute(() =>
                {
                    entry.Active = e.Conference.Active;
                });
            }
        }

        private async void UpdateUi()
        {
            if (!HasStarted)
            {
                return;
            }

            var model = ConnectionModel.Instance;

            var conferences = model.Conferences;

            // we could use more performant methods to reuse panels and data
            foreach (Transform child in ContentPanel)
            {
                Destroy(child.gameObject);
            }

            Bubbles bubbles = model.Bubbles;
            curBubbles = bubbles.GetAllBubblesFromCache();

            // stop all current fetch operations
            foreach (var (id, tk) in bubbleEntryTokens)
            {
                tk.Reset();
            }

            bubbleEntryTokens.Clear();
            bubbleEntries.Clear();

            List<Conference> confList = conferences.ConferenceGetListFromCache();
            HashSet<string> activeConf = new();
            foreach (var c in confList)
            {
                if (c.Active)
                {
                    activeConf.Add(c.Id);
                }
            }

            List<Task> avatarTasks = new();
            foreach (var b in curBubbles)
            {
                BubbleListEntry entry = Instantiate(ListEntryTemplate, ContentPanel);
                entry.gameObject.SetActive(true);
                bubbleEntries.Add(b.Id, entry);

                // create tokens to cancel update operations, if we start anew
                var cancelToken = new ResetCancellationToken();
                bubbleEntryTokens.Add(b.Id, cancelToken);
                var token = cancelToken.Token;
                var conf = conferences.GetConferenceIdByBubbleIdFromCache(b.Id);

                entry.Active = activeConf.Contains(conf);

                RawImage avatar = entry.Avatar;

                // update and ignore task
                avatarTasks.Add(UpdateAvatar(b, BubbleAvatarResolution, avatar, token));

                entry.Text.text = b.Name;

                Bubble cur = b;
                entry.OnClick += (e) =>
                {
                    rainbowGameobject.GetComponent<BubbleManager>().DisplayConversationsWithBubble(bubbles.GetBubbleByIdFromCache(cur.Id));

                    lock (bubbleLock)
                    {
                        // as bubble instances in the cache are replaced and not updated, we need to get the current one to propagate avatar updates
                        SelectBubble(bubbles.GetBubbleByIdFromCache(cur.Id));
                    }                    
                };
            }

            try
            {
                await Task.WhenAll(avatarTasks);
            }
            catch (OperationCanceledException)
            {
                // canceled -> nothing to do
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void SelectBubble(Bubble b)
        {
            if (b == null)
            {
                BubbleDetails.gameObject.SetActive(false);

                return;
            }

            if (selectedBubble != null)
            {
                if (bubbleEntries.TryGetValue(selectedBubble.Id, out var lb))
                {
                    lb.Selected = false;
                }
            }

            selectedBubble = b;
            BubbleDetails.gameObject.SetActive(true);

            BubbleDetails.SelectedBubble = b;
        }

        async Task UpdateAvatar(Bubble b, int size, RawImage image, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await AvatarLoader.UpdateAvatarImage(b, size, image, setAlpha: true, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // canceled -> nothing to do
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void OnDisable()
        {
            if (!HasStarted)
            {
                return;
            }

            Conferences conferences = ConnectionModel.Instance.Conferences;
            Bubbles bubbles = ConnectionModel.Instance.Bubbles;

            if (conferences != null)
            {
                conferences.ConferenceUpdated -= OnConferenceUpdated;
                conferences.ConferenceRemoved -= OnConferenceRemoved;
            }

            AvatarLoader.OnBubbleAvatarChanged -= OnBubbleAvatarUpdated;

            SelectBubble(null);

            // cancel still running operations
            foreach (var (id, tk) in bubbleEntryTokens)
            {
                tk.Reset();
            }
        }

        private async void OnBubbleAvatarUpdated(Bubble b, RainbowAvatarLoader.AvatarChange change)
        {
            if (bubbleEntries.TryGetValue(b.Id, out var entry) && bubbleEntryTokens.TryGetValue(b.Id, out var tk))
            {
                tk.Reset();

                var token = tk.Token;

                try
                {
                    await UpdateAvatar(b, BubbleAvatarResolution, entry.Avatar, token);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == token)
                {
                    // canceled -> nothing to do
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private void OnConferenceRemoved(object sender, IdEventArgs e)
        {
            Conferences conferences = ConnectionModel.Instance.Conferences;
            Bubble b = conferences.GetBubbleByConferenceIdFromCache(e.Id);

            if (bubbleEntries.TryGetValue(b.Id, out var entry))
            {
                UnityExecutor.Execute(() =>
                {
                    entry.Active = false;
                });
            }
        }
    }
} // end namespace Cortex