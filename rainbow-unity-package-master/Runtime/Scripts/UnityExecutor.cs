using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Rainbow.WebRTC.Unity
{
    public class UnityExecutor : MonoBehaviour
    {
        private static UnityExecutor instance;
        internal static bool destroyed;
        private ConcurrentQueue<Action> queueAction;
        private ConcurrentQueue<IEnumerator> queueCoRoutines;
        private Coroutine executer;
        private int unityThread;

        public static void Initialize() {
            var x = Instance;
        }
        
        public static UnityExecutor Instance
        {            
            get
            {
                if (instance == null && !destroyed )
                {
                    instance = new GameObject("executor").AddComponent<UnityExecutor>();
                }
                return instance;
            }
        }
        
        public static bool IsUnityThread()
        {
            return Instance.IsUnityMainThread();
        }

        private UnityExecutor() {
            queueAction = new();
            queueCoRoutines = new();
            
        }

        private void Awake()
        {
            unityThread = Thread.CurrentThread.ManagedThreadId;
            executer = StartCoroutine(ProcessQueue());
        }
        private bool IsUnityMainThread()
        {
            return unityThread == Thread.CurrentThread.ManagedThreadId;
        }

        private void OnDestroy()
        {
            destroyed = true;
            StopCoroutine(executer);
        }

        private void execute(IEnumerator coroutine)
        {
            try
            {
                StartCoroutine(coroutine);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }
        static public void Execute(IEnumerator coroutine)
        {
            if (destroyed)
            {
                return;
            }
            if(instance.IsUnityMainThread())
            {
                instance.execute(coroutine);
                return;
            }
            instance.queueCoRoutines.Enqueue(coroutine);
        }

        private void execute(Action action)
        {            
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void Execute(Action action)
        {
            if (destroyed)
            {
                return;
            }

            if (Instance.IsUnityMainThread())
            {
                instance.execute(action);
                return;
            }
            instance.queueAction.Enqueue(action);
        }

        public static void ExecuteSync(Action action)
        {
            if (destroyed)
            {
                return;
            }
            if ( Instance.IsUnityMainThread())
            {
                instance.execute(action);
                return;
            }
            else
            {
                ManualResetEvent manualResetEvent = new(false) { };
                instance.queueAction.Enqueue(() => {
                    instance.execute(action);
                    manualResetEvent.Set();
                });
                manualResetEvent.WaitOne();
            }
        }
        // ProcessQueue is a coroutine running in the background which excutes the actions
        // pushed in this.queueAction from the main thread.
        private IEnumerator ProcessQueue()
        {
            Action action;
            while (true)
            {
                if( destroyed)
                {
                    yield break;
                }

                if (!queueCoRoutines.IsEmpty)
                {
                    IEnumerator coroutine;
                    if (queueCoRoutines.TryDequeue(out coroutine))
                    {                        
                       execute(coroutine);
                    }
                    //else
                    //{
                    //    yield return false;
                    //}
                }

                if (!queueAction.IsEmpty)
                {
                    if (queueAction.TryDequeue(out action))
                    {
                        execute(action);
                    }
                    //else
                    //{
                    //    yield return false;
                    //}
                }

                if( queueCoRoutines.IsEmpty && queueAction.IsEmpty )
                {
                    yield return false;
                }

            }            
        }
    }
}