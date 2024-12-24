using AutoUpdaterDotNET;
using System;
using System.Windows.Forms;

namespace strayafreetweakingutil
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            AutoUpdater.Start("https://github.com/krylodev/straya-free-tweaker/raw/refs/heads/main/AutoUpdater.xml");
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }

        private static void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable) {
                var dialogResult = MessageBox.Show($"A new version {args.CurrentVersion} is available. Do you want to update now?", "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.Yes) {
                    try {
                        AutoUpdater.DownloadUpdate(args);
                    }
                    catch (Exception ex) {
                        MessageBox.Show($"Error occurred while trying to update the application: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else {
                    MessageBox.Show("The application will now close.", "Update Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                }
            }
        }
    }
}
