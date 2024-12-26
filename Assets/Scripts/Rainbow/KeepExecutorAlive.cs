using Rainbow.WebRTC.Unity;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Keeps the <see cref="UnityExecutor"/> instance alive during scene changes.
    /// If the executor is destroyed in some way, all not-yet executed actions will be cleared.
    /// </summary>
    public class KeepExecutorAlive : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        // Start is called before the first frame update
        void Start()
        {
            UnityExecutor.Initialize();
            UnityExecutor exec = UnityExecutor.Instance;
            DontDestroyOnLoad(exec);
        }
    }
} // end namespace Cortex