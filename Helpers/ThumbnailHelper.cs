using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyAwesomeMediaManager.Helpers
{
    public static class ThumbnailHelper
    {
        private static readonly string CacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyAwesomeMediaManager",
            ".thumbnails"
        );

        static ThumbnailHelper()
        {
            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);
        }

        /// <summary>
        /// Get cached thumbnail or generate and cache it.
        /// Adds film strip style borders for video thumbnails.
        /// </summary>
        public static Image? GetThumbnail(string filePath, int maxWidth, int maxHeight)
        {
            try
            {
                string cacheFile = GetCacheFilePath(filePath);

               /* if (File.Exists(cacheFile))
                {
                    DateTime cacheTime = File.GetLastWriteTimeUtc(cacheFile);
                    DateTime fileTime = File.GetLastWriteTimeUtc(filePath);
                    if (cacheTime >= fileTime)
                        return Image.FromFile(cacheFile);
                }*/

                using var shellFile = ShellFile.FromFilePath(filePath);
                var rawBitmap = shellFile.Thumbnail.ExtraLargeBitmap;
                if (rawBitmap == null)
                    return null;

                Image final;

                if (IsVideoFile(filePath))
                {
                    final = CreateVideoThumbnailWithBorders(rawBitmap, maxWidth, maxHeight);
                }
                else
                {
                    Size scaledSize = GetScaledSize(rawBitmap.Size, maxWidth, maxHeight);
                    final = new Bitmap(rawBitmap, scaledSize);
                }

                //final.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Png);
                if (!ReferenceEquals(final, rawBitmap)) rawBitmap.Dispose();

                return final;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsVideoFile(string filePath)
        {
            string[] videoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".flv", ".webm" };
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return Array.Exists(videoExtensions, v => v == ext);
        }

        private static string GetCacheFilePath(string filePath)
        {
            // Use SHA256 hash of the full file path as cache filename
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(filePath));
            string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return Path.Combine(CacheFolder, $"{hashString}.png");
        }

        public static Image CreateVideoThumbnailWithBorders(Image rawThumbnail, int maxWidth, int maxHeight)
        {
            int borderWidth = 10;
            int holeDiameter = 6;
            int holeSpacing = 12;

            // Compute maximum width available for the actual video image (excluding borders)
            int maxImageWidth = maxWidth - 2 * borderWidth;

            // Scale the image proportionally
            Size scaledImageSize = GetScaledSize(rawThumbnail.Size, maxImageWidth, maxHeight);

            // Now compute final canvas size based on scaled image size + borders
            int canvasWidth = scaledImageSize.Width + 2 * borderWidth;
            int canvasHeight = scaledImageSize.Height;

            Bitmap canvas = new Bitmap(canvasWidth, canvasHeight);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Black);

                // Draw the scaled image
                g.DrawImage(
                    rawThumbnail,
                    new Rectangle(borderWidth, 0, scaledImageSize.Width, scaledImageSize.Height)
                );

                using (Brush borderBrush = new SolidBrush(Color.DarkGray))
                {
                    g.FillRectangle(borderBrush, 0, 0, borderWidth, canvasHeight);
                    g.FillRectangle(borderBrush, canvasWidth - borderWidth, 0, borderWidth, canvasHeight);
                }

                using (Brush holeBrush = new SolidBrush(Color.White))
                {
                    for (int y = holeSpacing / 2; y < canvasHeight; y += holeSpacing)
                    {
                        int holeXLeft = borderWidth / 2 - holeDiameter / 2;
                        g.FillEllipse(holeBrush, holeXLeft, y - holeDiameter / 2, holeDiameter, holeDiameter);

                        int holeXRight = canvasWidth - borderWidth / 2 - holeDiameter / 2;
                        g.FillEllipse(holeBrush, holeXRight, y - holeDiameter / 2, holeDiameter, holeDiameter);
                    }
                }
            }

            return canvas;
        }



        public static Size GetScaledSize(Size originalSize, int maxWidth, int maxHeight)
        {
            float ratioX = (float)maxWidth / originalSize.Width;
            float ratioY = (float)maxHeight / originalSize.Height;
            float ratio = Math.Min(ratioX, ratioY);

            return new Size((int)(originalSize.Width * ratio), (int)(originalSize.Height * ratio));
        }

    }
}
