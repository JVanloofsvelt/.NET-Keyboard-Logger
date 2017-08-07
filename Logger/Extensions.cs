using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using Cassia;

namespace Logger
{
    static class Bytes
    {
        /// <summary>
        /// Saves array as a file, overwrites any existing file
        /// </summary>
        /// <returns></returns>
        public static async Task SaveAsFileAsync(this byte[] bytes, string path, CancellationToken token)
        {
            using (var file = File.Create(path))
            {
                await file.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            }
        }

        public static async Task<byte[]> LoadFromFileAsync(string path, CancellationToken token)
        {
            using (var file = File.OpenRead(path))
            {
                var data = new byte[file.Length];

                int nRead = await file.ReadAsync(data, 0, (int)file.Length, token).ConfigureAwait(false);

                if (nRead != file.Length)
                    throw new Exception("File not completely read");

                return data;
            }
        }
    }

    static class Extensions
    {
        public static IEnumerable<byte> Cycle(this IEnumerable<byte> input)
        {
            while (true)
            {
                foreach (var b in input)
                    yield return b;
            }
        }

        /// <summary>
        /// Attempts to close the main window of a process, kills the process if it was unable to do so
        /// </summary>
        /// <param name="millisecondsTimeout">Maximum time spent trying to acquire a handle to the main window</param>
        /// <returns>True if method was able to close the main window</returns>
        public static bool CloseOrKill(this Process process, int millisecondsTimeout=50)
        {
            IntPtr handle = IntPtr.Zero; ;


            // Try to get a handle to the main window
            Action TryGetHandle = () => {
                handle = process.MainWindowHandle;
            };

            if (millisecondsTimeout <= 0)
            {
                TryGetHandle();
            }
            else
            {
                var sw = new Stopwatch();
                sw.Start();

                do
                {
                    TryGetHandle();
                }
                while (handle == IntPtr.Zero && sw.ElapsedMilliseconds < millisecondsTimeout);

                sw.Stop();
            }


            // Try to close the window, else kill the process
            var success = process.CloseMainWindow();

            if (!success)
                process.Kill();

            return success;
        }

        public static string TryGetFriendlyProcessName(this Process process)
        {
            string name = null;

            try
            {
                name = process.MainModule?.FileVersionInfo.FileDescription;
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == WinAPI.ERROR_ACCESS_DENIED)
            {
                // It happens for some processes
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == WinAPI.ERROR_UNKNOWN && process.SessionId == 0)
            {
                // As expected
            }
            catch (Win32Exception exception)
            {
                Program.Trace($"{ nameof(Win32Exception) } on attempt to get a friendly name for process { process.Id }: { exception.Message} (Error code: { exception.NativeErrorCode })");
            }
            catch (Exception exception)
            {
                Program.Trace($"Exception on attempt to get a friendly name for process { process.Id }: { exception.Message }");
            }

            if (string.IsNullOrWhiteSpace(name))
                name = process.ProcessName;

            return name;
        }

        public static string TryGetFileName(this Process process)
        {
            string fileName = null;

            try
            {
                fileName = process.MainModule.FileName;
            }
            catch (ArgumentException exception)
            {
                Program.Trace($"{ exception.GetType().Name } on attempt to get the filename of process { process.Id } in session { process.SessionId }: sessionID is probably invalid");
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == WinAPI.ERROR_ACCESS_DENIED)
            {
                // It happens for some processes 
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == WinAPI.ERROR_UNKNOWN && process.SessionId == 0)
            {
                // As expected
            }
            catch (Win32Exception exception)
            {
                Program.Trace($"{ exception.GetType().Name } on attempt to get the filename of process { process.Id } in session { process.SessionId }: { exception.Message } (Error code { exception.NativeErrorCode })");
            }
            catch (Exception exception)
            {
                Program.Trace($"Exception on attempt to get the filename of process { process.Id } in session { process.SessionId }: { exception.GetType().Name }: { exception.Message }");
            }

            return fileName;
        }
    }
}
