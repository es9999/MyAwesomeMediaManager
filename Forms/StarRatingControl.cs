using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAwesomeMediaManager.Forms
{
    public class StarRatingControl : Control
    {
        private int starCount = 5;
        private int currentRating = 0;
        private int hoverRating = 0;

        public event EventHandler<int>? RatingChanged;

        public int StarCount
        {
            get => starCount;
            set
            {
                starCount = value;
                Invalidate();
            }
        }

        public int CurrentRating
        {
            get => currentRating;
            set
            {
                if (value < 0) value = 0;
                if (value > StarCount) value = StarCount;
                currentRating = value;
                Invalidate();
            }
        }

        public StarRatingControl()
        {
            this.DoubleBuffered = true;
            this.Height = 40;
            this.Width = 200;

            this.MouseMove += StarRatingControl_MouseMove;
            this.MouseLeave += StarRatingControl_MouseLeave;
            this.MouseClick += StarRatingControl_MouseClick;
        }

        private void StarRatingControl_MouseClick(object sender, MouseEventArgs e)
        {
            int star = GetStarIndexAtPoint(e.Location);
            if (star != 0)
            {
                CurrentRating = star;
                RatingChanged?.Invoke(this, CurrentRating);
            }
        }

        private void StarRatingControl_MouseLeave(object sender, EventArgs e)
        {
            hoverRating = 0;
            Invalidate();
        }

        private void StarRatingControl_MouseMove(object sender, MouseEventArgs e)
        {
            int star = GetStarIndexAtPoint(e.Location);
            if (hoverRating != star)
            {
                hoverRating = star;
                Invalidate();
            }
        }

        private int GetStarIndexAtPoint(Point p)
        {
            int starWidth = this.Width / StarCount;
            int index = p.X / starWidth + 1;
            if (index < 1 || index > StarCount) return 0;
            return index;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int ratingToShow = hoverRating > 0 ? hoverRating : currentRating;

            float starSize = this.Height * 0.7f; // Make stars 70% of height
            float spacing = this.Width / (float)StarCount;
            float offsetY = (this.Height - starSize) / 2f;

            for (int i = 0; i < StarCount; i++)
            {
                float centerX = spacing * i + spacing / 2f;
                float centerY = offsetY + starSize / 2f;
                bool filled = (i + 1) <= ratingToShow;

                DrawStar(e.Graphics, new PointF(centerX, centerY), starSize / 2f, filled);
            }
        }

        private void DrawStar(Graphics g, PointF center, float radius, bool filled)
        {
            PointF[] pts = new PointF[10];
            double step = Math.PI / 5;
            double rot = -Math.PI / 2;

            for (int i = 0; i < 10; i++)
            {
                double r = (i % 2 == 0) ? radius : radius / 2.5;
                pts[i] = new PointF(
                    center.X + (float)(r * Math.Cos(rot + step * i)),
                    center.Y + (float)(r * Math.Sin(rot + step * i))
                );
            }

            Color fillColor = filled ? Color.Gold : Color.White;
            Color outlineColor = Color.Black;

            using (Brush brush = new SolidBrush(fillColor))
            using (Pen pen = new Pen(outlineColor, 1.5f))
            {
                g.FillPolygon(brush, pts);
                g.DrawPolygon(pen, pts);
            }
        }


    }
}
