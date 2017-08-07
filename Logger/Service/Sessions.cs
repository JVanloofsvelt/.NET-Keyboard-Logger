using System;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.ComponentModel;
using System.Diagnostics;

using Cassia;

namespace Logger
{
    partial class Service
    {
        ConnectionState[] UnqualifiedConnectionStates = new ConnectionState[] { ConnectionState.Disconnected, ConnectionState.Listening };
        SessionChangeReason[] QualifiedSessionChangeReasons = new SessionChangeReason[] { SessionChangeReason.SessionLogon, SessionChangeReason.SessionUnlock };

        private void TryEnumerateSessions()
        {
            try
            {
                Program.Trace("Enumerating sessions");

                var manager = new TerminalServicesManager();

                using (var server = manager.GetLocalServer())
                {
                    foreach (var session in server.GetSessions())
                    {
                        if (session == null) continue; // Doubt this ever happens, but just to be sure


                        // For debug purposes
                        string userID = "";

                        if (!string.IsNullOrWhiteSpace(session.UserName) || !string.IsNullOrEmpty(session.UserAccount?.ToString()))
                            userID = $" (UserName: { session.UserName }, NTAccount: { session.UserAccount })";

                        Program.Trace($"Session { session.SessionId }{ userID }: { session.ConnectionState }");


                        // Start a logger in that session
                        Task startLogger;

                        if (!UnqualifiedConnectionStates.Contains(session.ConnectionState))
                            startLogger = TryRunLoggerInSessionAsync(session);
                    }
                }
            }
            catch (Exception exception)
            {
                Program.Trace($"Error when enumerating sessions: { exception.Message }");
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            var reason = changeDescription.Reason;
            var sessionID = changeDescription.SessionId;

            var session = TryGetSession(sessionID);
            Program.Trace($"Change event in session { sessionID } (user: { session?.UserName }): { reason }");

            if (QualifiedSessionChangeReasons.Contains(reason) && Webservice.IsRegistered)  // TODO: remove Webservice.IsRegistered constraint later?
            {
                Task startLogger;

                if (session != null)
                    startLogger = TryRunLoggerInSessionAsync(session);
                else
                    startLogger = TryRunLoggerInSessionAsync(sessionID, username: null);

            }

            if ((reason == SessionChangeReason.RemoteConnect || reason == SessionChangeReason.RemoteDisconnect) && session != null)
                Program.Trace($"For remote connect/disconnect in session { sessionID } - Client: { session.ClientName }, IP address: { session.ClientIPAddress }, Station: { session.WindowStationName}");
        }

        ITerminalServicesSession TryGetSession(int sessionID)
        {
            try
            {
                var manager = new TerminalServicesManager();

                using (var server = manager.GetLocalServer())
                {
                    return server.GetSession(sessionID);
                }
            }
            catch (Exception exception)
            {
                Program.Trace($"Unable to get terminalServicesSession: { exception.Message }");
            }

            return null;
        }

        Task TryRunLoggerInSessionAsync(ITerminalServicesSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            return TryRunLoggerInSessionAsync(session.SessionId, session.UserAccount?.ToString());
        }

        async Task TryRunLoggerInSessionAsync(int sessionID, string username)
        {
            var process = TryFindRunningLoggerInstance(sessionID);

            if (process != null)
            {
                Program.Trace($"Logger is already running in session { sessionID }");
            }
            else
            {

                var accountID = await GetAccountID(username).ConfigureAwait(false);

                if (cts.IsCancellationRequested)
                    return;

                if (accountID == null)
                {
                    Program.Trace($"That's odd, { nameof(GetAccountID) } returned null");
                    return;
                }

                try
                {
                    int startedProcessID = WinAPI.CreateProcessInSession((uint)sessionID, Program.InstallLocation, Program.InstallDirectory, $"--log { webserviceAuthToken } { accountID }");
                    Program.Trace($"Started logger in session { sessionID } with process ID { startedProcessID }");
                }
                catch (Exception exception)
                {
                    Program.Trace($"Failed to start logger in session { sessionID }: { exception.Message }");
                    return;
                }
            }
        }

        /// <summary>Returns the process of the running logger instance in the given session, returns null if none was found</summary>
        Process TryFindRunningLoggerInstance(int sessionID)
        {
            try
            {
                var manager = new TerminalServicesManager();

                using (var server = manager.GetLocalServer())
                {
                    foreach (var process in server.GetProcesses())
                    {
                        if (process.SessionId != sessionID)  // Skip processes of other sessions
                            continue;

                        var fileName = process.UnderlyingProcess.TryGetFileName();

                        // Compare filename, if match we found a running instance
                        if (fileName != null && Program.NormalizePath(fileName) == Program.InstallLocationNormalized)
                            return process.UnderlyingProcess;
                    }
                }
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == WinAPI.ERROR_UNKNOWN && sessionID == 0)
            {
                // As expected
            }
            catch (Exception exception)
            {
                Program.Trace($"Exception when enumerating processes: { exception.GetType() }: { exception.Message }");
            }

            return null;
        }
    }
}
