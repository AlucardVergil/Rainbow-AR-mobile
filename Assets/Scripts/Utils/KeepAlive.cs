using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Keeps an object alive during scene changes
    /// </summary>
    public class KeepAlive : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }

} // end namespace Cortex