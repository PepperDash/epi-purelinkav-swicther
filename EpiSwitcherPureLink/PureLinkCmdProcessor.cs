using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;

namespace PureLinkPlugin
{
    public class PureLinkCmdProcessor : IDisposable
    {
        Thread worker;
        CEvent wh = new CEvent();
        CrestronQueue<Action> tasks = new CrestronQueue<Action>();

        public PureLinkCmdProcessor()
        {
            worker = new Thread(ProcessFeedback, null, Thread.eThreadStartOptions.Running);
        }

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

        public void Dispose()
        {
            tasks.Enqueue(null);
            worker.Join();
            wh.Close();
        }
        #endregion
    }
}