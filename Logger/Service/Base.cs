using System;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Logger
{
    using Webservice;

    [System.ComponentModel.DesignerCategory("")]
    public partial class Service : ServiceBase, IDisposable
    {
        CancellationTokenSource cts = null;
        SemaphoreSlim SaveStateSemaphore = null;
        Task fetchCommands = null;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Program.Trace("Service.OnStart() was called");

            // Clean up disposables from last run
            cts?.Dispose();
            SaveStateSemaphore?.Dispose();

            // Init
            cts = new CancellationTokenSource();
            SaveStateSemaphore = new SemaphoreSlim(1, 1);


            // Load state
            TryLoadStateAsync().Wait();


            // Register if necessary
            if (!Webservice.IsRegistered)
            {
                var register = RetryUntilSuccessAsync(TryRegisterAsync, TimeSpan.FromSeconds(5));

                // TODO: Don't have to wait anymore when we can forward auth. token to logger instances by other means (e.g. named pipes) than cmd line arguments
                register.ContinueWith(t =>
                {
                    if (!string.IsNullOrWhiteSpace(Webservice.AuthenticationToken))
                        Program.Trace($"Successfully registered");
                    else
                        Program.Trace("Successfully registered but no authentication token was returned");

                    TryEnumerateSessions();
                }, TaskContinuationOptions.OnlyOnRanToCompletion);

                register.ContinueWith(t =>
                    this.fetchCommands = FetchCommands()
                );
            }
        }

        protected override void OnStop()
        {
            Program.Trace("Service.OnStop() was called");

            cts.Cancel();  // This will, among many others tasks, cancel any TrySaveStateAsync tasks

            // Save final state before stopping
            TrySaveStateAsync(ignoreCancellationToken: true).Wait();  

            fetchCommands?.Wait();
        }

        protected override void OnShutdown()
        {
            Program.Trace("Service.OnShutdown() was called");
            Stop();
        }
    }
}
