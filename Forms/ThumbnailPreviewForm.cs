using System;
using System.Drawing;
using System.Windows.Forms;

public class ThumbnailPreviewForm : Form
{
    private PictureBox previewBox;

    public ThumbnailPreviewForm(Image image, Size maxSize)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;

        previewBox = new PictureBox
        {
            Image = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black,
            Dock = DockStyle.Fill
        };

        Controls.Add(previewBox);
        Size = maxSize;

        Deactivate += (s, e) => Close();
    }

    public void ShowNear(Control parent)
    {
        var mainForm = parent.FindForm();
        if (mainForm == null) return;

        Rectangle appBounds = mainForm.RectangleToScreen(mainForm.ClientRectangle);
        Point parentScreenPos = parent.PointToScreen(Point.Empty);

        int maxPopupWidth = (int)(appBounds.Width * 0.8);
        int maxPopupHeight = (int)(appBounds.Height * 0.8);

        // Calculate the preview size to fit within maxPopupWidth x maxPopupHeight, preserving aspect ratio
        int imgW = previewBox.Image.Width;
        int imgH = previewBox.Image.Height;
        float ratioX = (float)maxPopupWidth / imgW;
        float ratioY = (float)maxPopupHeight / imgH;
        float ratio = Math.Min(1f, Math.Min(ratioX, ratioY));

        int popupWidth = (int)(imgW * ratio);
        int popupHeight = (int)(imgH * ratio);

        // Try positioning to the right of the thumbnail
        int proposedLeft = parentScreenPos.X + parent.Width + 10;
        int proposedTop = parentScreenPos.Y;

        // If it would overflow right edge, try to left of the tile
        if (proposedLeft + popupWidth > appBounds.Right)
        {
            proposedLeft = parentScreenPos.X - popupWidth - 10;
        }

        // Clamp horizontally to stay within app window
        if (proposedLeft < appBounds.Left)
            proposedLeft = appBounds.Left;
        if (proposedLeft + popupWidth > appBounds.Right)
            proposedLeft = appBounds.Right - popupWidth;

        // Clamp vertically to stay within app window
        if (proposedTop + popupHeight > appBounds.Bottom)
            proposedTop = appBounds.Bottom - popupHeight;
        if (proposedTop < appBounds.Top)
            proposedTop = appBounds.Top;

        Size = new Size(popupWidth, popupHeight);
        Location = new Point(proposedLeft, proposedTop);
        Show();
    }
}
