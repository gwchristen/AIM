using System;
using System.Windows.Forms;

namespace AIM.Installer
{
    /// <summary>
    /// Entry point for the AIM Installer application.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            Application.Run(new InstallerForm());
        }
    }
}
