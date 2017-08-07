using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;

namespace Logger
{
    using Webservice;

    partial class Service
    {
        string webserviceAuthToken = null;

        Webservice.Client Webservice
        {
            get { return new Webservice.Client(webserviceAuthToken); }
        }

        Dictionary<string, string> UserAccountIDs = new Dictionary<string, string>();


        async Task<string> GetAccountID(string username)
        {
            string accountID;


            bool accountExists = this.UserAccountIDs.TryGetValue(username, out accountID);

            if (accountExists)
                return accountID;

            try
            {
                accountID = await Webservice.CreateUserAccountAsync(username, cts.Token).ConfigureAwait(false);
                this.UserAccountIDs[username] = accountID;

                return accountID;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // As expected
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to create user account through webservice: { exception.Message}");
            }

            return null;
        }

        async Task FetchCommands()
        {
            while (!cts.IsCancellationRequested)
            {
                if (Webservice.IsRegistered)
                {
                    List<Command> commands = null;

                    try
                    {
                        commands = await Webservice.GetCommandsAsync(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested)
                    {
                        // As expected
                    }
                    catch (WebException exception) when (exception.Status == WebExceptionStatus.ConnectFailure)
                    {
                        Program.Trace(exception.Message);
                    }
                    catch (Exception exception)
                    {
                        Program.Trace($"{ exception.GetType() }: { exception.Message }");
                    }

                    commands?.ForEach(TryHandleCommand);
                }

                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }

        async Task<bool> TryRegisterAsync()
        {
            var webservice = new Webservice.Client();

            try
            {
                var registrationInfo = new RegistrationInfo
                {
                    Hostname = Environment.MachineName,
                    FileVersion = Program.FileVersion
                };

                this.webserviceAuthToken = await webservice.RegisterAsync(Program.RegistrationToken, registrationInfo, cts.Token).ConfigureAwait(false);
                var saveState = TrySaveStateAsync();

                return true;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                return false;
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

        protected async Task RetryUntilSuccessAsync(Func<Task<bool>> Action, TimeSpan interval)
        {
            bool success;

            do
            {
                success = await Action().ConfigureAwait(false);

                if (!success)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(interval, cts.Token).ConfigureAwait(false);
                }
            }
            while (!success);
        }
    }
}
