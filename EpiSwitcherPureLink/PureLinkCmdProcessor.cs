using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;

namespace PureLinkPlugin
{
    /// <summary>
    /// Plugin processor class used for threading
    /// </summary>
    public class PureLinkCmdProcessor : IDisposable
    {
        Thread worker;
        CEvent wh = new CEvent();
        CrestronQueue<Action> tasks = new CrestronQueue<Action>();

        /// <summary>
        /// Method to create a new worker thread
        /// </summary>
        public PureLinkCmdProcessor()
        {
            worker = new Thread(ProcessFeedback, null, Thread.eThreadStartOptions.Running);
        }

        /// <summary>
        /// Plugin method to queue tasks
        /// </summary>
        /// <param name="task"></param>
        public void EnqueueTask(Action task)
        {
            tasks.Enqueue(task);
            wh.Set();
        }

        object ProcessFeedback(object obj)
        {
            while (true)
            {
                Action task = null;

                if (tasks.Count > 0)
                {
                    task = tasks.Dequeue();
                    if (task == null) break;
                }
                if (task != null)
                {
                    task.Invoke();
                }
                else wh.Wait();
            }
            return null;
        }

        #region IDisposable Members

        /// <summary>
        /// Method to dispose of the worker thread
        /// </summary>
        public void Dispose()
        {
            EnqueueTask(null);
            worker.Join();
            wh.Close();
        }
        #endregion
    }
}
