using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class Theme
    {
        public static readonly Color Window = Color.FromArgb(11, 14, 20);
        public static readonly Color Card = Color.FromArgb(20, 24, 31);
        public static readonly Color CardAlt = Color.FromArgb(26, 31, 40);
        public static readonly Color Border = Color.FromArgb(48, 56, 70);
        public static readonly Color BorderSoft = Color.FromArgb(39, 46, 58);
        public static readonly Color Text = Color.FromArgb(235, 240, 247);
        public static readonly Color Muted = Color.FromArgb(155, 165, 178);
        public static readonly Color Accent = Color.FromArgb(223, 42, 58);
        public static readonly Color AccentHover = Color.FromArgb(255, 70, 84);
        public static readonly Color AccentDark = Color.FromArgb(128, 29, 38);
        public static readonly Color Success = Color.FromArgb(87, 197, 121);
        public static readonly Color Warning = Color.FromArgb(240, 178, 62);
        public static readonly Color Danger = Color.FromArgb(232, 79, 89);
        public static readonly Color Info = Color.FromArgb(90, 160, 255);

        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);
        public static readonly Font SectionFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static readonly Font HeroFont = new Font("Segoe UI Semibold", 24f, FontStyle.Bold);
        public static readonly Font ValueFont = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);

        public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();

            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            var safeRadius = radius;
            var maxRadius = Math.Min(bounds.Width, bounds.Height) / 2;
            if (safeRadius > maxRadius)
            {
                safeRadius = maxRadius;
            }

            if (safeRadius <= 1)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            var diameter = safeRadius * 2;
            var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        public static void StyleButton(Button button, bool accent)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = accent ? AccentDark : CardAlt;
            button.FlatAppearance.MouseOverBackColor = accent ? AccentHover : Card;
            button.BackColor = accent ? Accent : CardAlt;
            button.ForeColor = Text;
            button.Font = BodyFont;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Padding = new Padding(12, 6, 12, 6);
            button.Cursor = Cursors.Hand;
        }

        public static void StyleTextBox(TextBoxBase textBox)
        {
            textBox.BackColor = CardAlt;
            textBox.ForeColor = Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = BodyFont;
        }

        public static void StyleNumeric(NumericUpDown numeric)
        {
            numeric.BackColor = CardAlt;
            numeric.ForeColor = Text;
            numeric.BorderStyle = BorderStyle.FixedSingle;
            numeric.Font = BodyFont;
        }
    }
}
