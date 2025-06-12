using MyAwesomeMediaManager.Data;
using MyAwesomeMediaManager.Forms;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyAwesomeMediaManager
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

    public partial class MainForm : Form
    {
        // UI elements
        private NoAutoScrollFlowLayoutPanel flowPanel;
        private Panel titleBar;
        private Button closeBtn;
        private Button maxBtn;
        private Button minBtn;

        // State
        private string? selectedFilePath;

        // Constants for native methods and window resizing
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private const int RESIZE_HANDLE_SIZE = 10;

        public MainForm()
        {
            InitializeComponent();

            this.Shown += (s, e) =>
            {
                LayoutThumbnails();
            };

            // Layout thumbnails on resize (including maximize)
            /*this.Resize += (s, e) =>
            {
                LayoutThumbnails();
                flowPanel.Invalidate(); // force redraw
            };*/
            this.Resize += MainForm_Resize;

        }


        private void MainForm_Resize(object? sender, EventArgs e)
        {
            LayoutThumbnails();
        }


        private void InitializeComponent()
        {
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(10); // padding for resize area
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10);
            this.Width = 1200;
            this.Height = 700;
            this.MinimumSize = new Size(400, 300);

            // Title Bar panel
            titleBar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48),
                Cursor = Cursors.Default,
            };
            this.Controls.Add(titleBar);

            // Title Bar drag support
            titleBar.MouseDown += TitleBar_MouseDown;

            // Title Bar double-click maximizes/restores window
            titleBar.DoubleClick += (s, e) =>
            {
                this.WindowState = (this.WindowState == FormWindowState.Maximized)
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            };

            // Close button
            closeBtn = CreateTitleBarButton("✕", AnchorStyles.Top | AnchorStyles.Right, new Point(this.Width - 60, 0));
            closeBtn.Click += (s, e) => this.Close();
            titleBar.Controls.Add(closeBtn);

            // Maximize button
            maxBtn = CreateTitleBarButton("❐", AnchorStyles.Top | AnchorStyles.Right, new Point(this.Width - 100, 0));
            maxBtn.Click += (s, e) =>
            {
                this.WindowState = (this.WindowState == FormWindowState.Maximized)
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            };
            titleBar.Controls.Add(maxBtn);

            // Minimize button
            minBtn = CreateTitleBarButton("─", AnchorStyles.Top | AnchorStyles.Right, new Point(this.Width - 140, 0));
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            titleBar.Controls.Add(minBtn);

            // Enable drag from form background (for borderless window)
            this.MouseDown += Form_MouseDown;

            // Container panel to add padding around flowPanel
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 40, 20, 40), // left, top, right, bottom padding around thumbnails
                BackColor = this.BackColor,
            };
            this.Controls.Add(containerPanel);

            // FlowLayoutPanel for media thumbnails, docked inside containerPanel
            flowPanel = new NoAutoScrollFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = this.BackColor,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(20),  // padding inside the flow panel around thumbnails
                Margin = new Padding(0),    // margin ignored due to docking
            };
            containerPanel.Controls.Add(flowPanel);

            // Load media on startup
            RescanFolders();
            RemoveMissingMediaFiles();
            LoadMediaItems();
        }



        private Button CreateTitleBarButton(string text, AnchorStyles anchor, Point location)
        {
            var btn = new Button
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Width = 40,
                Height = 40,
                Location = location,
                Anchor = anchor,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Make window resizable with borderless style
            int style = NativeMethods.GetWindowLong(this.Handle, GWL_STYLE);
            style |= WS_THICKFRAME;
            NativeMethods.SetWindowLong(this.Handle, GWL_STYLE, style);
        }

        private void RescanFolders()
        {
            var folders = DatabaseHelper.GetAllFolders();

            foreach (var folder in folders)
            {
                try
                {
                    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".jpg" || ext == ".png" || ext == ".gif" || ext == ".mp4" || ext == ".mov" || ext == ".avi")
                        {
                            DatabaseHelper.InsertOrUpdateMedia(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error scanning folder: {folder}\n{ex.Message}", "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RemoveMissingMediaFiles()
        {
            var allFiles = DatabaseHelper.GetAllMedia();

            foreach (var file in allFiles)
            {
                if (!File.Exists(file))
                {
                    DatabaseHelper.DeleteMedia(file);
                }
            }
        }

        private void LoadMediaItems()
        {
            flowPanel.Controls.Clear();

            var items = DatabaseHelper.GetAllMedia();

            foreach (var path in items)
            {
                var meta = DatabaseHelper.GetMediaMetadata(path);

                var mediaControl = new MediaItemControl(path, meta?.Rating ?? 1)
                {
                    Margin = new Padding(10),
                    BackColor = Color.FromArgb(45, 45, 45),
                    // Size will be set dynamically in LayoutThumbnails
                };

                mediaControl.RatingChanged += (s, newRating) =>
                {
                    // Update rating in DB; preserve tags and other metadata
                    DatabaseHelper.UpdateMediaMetadata(path, newRating, false, meta?.Tags ?? "");
                };

                mediaControl.Click += (s, e) =>
                {
                    selectedFilePath = path;
                };

                flowPanel.Controls.Add(mediaControl);
            }

            LayoutThumbnails();
        }

        private void LayoutThumbnails()
        {
            if (flowPanel == null) return;

            int totalWidth = flowPanel.ClientSize.Width;
            if (totalWidth <= 0) return;

            const int columns = 4;
            const int spacing = 20;   // space between thumbnails (FlowLayoutPanel padding)
            const int margin = 10;    // margin around each thumbnail (Padding of each Control)

            // Calculate total spacing between thumbnails for 4 columns:
            // There are 5 spacing gaps if padding + margins, but flow panel has padding,
            // and each control has margin on left and right.
            // So total horizontal space taken by margins and padding:
            // - FlowLayoutPanel padding left + right = 20 + 20 = 40
            // - Each thumbnail margin left + right = 10 + 10 = 20
            // For 4 thumbnails in a row, total margin space is 20 * 4 = 80
            // Total horizontal padding + margin space = 40 + 80 = 120

            // The available width for thumbnails themselves:
            int totalMarginSpace = flowPanel.Padding.Left + flowPanel.Padding.Right + (margin * 2 * columns);
            int availableWidth = totalWidth - totalMarginSpace;

            int thumbnailWidth = availableWidth / columns;
            int thumbnailHeight = thumbnailWidth; // square thumbnails

            foreach (Control ctl in flowPanel.Controls)
            {
                ctl.Width = thumbnailWidth;
                ctl.Height = thumbnailHeight;
                ctl.Margin = new Padding(margin);

                if (ctl is PictureBox pb)
                {
                    pb.SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }

            // Force layout update and redraw
            flowPanel.PerformLayout();
            flowPanel.Invalidate();
        }


        // Override to support resizing for borderless window
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);

                int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                Point pos = PointToClient(new Point(x, y));

                bool left = pos.X <= RESIZE_HANDLE_SIZE;
                bool right = pos.X >= ClientSize.Width - RESIZE_HANDLE_SIZE;
                bool top = pos.Y <= RESIZE_HANDLE_SIZE;
                bool bottom = pos.Y >= ClientSize.Height - RESIZE_HANDLE_SIZE;

                if (left)
                {
                    if (top) m.Result = (IntPtr)HTTOPLEFT;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else m.Result = (IntPtr)HTLEFT;
                    return;
                }
                else if (right)
                {
                    if (top) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else m.Result = (IntPtr)HTRIGHT;
                    return;
                }
                else if (top)
                {
                    m.Result = (IntPtr)HTTOP;
                    return;
                }
                else if (bottom)
                {
                    m.Result = (IntPtr)HTBOTTOM;
                    return;
                }

                return;
            }

            base.WndProc(ref m);
        }

        // P/Invoke declarations for drag-to-move support
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}
