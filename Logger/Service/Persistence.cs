using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace Logger
{
    using static Serialization;

    partial class Service
    {
        [Serializable]
        class State
        {
            public string WebserviceAuthToken;
            public Dictionary<string, string> UserAccountIDs;
        }

        public async Task TryLoadStateAsync()
        {
            try
            {
                if (!File.Exists(Program.StateFileLocation))
                    return;

                var bytes = await Bytes.LoadFromFileAsync(Program.StateFileLocation, cts.Token).ConfigureAwait(false);
                var state = Deserialize(bytes) as State;

                webserviceAuthToken = state.WebserviceAuthToken;
                UserAccountIDs = state.UserAccountIDs;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // As expected
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to load state: { exception.Message }");
            }
        }

        public async Task TrySaveStateAsync(bool ignoreCancellationToken=false)
        {
            try
            {
                CancellationToken token = ignoreCancellationToken ? CancellationToken.None : cts.Token;

                await SaveStateSemaphore.WaitAsync(token).ConfigureAwait(false);

                var bytes = Serialize(new State
                {
                    WebserviceAuthToken = this.webserviceAuthToken,
                    UserAccountIDs = this.UserAccountIDs
                });

                await bytes.SaveAsFileAsync(Program.StateFileLocation, token).ConfigureAwait(false);

            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // As expected
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to save state: { exception.Message }");
            }
            finally
            {
                SaveStateSemaphore.Release();
            }
        }
    }
}
