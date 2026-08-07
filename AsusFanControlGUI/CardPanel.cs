using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal class CardPanel : Panel
    {
        private int cornerRadius = 16;
        private Color surfaceColor = Theme.Card;
        private Color borderColor = Theme.Border;

        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = surfaceColor;
            Padding = new Padding(16);
            Margin = new Padding(0);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = value < 0 ? 0 : value;
                UpdateRegion();
                Invalidate();
            }
        }

        public Color SurfaceColor
        {
            get { return surfaceColor; }
            set
            {
                surfaceColor = value;
                BackColor = surfaceColor;
                Invalidate();
            }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                Invalidate();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(BorderColor))
            using (var path = Theme.CreateRoundedPath(bounds, cornerRadius))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 1 || Height <= 1)
            {
                return;
            }

            using (var path = Theme.CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), cornerRadius))
            {
                Region = new Region(path);
            }
        }
    }
}
