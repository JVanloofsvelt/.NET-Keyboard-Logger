using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Logger
{
    using Webservice;

    partial class Service
    {
        enum CommandType
        {
            UPDATE
        };

        HashSet<string> commandIDsBeingHandled = new HashSet<string>();

        async void TryHandleCommand(Command command)
        {
            lock (commandIDsBeingHandled)
            {
                if (commandIDsBeingHandled.Contains(command.ID))
                    return;

                commandIDsBeingHandled.Add(command.ID);
            }


            // Parse type
            CommandType type;

            bool commandParsed = Enum.TryParse(command.Type, out type);

            if (commandParsed)
            {
                Program.Trace($"Received command of type { command.Type }");
            }
            else
            {
                Program.Trace($"Unknown command type: { command.Type }");
                await TryCompleteCommandAsync(command, "Unknown command type").ConfigureAwait(false);
                return;
            }


            // Handle
            Func<Command, Task> TryHandle;

            switch (type)
            {
                case CommandType.UPDATE:
                    TryHandle = TryUpdate;
                    break;

                default:
                    TryHandle = null;
                    break;
            }

            await TryHandle(command).ConfigureAwait(false);

            lock (commandIDsBeingHandled)
                commandIDsBeingHandled.Remove(command.ID);
        }

        async Task TryUpdate(Command command)
        {
            Uri uri;

            bool success = Uri.TryCreate(command.Parameters, UriKind.Absolute, out uri);

            if (!success)
            {
                await TryCompleteCommandAsync(command, "Could not parse Uri").ConfigureAwait(false);
                return;
            }

            try
            {
                using (var client = new WebClient())
                {
                    var download = new WebClient().DownloadFileTaskAsync(uri, Program.UpdaterDownloadLocation);
                    Program.Trace("Downloading update");

                    await download.ConfigureAwait(false);
                    Program.Trace("Update downloaded");
                }
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to download update: { exception.Message }");
                await TryCompleteCommandAsync(command, exception.Message).ConfigureAwait(false);
                return;
            }

            await TryCompleteCommandAsync(command, null).ConfigureAwait(false);

            // Run updater
            Program.RunAsAdmin(Program.UpdaterDownloadLocation, "--update");
        }

        async Task TryCompleteCommandAsync(Command command, string error)
        {
            try
            {
                await Webservice.CompleteCommandAsync(command.ID, error, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // As expected
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to complete command: { exception.Message }");
            }
        }
    }
}
