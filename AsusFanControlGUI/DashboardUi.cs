using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class DashboardTheme
    {
        public static readonly Color Background = Color.FromArgb(11, 16, 24);
        public static readonly Color BackgroundAlt = Color.FromArgb(15, 22, 34);
        public static readonly Color Card = Color.FromArgb(18, 27, 40);
        public static readonly Color CardElevated = Color.FromArgb(22, 33, 49);
        public static readonly Color CardBorder = Color.FromArgb(45, 58, 79);
        public static readonly Color Accent = Color.FromArgb(0, 200, 240);
        public static readonly Color AccentSoft = Color.FromArgb(15, 118, 160);
        public static readonly Color Success = Color.FromArgb(91, 220, 154);
        public static readonly Color Warning = Color.FromArgb(255, 192, 82);
        public static readonly Color Danger = Color.FromArgb(255, 98, 130);
        public static readonly Color Text = Color.FromArgb(240, 245, 252);
        public static readonly Color SubtleText = Color.FromArgb(154, 167, 188);
        public static readonly Color MutedText = Color.FromArgb(107, 120, 141);
        public static readonly Color Divider = Color.FromArgb(37, 48, 66);

        public static readonly Font Title = new Font("Segoe UI Semibold", 19f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Section = new Font("Segoe UI Semibold", 12f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Subtitle = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font ValueLarge = new Font("Segoe UI Semibold", 30f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font ValueMedium = new Font("Segoe UI Semibold", 18f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Body = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Small = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
    }

    internal static class UiGeometry
    {
        public static GraphicsPath RoundRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            path.StartFigure();
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal class RoundedPanel : Panel
    {
        private int cornerRadius = 18;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = DashboardTheme.Card;
            Padding = new Padding(20);
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = Math.Max(0, value);
                UpdateRegion();
                Invalidate();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Paint the full surface ourselves to avoid flicker.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = UiGeometry.RoundRect(bounds, cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen pen = new Pen(DashboardTheme.CardBorder))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = UiGeometry.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), cornerRadius))
            {
                Region = new Region(path);
            }
        }
    }

    internal class ToggleSwitch : Control
    {
        private bool isChecked;

        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(56, 28);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value)
                {
                    return;
                }

                isChecked = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
        }

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            EventHandler handler = CheckedChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color trackColor = Enabled
                ? (Checked ? DashboardTheme.AccentSoft : Color.FromArgb(35, 46, 62))
                : Color.FromArgb(28, 35, 47);

            Color thumbColor = Enabled ? Color.White : Color.FromArgb(170, 178, 191);

            Rectangle trackBounds = new Rectangle(1, 5, Width - 2, Height - 10);
            Rectangle thumbBounds = Checked
                ? new Rectangle(Width - Height + 4, 4, Height - 8, Height - 8)
                : new Rectangle(4, 4, Height - 8, Height - 8);

            using (GraphicsPath trackPath = UiGeometry.RoundRect(trackBounds, trackBounds.Height / 2))
            {
                using (SolidBrush trackBrush = new SolidBrush(trackColor))
                {
                    e.Graphics.FillPath(trackBrush, trackPath);
                }
            }

            using (GraphicsPath thumbPath = UiGeometry.RoundRect(thumbBounds, thumbBounds.Height / 2))
            {
                using (SolidBrush thumbBrush = new SolidBrush(thumbColor))
                {
                    e.Graphics.FillPath(thumbBrush, thumbPath);
                }

                using (Pen thumbBorder = new Pen(Color.FromArgb(40, 0, 0, 0)))
                {
                    e.Graphics.DrawPath(thumbBorder, thumbPath);
                }
            }
        }
    }

    internal class BadgeChip : Control
    {
        private int horizontalPadding = 14;
        private int verticalPadding = 6;

        public BadgeChip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = true;
            Font = DashboardTheme.Small;
            ForeColor = DashboardTheme.Text;
            TextColor = DashboardTheme.Text;
            BackColor = DashboardTheme.CardElevated;
            FillColor = DashboardTheme.CardElevated;
            BorderColor = DashboardTheme.CardBorder;
            CornerRadius = 999;
            Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        }

        public int CornerRadius { get; set; }

        public Color FillColor { get; set; }

        public Color BorderColor { get; set; }

        public Color TextColor { get; set; }

        public int HorizontalPadding
        {
            get { return horizontalPadding; }
            set
            {
                horizontalPadding = Math.Max(4, value);
                Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
                PerformLayout();
                Invalidate();
            }
        }

        public int VerticalPadding
        {
            get { return verticalPadding; }
            set
            {
                verticalPadding = Math.Max(2, value);
                Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
                PerformLayout();
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            PerformLayout();
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size textSize = TextRenderer.MeasureText(Text ?? string.Empty, Font);
            return new Size(textSize.Width + (horizontalPadding * 2), textSize.Height + (verticalPadding * 2) + 2);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Paint all content in OnPaint.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = UiGeometry.RoundRect(bounds, Math.Min(CornerRadius, Math.Min(Width, Height) / 2)))
            {
                using (SolidBrush fillBrush = new SolidBrush(FillColor))
                {
                    e.Graphics.FillPath(fillBrush, path);
                }

                using (Pen borderPen = new Pen(BorderColor))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            Rectangle textBounds = Rectangle.Inflate(bounds, -horizontalPadding, -verticalPadding);
            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                textBounds,
                Enabled ? TextColor : DashboardTheme.MutedText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
