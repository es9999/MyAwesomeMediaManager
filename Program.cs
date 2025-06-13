using System;
using System.Windows.Forms;
using MyAwesomeMediaManager.Data;
using LibVLCSharp.Shared;

namespace MyAwesomeMediaManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Core.Initialize(); // <--- Add this line ONCE per app lifecycle
            DatabaseHelper.InitializeDatabase();

            var folders = DatabaseHelper.GetAllFolders();
            if (folders.Count == 0)
            {
                Application.Run(new FolderSetupForm());
            }
            else
            {
                var main = new MainForm();
                main.WindowState = FormWindowState.Maximized;
                Application.Run(main);
            }
        }

    }
}
