using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cortex
{
    public class Clickable : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Event that is called when the element this script is attached to is clicked
        /// </summary>
        public event Action<PointerEventData> OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(eventData);
        }
    }
} // end namespace Cortex