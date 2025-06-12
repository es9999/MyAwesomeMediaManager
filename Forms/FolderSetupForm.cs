using MyAwesomeMediaManager.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyAwesomeMediaManager
{
    public partial class FolderSetupForm : Form
    {
        // Internal list of folders selected by user
        private readonly List<string> folders = new();

        // Public property to access the selected folders
        public IReadOnlyList<string> SelectedFolders => folders.AsReadOnly();

        // UI controls
        private ListBox listBox;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnProceed;
        private ProgressBar progressBar;

        public FolderSetupForm()
        {
            InitializeComponent();
            LoadSavedFolders();
        }

        private void InitializeComponent()
        {
            this.Text = "Setup Folders";
            this.Width = 500;
            this.Height = 400;

            listBox = new ListBox { Top = 10, Left = 10, Width = 460, Height = 250 };
            btnAdd = new Button { Text = "Add Folder", Top = 270, Left = 10, Width = 100 };
            btnRemove = new Button { Text = "Remove Selected", Top = 270, Left = 120, Width = 120 };
            btnProceed = new Button { Text = "Proceed", Top = 320, Left = 370, Width = 100 };
            progressBar = new ProgressBar { Top = 300, Left = 10, Width = 350, Height = 20 };

            btnAdd.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK && !folders.Contains(fbd.SelectedPath))
                {
                    folders.Add(fbd.SelectedPath);
                    listBox.Items.Add(fbd.SelectedPath);
                }
            };

            btnRemove.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string selectedFolder = listBox.SelectedItem.ToString();
                    folders.Remove(selectedFolder);
                    listBox.Items.Remove(listBox.SelectedItem);
                }
            };

            btnProceed.Click += async (s, e) =>
            {
                btnProceed.Enabled = false;
                btnAdd.Enabled = false;
                btnRemove.Enabled = false;
                progressBar.Value = 0;

                if (folders.Count == 0)
                {
                    MessageBox.Show("Please add at least one folder before proceeding.", "No folders", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnProceed.Enabled = true;
                    btnAdd.Enabled = true;
                    btnRemove.Enabled = true;
                    return;
                }

                DatabaseHelper.InitializeDatabase();

                var allFiles = new List<string>();

                // Clear previously saved folders before adding current ones to avoid duplicates
                DatabaseHelper.ClearSavedFolders();


                foreach (var folder in folders)
                {
                    DatabaseHelper.AddFolder(folder);

                    try
                    {
                        var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            var ext = Path.GetExtension(file).ToLower();
                            if (ext is ".jpg" or ".png" or ".gif" or ".mp4" or ".mov" or ".avi")
                            {
                                allFiles.Add(file);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // optionally notify user or ignore inaccessible folders
                    }
                }

                int processed = 0;
                foreach (var file in allFiles)
                {
                    DatabaseHelper.InsertOrUpdateMedia(file);
                    processed++;
                    progressBar.Value = (int)((processed / (double)allFiles.Count) * 100);
                    await Task.Delay(10); // simulate processing delay
                }

                new MainForm().Show();
                this.Hide();
            };

            this.Controls.Add(listBox);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnRemove);
            this.Controls.Add(btnProceed);
            this.Controls.Add(progressBar);
        }

        private void LoadSavedFolders()
        {
            DatabaseHelper.InitializeDatabase();

            var savedFolders = DatabaseHelper.GetAllFolders();
            foreach (var folder in savedFolders)
            {
                if (!folders.Contains(folder))
                {
                    folders.Add(folder);
                    listBox.Items.Add(folder);
                }
            }
        }

       
    }
}
