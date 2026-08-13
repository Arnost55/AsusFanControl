using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class ToggleSwitchControl : Control
    {
        private bool isChecked;
        private Color onColor = Theme.Info;
        private Color offColor = Theme.BorderSoft;

        public event EventHandler CheckedChanged;

        public ToggleSwitchControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Theme.SmallFont;
            MinimumSize = new Size(52, 28);
            Size = new Size(52, 28);
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

                if (CheckedChanged != null)
                {
                    CheckedChanged(this, EventArgs.Empty);
                }
            }
        }

        public Color OnColor
        {
            get { return onColor; }
            set
            {
                onColor = value;
                Invalidate();
            }
        }

        public Color OffColor
        {
            get { return offColor; }
            set
            {
                offColor = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            e.Graphics.Clear(BackColor);

            var trackRect = new Rectangle(1, 6, Width - 3, Height - 12);
            var trackColor = Checked ? OnColor : OffColor;
            var trackBorder = Checked ? Theme.InfoDark : Theme.Border;

            using (var path = Theme.CreateRoundedPath(trackRect, trackRect.Height))
            using (var fill = new SolidBrush(trackColor))
            using (var border = new Pen(trackBorder))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }

            var thumbDiameter = Math.Max(14, trackRect.Height - 4);
            var thumbX = Checked ? trackRect.Right - thumbDiameter - 2 : trackRect.Left + 2;
            var thumbRect = new Rectangle(thumbX, trackRect.Top + 2, thumbDiameter, thumbDiameter);

            using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            using (var thumbPath = Theme.CreateRoundedPath(thumbRect, thumbRect.Height))
            {
                var shadowRect = thumbRect;
                shadowRect.Offset(0, 1);
                using (var shadowPath = Theme.CreateRoundedPath(shadowRect, shadowRect.Height))
                {
                    e.Graphics.FillPath(shadow, shadowPath);
                }

                using (var thumbFill = new SolidBrush(Theme.Text))
                using (var thumbBorder = new Pen(Color.FromArgb(80, 255, 255, 255)))
                {
                    e.Graphics.FillPath(thumbFill, thumbPath);
                    e.Graphics.DrawPath(thumbBorder, thumbPath);
                }
            }

            if (Focused)
            {
                using (var focusPen = new Pen(Color.FromArgb(160, Theme.Info)))
                using (var focusPath = Theme.CreateRoundedPath(new Rectangle(0, 4, Width - 1, Height - 9), Height))
                {
                    e.Graphics.DrawPath(focusPen, focusPath);
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == MouseButtons.Left)
            {
                Checked = !Checked;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
        }
    }
}
