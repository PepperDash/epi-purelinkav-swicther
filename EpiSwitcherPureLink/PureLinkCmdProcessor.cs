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

            CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type == eProgramStatusEventType.Stopping)
                        Dispose();
                };
        }

        /// <summary>
        /// Plugin method to queue tasks
        /// </summary>
        /// <param name="task"></param>
        public void EnqueueTask(Action task)
        {
            if (_disposed)
                return;

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

        // To detect redundant calls
        private bool _disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            CrestronEnvironment.GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                EnqueueTask(null);
                worker.Join();
                wh.Close();
                wh.Dispose();
            }

            _disposed = true;
        }
    }
}
