using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Font = System.Drawing.Font;
using Image = System.Drawing.Image;
using VLCMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace MyAwesomeMediaManager.Forms
{
    public class MediaModalForm : Form
    {
        private LibVLC? _libVLC;
        private VLCMediaPlayer? _mediaPlayer;
        private VideoView? _videoView;
        private PictureBox? _imageBox;
        private Panel? _controlPanel;
        private Button? _closeButton;
        private Button? _fullscreenToggleButton;
        private Timer? _seekTimer;
        private bool _isFullscreen = false;
        private Rectangle _normalBounds;
        private TrackBar? _volumeBar;


        public MediaModalForm(string filePath, Form mainForm)
        {
            InitializeForm(filePath, mainForm);
            LoadMedia(filePath);
            this.KeyPreview = true;
            this.KeyDown += MediaModalForm_KeyDown;
        }

        private void InitializeForm(string filePath, Form mainForm)
        {
            this.FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing/moving for modeless
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen; // Not CenterParent
            this.Size = new Size((int)(mainForm.Width * 0.9), (int)(mainForm.Height * 0.9));
            _normalBounds = this.Bounds;

            // Remove dimming and disabling of mainForm
            // Remove TopMost so user can interact freely with other windows
            this.TopMost = false;

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Space)
                {
                    if (_mediaPlayer?.IsPlaying == true)
                        _mediaPlayer.Pause();
                    else
                        _mediaPlayer?.Play();
                }
                else if (e.KeyCode == Keys.Escape && _isFullscreen)
                {
                    ExitFullscreen();
                }
                else if (e.KeyCode == Keys.Right)
                {
                    _mediaPlayer!.Time += 5000; // seek forward 5s
                }
                else if (e.KeyCode == Keys.Left)
                {
                    _mediaPlayer!.Time -= 5000; // seek backward 5s
                }
                else if (e.KeyCode == Keys.Up)
                {
                    _mediaPlayer!.Volume = Math.Min(100, _mediaPlayer.Volume + 10);
                }
                else if (e.KeyCode == Keys.Down)
                {
                    _mediaPlayer!.Volume = Math.Max(0, _mediaPlayer.Volume - 10);
                }
            };

            // Close button (top-right)
            _closeButton = new Button
            {
                Text = "X",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(this.ClientSize.Width - 50, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _closeButton.FlatAppearance.BorderSize = 0;
            _closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(_closeButton);

            // Fullscreen toggle button (top-right, left of Close)
            _fullscreenToggleButton = new Button
            {
                Text = "Fullscreen",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(15, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _fullscreenToggleButton.FlatAppearance.BorderSize = 0;
            _fullscreenToggleButton.Click += (s, e) => ToggleFullscreen();
            this.Controls.Add(_fullscreenToggleButton);

            _closeButton.BringToFront();
            _fullscreenToggleButton.BringToFront();

            // Remove KeyDown handler for Escape (optional)
            // If you want Escape to close popup, you can override ProcessCmdKey instead.

            this.Resize += (s, e) =>
            {
                // Keep buttons positioned correctly on resize
                _closeButton.Location = new Point(this.ClientSize.Width - 50, 10);
                _fullscreenToggleButton.Location = new Point(15, 15);
                if (_controlPanel != null)
                {
                    _controlPanel.Width = this.ClientSize.Width;
                }
            };
        }

        private void AddPlaybackControls(bool showPlaybackControls)
        {
            if (_controlPanel != null)
                this.Controls.Remove(_controlPanel);

            _controlPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            this.Controls.Add(_controlPanel);


            if (!showPlaybackControls)
            {
                // For images, only show fullscreen toggle and return
                return;
            }

            var playPauseButton = new Button
            {
                Text = "Pause",
                Width = 60,
                Height = 30,
                Location = new Point(10, 15)
            };
            playPauseButton.Click += (s, e) =>
            {
                if (_mediaPlayer == null) return;
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    playPauseButton.Text = "Play";
                }
                else
                {
                    _mediaPlayer.Play();
                    playPauseButton.Text = "Pause";
                }
            };
            _controlPanel.Controls.Add(playPauseButton);

            var stopButton = new Button
            {
                Text = "Stop",
                Width = 60,
                Height = 30,
                Location = new Point(80, 15)
            };
            stopButton.Click += (s, e) =>
            {
                _mediaPlayer?.Stop();
                playPauseButton.Text = "Play";
            };
            _controlPanel.Controls.Add(stopButton);

            var timeLabel = new Label
            {
                Text = "00:00 / 00:00",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(150, 20)
            };
            _controlPanel.Controls.Add(timeLabel);

            var seekBar = new TrackBar
            {
                Left = 250,
                Top = 20,
                Width = this.ClientSize.Width - 400,
                TickStyle = TickStyle.None,
                Minimum = 0,
                Maximum = 1000,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _controlPanel.Controls.Add(seekBar);

            _volumeBar = new TrackBar
            {
                Width = 100,
                Height = 30,
                Location = new Point(this.ClientSize.Width - 110, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Minimum = 0,
                Maximum = 100,
                Value = 80,
                TickFrequency = 10
            };
            _volumeBar.Scroll += (s, e) =>
            {
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = _volumeBar.Value;
            };
            _controlPanel.Controls.Add(_volumeBar);


            bool seeking = false;

            seekBar.MouseDown += (s, e) => seeking = true;
            seekBar.MouseUp += (s, e) =>
            {
                if (_mediaPlayer != null && _mediaPlayer.Length > 0)
                {
                    long newTime = seekBar.Value * _mediaPlayer.Length / 1000;
                    _mediaPlayer.Time = newTime;
                }
                seeking = false;
            };

            _seekTimer = new Timer { Interval = 500 };
            _seekTimer.Tick += (s, e) =>
            {
                if (_mediaPlayer != null && _mediaPlayer.Length > 0 && !seeking)
                {
                    long currentTime = _mediaPlayer.Time;
                    long duration = _mediaPlayer.Length;
                    int pos = (int)(currentTime * 1000 / duration);

                    if (pos >= 0 && pos <= 1000)
                        seekBar.Value = pos;

                    timeLabel.Text = $"{FormatTime(currentTime)} / {FormatTime(duration)}";
                }
            };
            _seekTimer.Start();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int seekStepMs = 5000;
            const int volumeStep = 5;

            // Prevent null reference
            if (_mediaPlayer == null)
                return base.ProcessCmdKey(ref msg, keyData);

            // Toggle play/pause
            if (keyData == Keys.Space)
            {
                TogglePlayPause();
                return true;
            }

            // Exit modal
            if (keyData == Keys.Escape)
            {
                if (_isFullscreen)
                    ExitFullscreen();
                else
                    this.Close();
                return true;
            }

            // Seek backward
            if (keyData == Keys.Left)
            {
                long newTime = Math.Max(0, _mediaPlayer.Time - seekStepMs);
                _mediaPlayer.Time = newTime;
                return true;
            }

            // Seek forward
            if (keyData == Keys.Right)
            {
                long newTime = Math.Min(_mediaPlayer.Length, _mediaPlayer.Time + seekStepMs);
                _mediaPlayer.Time = newTime;
                return true;
            }

            // Volume up
            if (keyData == Keys.Up)
            {
                int newVolume = Math.Min(100, _mediaPlayer.Volume + volumeStep);
                _mediaPlayer.Volume = newVolume;
                if (_volumeBar != null)
                    _volumeBar.Value = newVolume;
                return true;
            }

            // Volume down
            if (keyData == Keys.Down)
            {
                int newVolume = Math.Max(0, _mediaPlayer.Volume - volumeStep);
                _mediaPlayer.Volume = newVolume;
                if (_volumeBar != null)
                    _volumeBar.Value = newVolume;
                return true;
            }

            // Alt+Enter toggles fullscreen
            if (keyData == (Keys.Alt | Keys.Enter))
            {
                ToggleFullscreen();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        private void TogglePlayPause()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Pause();
            else
                _mediaPlayer.Play();

            UpdatePlaybackControlsUI(); // update play/pause button icon etc.
        }
        
        private void UpdatePlaybackControlsUI()
        {
            if (_controlPanel == null)
                return;

            var playPauseButton = _controlPanel.Controls["playPauseButton"] as Button;
            if (playPauseButton != null)
            {
                playPauseButton.Text = _mediaPlayer.IsPlaying ? "Pause" : "Play";
            }
        }

        private void MediaModalForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (_mediaPlayer == null || !_mediaPlayer.IsPlaying)
                return;

            const int seekStepMs = 5000;
            const int volumeStep = 5;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    {
                        var newTime = Math.Max(0, _mediaPlayer.Time - seekStepMs);
                        _mediaPlayer.Time = (long)newTime;
                        e.Handled = true;
                        break;
                    }
                case Keys.Right:
                    {
                        var newTime = Math.Min(_mediaPlayer.Length, _mediaPlayer.Time + seekStepMs);
                        _mediaPlayer.Time = (long)newTime;
                        e.Handled = true;
                        break;
                    }
                case Keys.Down:
                    {
                        int newVolume = Math.Min(100, _mediaPlayer.Volume + volumeStep);
                        _mediaPlayer.Volume = newVolume;
                        e.Handled = true;
                        break;
                    }
                case Keys.Up:
                    {
                        int newVolume = Math.Max(0, _mediaPlayer.Volume - volumeStep);
                        _mediaPlayer.Volume = newVolume;
                        e.Handled = true;
                        break;
                    }
            }
        }


        private bool IsVideoFile(string filePath)
        {
            var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm"
    };
            return videoExtensions.Contains(Path.GetExtension(filePath));
        }

        private bool IsImageFile(string filePath)
        {
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff"
    };
            return imageExtensions.Contains(Path.GetExtension(filePath));
        }



        private string FormatTime(long milliseconds)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);
            return t.Hours > 0 ? t.ToString(@"hh\:mm\:ss") : t.ToString(@"mm\:ss");
        }


        private void LoadMedia(string filePath)
        {
            if (!File.Exists(filePath)) return;

            bool isVideo = IsVideoFile(filePath);
            bool isImage = IsImageFile(filePath);

            if (_videoView != null)
            {
                this.Controls.Remove(_videoView);
                _videoView.Dispose();
                _videoView = null;
            }

            if (_imageBox != null)
            {
                this.Controls.Remove(_imageBox);
                _imageBox.Dispose();
                _imageBox = null;
            }

            if (isVideo)
            {
                _libVLC = new LibVLC();
                _mediaPlayer = new VLCMediaPlayer(_libVLC);

                _videoView = new VideoView
                {
                    MediaPlayer = _mediaPlayer,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black
                };
                this.Controls.Add(_videoView);
                _videoView.BringToFront();

                var media = new Media(_libVLC, new Uri(filePath));
                _mediaPlayer.Play(media);
            }
            else if (isImage)
            {
                _imageBox = new PictureBox
                {
                    Image = Image.FromFile(filePath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black
                };
                this.Controls.Add(_imageBox);
                _imageBox.BringToFront();
            }

            AddPlaybackControls(showPlaybackControls: isVideo);
            _controlPanel?.BringToFront();

            _closeButton?.BringToFront();
            _fullscreenToggleButton?.BringToFront();
        }



        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _normalBounds = this.Bounds;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal; // Reset state before fullscreen
                this.Bounds = Screen.FromControl(this).Bounds;
                _fullscreenToggleButton!.Text = "Exit Fullscreen";
                _isFullscreen = true;

                _controlPanel?.BringToFront();
                _closeButton?.BringToFront();
                _fullscreenToggleButton?.BringToFront();
            }
            else
            {
                ExitFullscreen();
            }
        }

        private void ExitFullscreen()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Bounds = _normalBounds;
            _fullscreenToggleButton!.Text = "Fullscreen";
            _isFullscreen = false;

            _controlPanel?.BringToFront();
            _closeButton?.BringToFront();
            _fullscreenToggleButton?.BringToFront();
        }


        private Timer? _autoHideTimer;
        private Point _lastMousePosition;

        private void StartAutoHideControls()
        {
            if (_autoHideTimer == null)
            {
                _autoHideTimer = new Timer { Interval = 3000 }; // 3 seconds
                _autoHideTimer.Tick += (s, e) =>
                {
                    if (Cursor.Position == _lastMousePosition)
                    {
                        Cursor.Hide();
                        _controlPanel?.Hide();
                    }
                };
            }

            this.MouseMove -= ResetAutoHideTimer;

            if (_videoView != null)
                _videoView.MouseMove -= ResetAutoHideTimer;

            if (_controlPanel != null)
                _controlPanel.MouseMove -= ResetAutoHideTimer;


            _lastMousePosition = Cursor.Position;
            _autoHideTimer.Start();
        }

        private void StopAutoHideControls()
        {
            if (_autoHideTimer != null)
            {
                _autoHideTimer.Stop();
                _autoHideTimer.Dispose();
                _autoHideTimer = null;
            }

            this.MouseMove -= ResetAutoHideTimer;

            if (_videoView != null)
                _videoView.MouseMove -= ResetAutoHideTimer;

            if (_controlPanel != null)
                _controlPanel.MouseMove -= ResetAutoHideTimer;


            Cursor.Show();
            _controlPanel?.Show();
        }

        private void ResetAutoHideTimer(object? sender, EventArgs e)
        {
            _lastMousePosition = Cursor.Position;
            Cursor.Show();
            _controlPanel?.Show();
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            _seekTimer?.Dispose();
            _imageBox?.Dispose();
            _videoView?.Dispose();
        }
    }
}
