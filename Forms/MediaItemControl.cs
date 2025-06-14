﻿using MyAwesomeMediaManager.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MyAwesomeMediaManager.Forms
{
    public class MediaItemControl : UserControl
    {
        private PictureBox thumbnailBox;
        private StarRatingControl ratingControl;
        private ThumbnailPreviewForm? previewForm;

        private int padding = 10;

        public string FilePath { get; private set; }

        public event EventHandler<int>? RatingChanged;

        public MediaItemControl(string filePath, int initialRating = 0)
        {
            FilePath = filePath;
            Margin = new Padding(10);
            DoubleBuffered = true;

            BackColor = initialRating > 0 ? Color.FromArgb(45, 45, 45) : Color.FromArgb(70, 60, 30);
            ForeColor = Color.White;
            BorderStyle = BorderStyle.None;


            thumbnailBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            ratingControl = new StarRatingControl
            {
                StarCount = 5,
                CurrentRating = initialRating,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.Gold
            };

            ratingControl.RatingChanged += (s, newRating) =>
            {
                RatingChanged?.Invoke(this, newRating);
                if (newRating > 0)
                    this.BackColor = Color.FromArgb(45, 45, 45);
            };

            try
            {
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp")
                {
                    // Image files: load directly as before
                    thumbnailBox.Image = Image.FromFile(filePath);
                }
                else if (ext is ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv" or ".flv") // add your video extensions here
                {
                    // Set a reasonable max size based on the current thumbnailBox size, or a fallback
                    bool isVideo = ext is ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv" or ".flv";
                    int maxThumbWidth = isVideo ? 400 : thumbnailBox.Width > 0 ? thumbnailBox.Width : 256;
                    int maxThumbHeight = isVideo ? 300 : thumbnailBox.Height > 0 ? thumbnailBox.Height : 256;
                    int width = isVideo ? 640 : maxThumbWidth;
                    int height = isVideo ? 360 : maxThumbHeight;

                    var thumb = ThumbnailHelper.GetThumbnail(FilePath, width, height);

                    thumbnailBox.Image = thumb ?? SystemIcons.Application.ToBitmap();

                }
                else
                {
                    // Other files: plain shell thumbnail with no border
                    var thumb = MyAwesomeMediaManager.Helpers.ThumbnailHelper.GetThumbnail(filePath, 640, 640);
                    thumbnailBox.Image = thumb ?? SystemIcons.Application.ToBitmap();
                }
            }
            catch
            {
                thumbnailBox.Image = SystemIcons.Error.ToBitmap();
            }

            Controls.Add(thumbnailBox);
            Controls.Add(ratingControl);

            // Unified hover handling for tile and thumbnail
            this.MouseEnter += (s, e) => UpdateHoverState();
            this.MouseMove += (s, e) => UpdateHoverState();
            this.MouseLeave += (s, e) => HidePreview();

            thumbnailBox.MouseEnter += (s, e) => UpdateHoverState();
            thumbnailBox.MouseMove += (s, e) => UpdateHoverState();
            thumbnailBox.MouseLeave += (s, e) => HidePreview();

            ratingControl.MouseEnter += (s, e) => HidePreview();
            ratingControl.MouseLeave += (s, e) => UpdateHoverState();

            this.Click += (s, e) => OnThumbnailClicked();
            thumbnailBox.Click += (s, e) => OnThumbnailClicked();

            this.Resize += (s, e) => {
                UpdateLayout();
                LoadThumbnail(); // Reload thumbnail if size changed
            };

            UpdateLayout();
        }


        private void UpdateLayout()
        {
            if (ClientSize.Width < 100 || ClientSize.Height < 100 || thumbnailBox.Image == null)
                return;

            int padding = 10;
            int availableWidth = ClientSize.Width - padding * 2;

            // Fixed height for the star rating
            int ratingHeight = (int)(ClientSize.Height * 0.18); // or use a constant like 24
            int ratingTop = ClientSize.Height - ratingHeight - padding;

            // Space above the stars for the thumbnail
            int spaceAboveStars = ratingTop - padding;

            // Scale image to fit within available space above the stars
            Size imgSize = thumbnailBox.Image.Size;
            Size scaled = ThumbnailHelper.GetScaledSize(imgSize, availableWidth, spaceAboveStars);

            // Center thumbnail vertically in the space above stars
            int thumbTop = padding + (spaceAboveStars - scaled.Height) / 2;

            thumbnailBox.SetBounds(
                (ClientSize.Width - scaled.Width) / 2,
                thumbTop,
                scaled.Width,
                scaled.Height
            );

            ratingControl.SetBounds(
                padding,
                ratingTop,
                availableWidth,
                ratingHeight
            );
        }

        private void LoadThumbnail()
        {
            try
            {
                string ext = Path.GetExtension(FilePath).ToLower();
                bool isVideo = ext is ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv" or ".flv";
                int maxThumbWidth = isVideo ? 400 : thumbnailBox.Width > 0 ? thumbnailBox.Width : 256;
                int maxThumbHeight = isVideo ? 300 : thumbnailBox.Height > 0 ? thumbnailBox.Height : 256;
                int width = isVideo ? 640 : maxThumbWidth;
                int height = isVideo ? 360 : maxThumbHeight;

                var thumb = ThumbnailHelper.GetThumbnail(FilePath, width, height);

                thumbnailBox.Image = thumb ?? SystemIcons.Application.ToBitmap();
            }
            catch
            {
                thumbnailBox.Image = SystemIcons.Error.ToBitmap();
            }
        }

        private void ShowPreview()
        {
            if (previewForm != null) return;

            string ext = Path.GetExtension(FilePath).ToLower();
            Image previewImage;

            if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp")
            {
                try
                {
                    previewImage = Image.FromFile(FilePath);
                }
                catch
                {
                    previewImage = SystemIcons.Error.ToBitmap();
                }
            }
            else
            {
                if (thumbnailBox.Image == null) return;
                previewImage = thumbnailBox.Image;
            }

            var mainForm = this.FindForm();
            if (mainForm == null) return;

            int previewHeight = (int)(mainForm.ClientSize.Height * 0.8f);
            float aspectRatio = (float)previewImage.Width / previewImage.Height;
            int previewWidth = (int)(previewHeight * aspectRatio);

            previewForm = new ThumbnailPreviewForm(previewImage, new Size(previewWidth, previewHeight));
            previewForm.ShowNear(this);
        }

        private void HidePreview()
        {
            if (previewForm != null)
            {
                previewForm.Close();
                previewForm = null;
            }
        }

        private void UpdateHoverState()
        {
            Point pos = this.PointToClient(Cursor.Position);
            bool overTile = this.ClientRectangle.Contains(pos);
            bool overRating = ratingControl.Bounds.Contains(pos);

            if (overTile && !overRating)
            {
                ShowPreview();
            }
            else
            {
                HidePreview();
            }
        }


        private void OnThumbnailClicked()
        {
            // Hide any preview popup before opening the modal viewer
            HidePreview();

            var modal = new MediaModalForm(FilePath, FindForm());
            modal.Show();
            // Ensure the modal stays above other windows
            modal.BringToFront();
        }

    }
}
