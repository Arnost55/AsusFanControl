using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            StartupTrace.Log("Main entered");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            if (TryBootstrapToSystem())
            {
                StartupTrace.Log("Bootstrap requested relaunch");
                return;
            }

            try
            {
                StartupTrace.Log("Starting WinForms app");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var mainForm = new Form1())
                {
                    StartupTrace.Log("Form constructed");
                    Application.Run(mainForm);
                    StartupTrace.Log("Application.Run returned");
                }
            }
            catch (Exception ex)
            {
                StartupTrace.Log("Fatal exception in Main");
                ReportFatalError("Unable to start Asus Fan Control.", ex);
            }
        }

        private static bool TryBootstrapToSystem()
        {
            if (!IsRunningElevated())
            {
                RelaunchAsAdministrator();
                return true;
            }

            return false;
        }

        private static bool IsRunningElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static bool IsRunningAsSystem()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                return identity.User != null && identity.User.IsWellKnown(WellKnownSidType.LocalSystemSid);
            }
        }

        private static void RelaunchAsAdministrator()
        {
            try
            {
                StartupTrace.Log("Attempting admin relaunch");
                var startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                Process.Start(startInfo);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                MessageBox.Show(
                    "Asus Fan Control needs administrator permission to start.",
                    "Asus Fan Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to restart Asus Fan Control with administrator privileges.\n\n" + ex.Message,
                    "Asus Fan Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool LaunchAsSystem()
        {
            var psexecPath = FindPsExecPath();
            if (psexecPath == null)
            {
                StartupTrace.Log("PsExec not found");
                return false;
            }

            try
            {
                StartupTrace.Log("Attempting SYSTEM relaunch");
                var startInfo = new ProcessStartInfo
                {
                    FileName = psexecPath,
                    Arguments = "-accepteula -i -s -d \"" + Application.ExecutablePath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to start Asus Fan Control as SYSTEM.\n\n" + ex.Message,
                    "Asus Fan Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ReportFatalError("An unexpected UI error occurred.", e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            ReportFatalError("An unexpected startup error occurred.", exception ?? new Exception("Unknown unhandled exception."));
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportFatalError("An unexpected background error occurred.", e.Exception);
            e.SetObserved();
        }

        private static void ReportFatalError(string message, Exception ex)
        {
            try
            {
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsusFanControl");
                Directory.CreateDirectory(logDirectory);
                var logPath = Path.Combine(logDirectory, "startup-error.log");
                File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + message + Environment.NewLine + (ex != null ? ex.ToString() : "Unknown exception") + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // Ignore logging failures.
            }

            try
            {
                MessageBox.Show(
                    message + "\n\n" + (ex != null ? ex.Message : "Unknown exception") + "\n\nA log was written to your LocalAppData\\AsusFanControl folder.",
                    "Asus Fan Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Last resort only.
            }
        }

        internal static class StartupTrace
        {
            private static readonly object Sync = new object();

            public static void Log(string message)
            {
                try
                {
                    var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsusFanControl");
                    Directory.CreateDirectory(logDirectory);
                    var logPath = Path.Combine(logDirectory, "startup-trace.log");
                    lock (Sync)
                    {
                        File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
                    }
                }
                catch
                {
                    // Ignore logging failures.
                }
            }
        }

        private static string FindPsExecPath()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PsExec.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AsusFanControl", "PsExec.exe")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
