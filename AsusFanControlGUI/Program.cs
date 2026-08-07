using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
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
            if (TryBootstrapToSystem())
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static bool TryBootstrapToSystem()
        {
            if (IsRunningAsSystem())
            {
                return false;
            }

            if (!IsRunningElevated())
            {
                RelaunchAsAdministrator();
                return true;
            }

            LaunchAsSystem();
            return true;
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

        private static void LaunchAsSystem()
        {
            var psexecPath = FindPsExecPath();
            if (psexecPath == null)
            {
                MessageBox.Show(
                    "PsExec.exe was not found.\n\nPlace PsExec.exe next to AsusFanControlGUI.exe or in Documents\\AsusFanControl, then try again.",
                    "Asus Fan Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = psexecPath,
                    Arguments = "-accepteula -i -s -d \"" + Application.ExecutablePath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to start Asus Fan Control as SYSTEM.\n\n" + ex.Message,
                    "Asus Fan Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
