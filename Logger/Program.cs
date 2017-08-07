using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.ComponentModel;
using System.Configuration.Install;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;

namespace Logger
{
    using Webservice;

    class Program
    {
        internal static readonly string RegistrationToken = "239a2ce9-99d4-4361-92f6-1da30093a0c3";


#if DEBUG
        static string[] DependenciesToBeMergedWithAssembly = new string[] { "RestSharp.dll", "Cassia.dll" };
#endif

        // InstallLocationNormalized
        internal static string InstallLocationNormalized
        {
            get { return installLocationNormalized.Value; }
        }

        static Lazy<string> installLocationNormalized = new Lazy<string>(() =>
            NormalizePath(InstallLocation)
        );


        // InstallLocation
        internal static string InstallLocation
        {
            get { return installLocation.Value; }
        }

        static Lazy<string> installLocation = new Lazy<string>(() =>
            Path.Combine(InstallDirectory, OriginalFileName)
        );


        // UpdaterDownloadLocationNormalized
        internal static string UpdaterDownloadLocationNormalized
        {
            get { return updaterDownloadLocationNormalized.Value; }
        }

        static Lazy<string> updaterDownloadLocationNormalized = new Lazy<string>(() =>
            NormalizePath(UpdaterDownloadLocation)
        );


        // UpdaterDownloadLocation
        internal static string UpdaterDownloadLocation
        {
            get { return updaterDownloadLocation.Value; }
        }

        static Lazy<string> updaterDownloadLocation = new Lazy<string>(() =>
            Path.Combine(InstallDirectory, Path.ChangeExtension(AssemblyLocation, ".updater.exe"))
        );


        // StateFileLocation
        internal static string StateFileLocation
        {
            get { return stateFileLocation.Value; }
        }

        static Lazy<string> stateFileLocation = new Lazy<string>(() =>
            Path.Combine(AssemblyDirectory, "state.dat")
        );


        // InstallDirectory
        internal static string InstallDirectory
        {
            get { return installDirectory.Value; }
        }

        static Lazy<string> installDirectory = new Lazy<string>(() =>
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(dir, "Common Files", "Services");
        });


        // OriginalFileName
        static string OriginalFileName
        {
            get { return originalFileName.Value; }
        }

        static Lazy<string> originalFileName = new Lazy<string>(() =>
            FileVersionInfo.GetVersionInfo(AssemblyLocation).OriginalFilename
        );


        // FileName
        static string FileName
        {
            get { return fileName.Value; }
        }

        static Lazy<string> fileName = new Lazy<string>(() =>
            Path.GetFileName(AssemblyLocation)
        );


        // FileVersion
        internal static string FileVersion
        {
            get { return fileVersion.Value; }
        }

        static Lazy<string> fileVersion = new Lazy<string>(() =>
            FileVersionInfo.GetVersionInfo(AssemblyLocation).FileVersion
        );


        // AssemblyDirectory
        static string AssemblyDirectory
        {
            get { return assemblyDirectory.Value; }
        }

        static Lazy<string> assemblyDirectory = new Lazy<string>(() =>
            Path.GetDirectoryName(AssemblyLocation)
        );


        // AssemblyLocation
        static string AssemblyLocation
        {
            get { return assemblyLocation.Value; }
        }

        static Lazy<string> assemblyLocation = new Lazy<string>(() =>
            Assembly.GetEntryAssembly().Location
        );


        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var service = new Service();
            string command = args?.FirstOrDefault(); 

            switch (command)
            {
                default:
                    if (Environment.UserInteractive)
                    {
                        if (!IsAdministrator)
                            RunAsAdmin(AssemblyLocation, "--setup");
                        else
                            Setup(service.ServiceName);
                    }
                    else
                    {
                        if (File.Exists(UpdaterDownloadLocation))
                            TryDeleteUpdater(delay: TimeSpan.FromSeconds(2), force: true);

                        ServiceBase.Run(new[] { service });
                    }

                    break;

                case "--setup":
                    if (!IsAdministrator)
                        Program.Trace("--setup is missing administrator role");
                    else
                        Setup(service.ServiceName);

                    break;

                case "--update":
                    if (!IsAdministrator)
                        Program.Trace("--update is missing administrator role");
                    else
                        Setup(service.ServiceName, forceInstall: true);

                    break;

                case "--install":
                    return TryInstallWindowsService(service.ServiceName);

                case "--uninstall":
                    TryUninstallWindowsService(service.ServiceName);
                    break;

                case "--log":
                    var webserviceAuthToken = args?.Skip(1).FirstOrDefault();
                    var accountID = args?.Skip(2).FirstOrDefault();

                    Application.ApplicationExit += (obj, e) => {
                        Logger.Instance.Stop();
                        Program.Trace("Stopped logger on ApplicationExit event");
                    };

                    SystemEvents.SessionEnding += (obj, e) => {
                        Program.Trace($"Session ending (reason: { e.Reason }), calling Application.Exit()");
                        Application.Exit();
                    };

                    Logger.Instance.Start(webserviceAuthToken, accountID);
                    Program.Trace($"Started logging");

                    Application.Run();

                    break;
            }

