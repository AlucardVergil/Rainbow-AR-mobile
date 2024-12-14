using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Rainbow.WebRTC.Unity
{
    public class RainbowThreadExecutor
    {
        private BlockingCollection<Action> queue;
        private bool terminated;
        private Task task;
        
        public RainbowThreadExecutor()
        {
            terminated = false;
            queue = new BlockingCollection<Action>();
            task = Task.Factory.StartNew(ThreadExecutorLoop, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);            
        }

        /// <summary>
        /// Stop asynchronously stops the executor queue
        /// </summary>
        public void Stop()
        {
            terminated = true;
            queue.Add(() => { });
        }

        /// <summary>
        /// Execute pushes an action to be executed in this thread.
        /// </summary>
        /// <param name="action"> action is the action to execute. </param>
        public void Execute(Action action)
        {
            queue.Add(action);
        }

        private void ThreadExecutorLoop()
        {
            while(!terminated)
            {
                Action action = queue.Take();
                if( action != null)
                {

                    try
                    {
                        action();
                    } 
                    catch(Exception ex)
                    {
                        UnityEngine.Debug.LogError($"exception in rainbow thread executor: {ex.Message}\n{ex.StackTrace}");
                    }
                }                    
            }
        }
    }
}
