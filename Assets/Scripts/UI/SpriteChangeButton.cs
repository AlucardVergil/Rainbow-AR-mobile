using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cortex
{
    [RequireComponent(typeof(Button))]
    public class SpriteChangeButton : BaseStartOrEnabled
    {
        public Sprite Switch;

        private bool _switched = false;
        public bool Switched
        {
            get => _switched;
            set
            {
                _switched = value;

                UpdateImages();
            }
        }

        private Sprite initial;
        private Image image;
        public Button Button
        {
            get; private set;
        }

        public event Action OnClick;

        void Awake()
        {
            Button = GetComponent<Button>();
            image = GetComponent<Image>();
            initial = image.sprite;
        }
        // Start is called before the first frame update
        protected override void OnStartOrEnable()
        {
            Button.onClick.AddListener(OnClickInternal);
            UpdateImages();
        }
        void OnDisable()
        {
            Button.onClick.RemoveListener(OnClickInternal);
        }

        private void OnClickInternal()
        {
            Switched = !Switched;
            OnClick?.Invoke();

            UpdateImages();
        }

        private void UpdateImages()
        {
            if (Switched)
            {
                image.sprite = Switch;
            }
            else
            {
                image.sprite = initial;

            }
            Button.targetGraphic = image;
        }

    }
} // end namespace Cortex