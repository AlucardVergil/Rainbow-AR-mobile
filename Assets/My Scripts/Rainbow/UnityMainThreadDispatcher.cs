using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            _instance = new GameObject("UnityMainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
        }
        return _instance;
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    // Enqueue action to execute on main thread when i don't care about execution order
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }



    // Enqueue action to execute on main thread but suspend the rest of the code in the background thread until this code on the main thread finishes.
    // This is when i don't need to return anything from main thread to the background thread, but instead just suspend execution until it finishes
    // NOTE: If i want to also return a result of any type look below in comment for the template to execute in the corresponding script!
    // Use "await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>      {        });" in an async method to call this. Can also use "await Task.Run(async () =>" if i want to send something to the background thread.
    public Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

        Enqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }


    //KEEP AS TEMPLATE
    #region Enqueue Example Template To Be Used In Any script I Want When I Also Want To Return A Result From Main Thread Back To The Background Thread After The Main Thread Finishes The Action


    /*
    public async void BackgroundThreadTask()
    {
        Debug.Log("Doing background work on background thread");

        // Wait for main thread action to complete
        var result = await EnqueueAndWaitForMainThreadAction();

        Debug.Log($"Received main thread result: {result}");

        Debug.Log("Background thread task complete");
    }



    private Task<int> EnqueueAndWaitForMainThreadAction()
    {
        var taskCompletionSource = new TaskCompletionSource<int>();

        // Enqueue action to main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log("Main thread actions executed example");            

            // After main thread work is done, complete the task
            taskCompletionSource.SetResult(42);
        });

        // Wait for task completion (i.e., wait for the main thread work to finish)
        return taskCompletionSource.Task;
    }
    */
    #endregion

}