            return 0;
        }

        /// <summary>
        /// Delay serves to give updater some time to exit, because it won't necessarily have exited already once it has started this service
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="force"></param>
        static void TryDeleteUpdater(TimeSpan delay, bool force)
        {
            Program.Trace($"Going to delete the updater in { delay.TotalSeconds } second(s)");

            Action TryDelete = () =>
            {
                if (force)
                    TryKillRunningInstances(UpdaterDownloadLocationNormalized);

                try
                {
                    File.Delete(UpdaterDownloadLocation);
                    Program.Trace("Updater deleted");
                }
                catch (Exception exception)
                {
                    Program.Trace($"Failed to delete updater: { exception.Message }");
                }
            };

            if (delay > new TimeSpan(0))
            {
                Task.Delay(delay).ContinueWith(t =>
                    TryDelete()
                );
            }
            else
                TryDelete();
        }

        static void Setup(string serviceName, bool forceInstall=false)
        {
            if (!IsAdministrator)
                throw new Exception("Administrator privileges required");

            Program.Trace("Running setup");

            // Check if previous installation exists
            bool previousInstallationExists = File.Exists(InstallLocation);


            // Determine if we should do an install (and overwrite any existing installation)
            bool doCopy;

            if (forceInstall)
            {
                Program.Trace("Force install");
                doCopy = true;
            }
            else
            {
                if (!previousInstallationExists)
                {
                    Program.Trace("No previous installation found");
                    doCopy = true; // Easy
                }
                else
                {
                    Program.Trace("Previous installation found");

                    // If the installed assembly is not certainly newer than this one, overwrite the existing installation
                    doCopy = IsOlderAssembly(InstallLocation) ?? true;

                    if (doCopy)
                        Program.Trace("Previously installed assembly is not certainly newer");
                }
            }

#if DEBUG
            doCopy = true;
#endif

            if (doCopy)
            {
                TryStopWindowsService(serviceName); // Stop the service to prevent it from spawning more logger instances (per user session)

                if (previousInstallationExists)
                    TryKillRunningInstances(InstallLocationNormalized);  // Kill all running instances to free Windows' lock on the previously installed executable

                if (!previousInstallationExists || !Directory.Exists(InstallDirectory))
                    Directory.CreateDirectory(InstallDirectory);

                Program.Trace("Attempting to copy and overwrite assembly");
                File.Copy(AssemblyLocation, InstallLocation, overwrite: true);

#if DEBUG
                foreach (var dependency in DependenciesToBeMergedWithAssembly)
                {
                    var source = Path.Combine(AssemblyDirectory, dependency);
                    var destination = Path.Combine(InstallDirectory, Path.GetFileName(dependency));

                    if (NormalizePath(source) == NormalizePath(destination))
                        continue;

                    File.Copy(source, destination, overwrite: true);
                }
#endif
                Program.Trace("Copying done");

                TryInstallWindowsService(serviceName, overwrite: true);
            }
            else
            {
                Program.Trace("Previously installed assembly is same version or newer, not overwriting it");

                // Seems like we are not copying the assembly to the install location (because the installed assembly is certainly newer),
                // nevertheless ensure the assembly is installed as a Windows service:
                TryInstallWindowsService(serviceName, overwrite: false);
            }

            TryStartWindowsService(serviceName);
        }

        static bool? IsOlderAssembly(string otherAssemblyLocation)
        {
            var otherFileVersion = FileVersionInfo.GetVersionInfo(otherAssemblyLocation).FileVersion;

            if (FileVersion == otherFileVersion)
                return false;

            Version thisVersion, otherVersion;

            if (!Version.TryParse(FileVersion, out thisVersion))
                return null;


            if (!Version.TryParse(otherFileVersion, out otherVersion))
                return null;

            return otherVersion < thisVersion;
        }

        static void TryUninstallWindowsService(string serviceName)
        {
            Program.Trace("Uninstalling Windows service");

            try
            {
                using (var installer = new AssemblyInstaller(InstallLocation, null))
                {
                    installer.Uninstall(null);
                    installer.Commit(null);
                }
            }
            catch (FileNotFoundException exception) when (exception.FileName.ToLower().Contains("installstate"))
            {
                // This doesn't seem to be a problem
            }
            catch (Exception exception)
            {
                Program.Trace($"Uninstalling Windows service failed: { exception.Message }");
                return;
            }

            Program.Trace("Windows service uninstalled");
        }

        static void TryInstallWindowsService(string serviceName, bool overwrite)
        {
            Program.Trace("Attempting to install Windows service");

            try
            {
                int result = Execute("--install");

                if (result == WinAPI.ERROR_SERVICE_EXISTS)
                {
                    Program.Trace("Windows service was already installed");

                    if (!overwrite)
                    {
                        return;
                    }
                    else
                    {
                        Execute("--uninstall");

                        Program.Trace("Installing Windows service again");
                        result = Execute("--install");

                        if (result == WinAPI.ERROR_SERVICE_EXISTS)
                        {
                            Program.Trace("Ran uninstall, but Windows service still exists");
                            return;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Program.Trace("Error when installing Windows service: " + exception.Message);
                return;
            }
            finally
            {
                // Try to clean up installation files
                var extensions = new[] { "InstallLog", "InstallState" };

                foreach (var ext in extensions)
                {
                    string fileName;

                    try
                    {
                        fileName = $"{ Path.GetFileNameWithoutExtension(InstallLocation) }.{ ext }";
                        var fileLocation = Path.Combine(InstallDirectory, fileName);
                        File.Delete(fileLocation);
                    }
                    catch (Exception exception)
                    {
                        Program.Trace($"Failed to delete { ext } file: { exception.Message }");
                    }
                }
            }

            Program.Trace("Windows service installed");
        }

        static int TryInstallWindowsService(string serviceName)
        {
            try
            {
                using (var installer = new AssemblyInstaller(InstallLocation, null))
                {
                    installer.Install(null);
                    installer.Commit(null);
                }
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == WinAPI.ERROR_SERVICE_EXISTS)
            {
                return WinAPI.ERROR_SERVICE_EXISTS;
            }

            return 0;
        }


        static void TryStopWindowsService(string serviceName)
        {
            Program.Trace("Stopping Windows service");

            try
            {
                using (var controller = new ServiceController(serviceName))
                {
                    if (controller.Status != ServiceControllerStatus.Stopped && controller.Status != ServiceControllerStatus.StopPending)
                    {
                        controller.Stop();
                    }
                    else
                    {
                        Program.Trace("Windows Service was already stopped");
                        return;
                    }

                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(120));
                }
            }
            catch (InvalidOperationException exception)
            {
                // service was not found, then it's as good as stopped
                Program.Trace($"I think I failed to find the windows service, but this shouldn't be a problem: { exception.Message }");
                return;
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to stop Windows service: { exception.Message }");
                return;
            }

            Program.Trace("Stopped Windows service");
        }

        static void TryStartWindowsService(string serviceName, bool waitForIt = false)
        {
            Program.Trace("Starting Windows service");

            try
            {
                using (var controller = new ServiceController(serviceName))
                {
                    if (controller.Status != ServiceControllerStatus.Running && controller.Status != ServiceControllerStatus.StartPending)
                    {
                        controller.Start();
                    }
                    else
                    {
                        Program.Trace("Windows Service was already started");
                        return;
                    }

                    if (waitForIt)
                        controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to start Windows service: { exception.Message }");
                return;
            }

            Program.Trace("Started Windows service");
        }

        static void TryKillRunningInstances(string normalizedExecutablePath)
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                var fileName = process.TryGetFileName();

                if (fileName == null)
                    continue;

                if (NormalizePath(fileName) != normalizedExecutablePath)
                    continue;
                
                try
                {
                    process.Kill();
                    Program.Trace($"Killed process { process.Id }");
                }
                catch (Exception exception)
                {
                    Program.Trace($"Couldn't kill process { process.Id }: {exception.Message}");
                }
                finally
                {
                    process?.Close();
                }
            }
        }


        /// <summary>
        /// Runs this assembly in a different domain, helps to circumvent stubborn file locks from the ManagedInstaller and AssemblyInstaller classes
        /// </summary>
        static int Execute(params string[] args)
        {
            var domain = AppDomain.CreateDomain(FileName);
            int result = domain.ExecuteAssemblyByName(Assembly.GetExecutingAssembly().FullName, args);
            AppDomain.Unload(domain);
            return result;
        }

        internal static void RunAsAdmin(string fileName, string arguments = null)
        {
            Program.Trace($"Running '{ fileName }' as admin");
            Process process = null;

            try
            {
                var startInfo = new ProcessStartInfo(fileName, arguments);
                startInfo.Verb = "runas";

                process = Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                Program.Trace($"Failed to elevate: { exception.Message }");
            }
            finally
            {
                process?.Close();
            }
        }

        static bool IsAdministrator
        {
            get
            {
                bool isAdmin;
                WindowsIdentity user = null;

                try
                {
                    user = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(user);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (Exception)
                {
                    isAdmin = false;
                }
                finally
                {
                    user?.Dispose();
                }

                return isAdmin;
            }
        }

        internal static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            Program.Trace($"Unhandled exception: { exception?.Message }");
            Program.Trace(exception?.StackTrace);
        }


        static SemaphoreSlim TraceSemaphore = new SemaphoreSlim(1, 1);

        internal static async void Trace(string text)
        {
            bool entered = await TraceSemaphore.WaitAsync(TimeSpan.FromSeconds(2));

            if (!entered)
                return;

            try
            {
                File.AppendAllLines(@"C:\users\jorn\desktop\debug.txt", new string[] { $"{ DateTime.Now } { text }" });
            }
            finally
            {
                TraceSemaphore.Release();
            }
        }
    }
}