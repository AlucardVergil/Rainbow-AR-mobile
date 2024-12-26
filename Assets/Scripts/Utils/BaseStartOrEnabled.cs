using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// This is a helper base class to have a symmetric counterpart to OnDisable.
    /// While OnEnable exists, it isn't symmetric to OnDisable, since OnStart is called after it, but only once. Thus, for elements that are shown and hidden, like UI, the lifecycle requires extra care. This class provides an additional OnStartOrEnable method to aid with that.
    /// </summary>
    public class BaseStartOrEnabled : MonoBehaviour
    {
        /// <summary>
        /// Shows whether the component has already started
        /// </summary>
        protected bool HasStarted = false;

        /// <summary>
        /// The usual unity Start method. If the implementation wants to override this method, this base method should be called.
        /// </summary>
        protected virtual void Start()
        {
            HasStarted = true;

            OnStartOrEnable();
        }

        /// <summary>
        /// The usual unity OnEnable method. If the implementation wants to override this method, this base method should be called.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (!HasStarted)
            {
                return;
            }

            OnStartOrEnable();
        }

        /// <summary>
        /// This will be called every time the component becomes "active". When becoming active for the first time, this will be called on Start, otherwise on OnEnable.
        /// </summary>
        protected virtual void OnStartOrEnable()
        {

        }

    }

} // end namespace Cortex