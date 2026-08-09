using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class Theme
    {
        public static readonly Color Window = Color.FromArgb(6, 8, 12);
        public static readonly Color Card = Color.FromArgb(15, 20, 28);
        public static readonly Color CardAlt = Color.FromArgb(21, 27, 37);
        public static readonly Color Border = Color.FromArgb(49, 58, 72);
        public static readonly Color BorderSoft = Color.FromArgb(31, 38, 50);
        public static readonly Color Text = Color.FromArgb(242, 247, 252);
        public static readonly Color Muted = Color.FromArgb(153, 166, 182);
        public static readonly Color Accent = Color.FromArgb(83, 242, 150);
        public static readonly Color AccentHover = Color.FromArgb(120, 255, 186);
        public static readonly Color AccentDark = Color.FromArgb(16, 74, 49);
        public static readonly Color Success = Color.FromArgb(67, 214, 141);
        public static readonly Color Warning = Color.FromArgb(255, 184, 84);
        public static readonly Color Danger = Color.FromArgb(239, 82, 97);
        public static readonly Color Info = Color.FromArgb(78, 171, 255);
        public static readonly Color InfoDark = Color.FromArgb(15, 44, 78);

        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
        public static readonly Font SectionFont = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font HeroFont = new Font("Segoe UI Semibold", 30f, FontStyle.Bold);
        public static readonly Font ValueFont = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
        public static readonly Font DisplayFont = new Font("Segoe UI Semibold", 24f, FontStyle.Bold);

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
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = accent ? AccentDark : Border;
            button.FlatAppearance.MouseDownBackColor = accent ? AccentDark : CardAlt;
            button.FlatAppearance.MouseOverBackColor = accent ? AccentHover : CardAlt;
            button.BackColor = accent ? Accent : CardAlt;
            button.ForeColor = Text;
            button.Font = accent ? SectionFont : BodyFont;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Padding = new Padding(14, 7, 14, 7);
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

        public static Color Blend(Color start, Color end, float amount)
        {
            if (amount < 0f)
            {
                amount = 0f;
            }
            else if (amount > 1f)
            {
                amount = 1f;
            }

            return Color.FromArgb(
                (int)(start.A + ((end.A - start.A) * amount)),
                (int)(start.R + ((end.R - start.R) * amount)),
                (int)(start.G + ((end.G - start.G) * amount)),
                (int)(start.B + ((end.B - start.B) * amount)));
        }

        public static Color WithAlpha(Color color, int alpha)
        {
            if (alpha < 0)
            {
                alpha = 0;
            }
            else if (alpha > 255)
            {
                alpha = 255;
            }

            return Color.FromArgb(alpha, color);
        }
    }
}
