using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    /// <summary>
    /// Script for placing elements in multiple rows
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MultiRowContainer : BaseStartOrEnabled
    {
        [SerializeField]
        [Min(1)]
        private int _itemsPerRow = 3;

        [SerializeField]
        [Min(1)]
        private int _maxRows = 2;
        public int MaxRows
        {
            get => _maxRows;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException($"Maximum number of rows must be non-negative (>=0), but was {value}");
                }
                if (value == _maxRows)
                {
                    return;
                }
                _maxRows = value;
                ReLayoutFrom();
            }
        }
        public int ItemsPerRow
        {
            get => _itemsPerRow;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException($"Item count per row must be positive (>0), but was {value}");
                }
                if (value == _itemsPerRow)
                {
                    return;
                }
                _itemsPerRow = value;
                ReLayoutFrom();
            }
        }

        private readonly List<HorizontalLayoutGroup> rows = new();

        private class Entry
        {
            public RectTransform transform;
            public int currentRow;
        }
        private readonly List<Entry> entries = new();
        private readonly Dictionary<RectTransform, int> entryPositions = new();

        private CanvasGroup canvasGroup;

        private readonly List<VerticalLayoutGroup> pages = new();

        [SerializeField]
        private GameObject Content;

        [SerializeField]
        private GameObject Navigation;
        [SerializeField]
        private Button ButtonPrev;
        [SerializeField]
        private Button ButtonNext;
        [SerializeField]
        private TMP_Text PageText;

        public int VisiblePage
        {
            get;
            private set;
        }

        private VerticalLayoutGroup CreatePage()
        {
            VerticalLayoutGroup page = new GameObject("Page").AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = false;

            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            page.GetOrAddComponent<RectTransform>();

            return page;
        }
        private void ReLayoutFrom(int index = 0)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            int numRows = (entries.Count + ItemsPerRow - 1) / ItemsPerRow;

            int numPages = (numRows + MaxRows - 1) / MaxRows;

            while (pages.Count < numPages)
            {
                var page = CreatePage();
                var t = page.GetComponent<RectTransform>();
                t.SetParent(Content.transform, false);

                t.anchorMin = new Vector2(0.0f, 0.0f);
                t.anchorMax = new Vector2(1.0f, 1.0f);
                t.offsetMin = new Vector2(0.0f, 0.0f);
                t.offsetMax = new Vector2(0.0f, 0.0f);
                pages.Add(page);
            }
            while (rows.Count < numRows)
            {
                // add new row
                var rowObj = new GameObject("Row").AddComponent<HorizontalLayoutGroup>();
                rowObj.childAlignment = TextAnchor.MiddleCenter;
                rowObj.childControlWidth = false;
                rowObj.childControlHeight = false;
                rowObj.childForceExpandWidth = true;
                rowObj.childForceExpandHeight = false;

                rows.Add(rowObj);
            }

            for (int i = index; i < entries.Count; i++)
            {
                Entry ei = entries[i];
                int row = i / ItemsPerRow;
                if (row != ei.currentRow)
                {
                    var rowObj = rows[row];
                    ei.currentRow = row;
                    ei.transform.SetParent(rowObj.transform, false);
                }
            }

            // if we have more rows than needed, remove them
            while (rows.Count > numRows)
            {
                var obj = rows.Last();
                Destroy(obj.gameObject);
                rows.RemoveAt(rows.Count - 1);
            }

            // reparent rows to pages and create, if needed

            for (int i = 0; i < rows.Count; i++)
            {
                int pageIdx = i / MaxRows;
                rows[i].transform.SetParent(pages[pageIdx].transform, false);
            }

            // remove now empty pages
            while (pages.Count > numPages)
            {
                var obj = pages.Last();
                Destroy(obj.gameObject);
                pages.RemoveAt(pages.Count - 1);
            }

            if (numPages > 1)
            {
                Navigation.SetActive(true);
            }
            else
            {
                Navigation.SetActive(false);
            }

            // clamp active page to current last one
            SetVisiblePage(Math.Min(VisiblePage, numPages - 1));

            if (entries.Any())
            {
                canvasGroup.alpha = 1.0f;
            }
            else
            {
                canvasGroup.alpha = 0.0f;
            }

            LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
        }

        public void SetVisiblePage(int pageIndex)
        {
            if (pages.Count == 0)
            {
                return;
            }

            VisiblePage = pageIndex;
            VisiblePage = Math.Clamp(VisiblePage, 0, pages.Count - 1);
            // could be made more efficient
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].gameObject.SetActive(i == VisiblePage);
            }

            PageText.text = $"Page {VisiblePage + 1}";

            ButtonPrev.interactable = VisiblePage > 0;
            ButtonNext.interactable = VisiblePage + 1 < pages.Count;

        }
        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            VisiblePage = 0;
        }

        protected override void OnStartOrEnable()
        {
            ButtonPrev.onClick.AddListener(OnPrevClick);
            ButtonNext.onClick.AddListener(OnNextClick);

            ReLayoutFrom();
        }

        void OnDisable()
        {
            ButtonPrev.onClick.RemoveListener(OnPrevClick);
            ButtonNext.onClick.RemoveListener(OnNextClick);
        }
        private void OnNextClick()
        {
            SetVisiblePage(Math.Min(pages.Count - 1, VisiblePage + 1));
        }

        private void OnPrevClick()
        {
            SetVisiblePage(Math.Max(0, VisiblePage - 1));
        }

        public void Add(RectTransform transform)
        {
            if (entryPositions.ContainsKey(transform))
            {
                Debug.LogWarning($"[MultiRowContainer] Trying to add object multiple times: {transform.name}");
                return;
            }

            int index = entries.Count;

            entries.Add(new Entry()
            {
                currentRow = -1,
                transform = transform
            });
            entryPositions.Add(transform, index);

            ReLayoutFrom(index);

            return;
        }

        public void Remove(RectTransform transform)
        {
            if (!entryPositions.TryGetValue(transform, out int index))
            {
                return;
            }

            Entry e = entries[index];

            // remove entry and update the ones after it
            entries.RemoveAt(index);
            entryPositions.Remove(transform);
            // remove parent
            e.transform.SetParent(null);

            // reduce indices of all entries after this
            for (int i = index; i < entries.Count; i++)
            {
                Entry ei = entries[i];
                entryPositions[ei.transform] = i;
            }

            ReLayoutFrom(index);
        }
    }
} // end namespace Cortex