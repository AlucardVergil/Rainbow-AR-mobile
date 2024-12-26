using System.Collections.Generic;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Prints out changes happening in a DistributedStorage
    /// </summary>
    public class DebugLogDistributedStorage : MonoBehaviour
    {

        [SerializeField]
        private DistributedStorage DistributedStorage;
        // Start is called before the first frame update
        void Start()
        {
            if (DistributedStorage == null)
            {
                DistributedStorage = FindFirstObjectByType<DistributedStorage>();
            }
            if (DistributedStorage != null)
            {
                DistributedStorage.OnChange += OnChange;
                // this will show the initial state
                DistributedStorage.CallbackCurrentState(OnChange);
            }
        }

        void OnDestroy()
        {
            if (DistributedStorage != null)
            {
                DistributedStorage.OnChange -= OnChange;
            }
        }

        private void OnChange(IReadOnlyDictionary<string, string> state, string key, string value, DistributedStorage.ChangeType type)
        {
            Debug.Log($"[DebugLogDistributedStorage] Key: {key}, value: {value}, type: {type}");
        }
    }
} // end namespace Cortex