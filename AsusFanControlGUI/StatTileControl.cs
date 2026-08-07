using System.Drawing;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class StatTileControl : UserControl
    {
        private readonly CardPanel surface;
        private readonly Panel accentBar;
        private readonly Label titleLabel;
        private readonly Label valueLabel;
        private readonly Label detailLabel;

        public StatTileControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 12, 12);
            MinimumSize = new Size(170, 96);
            Size = new Size(190, 104);

            surface = new CardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceColor = Theme.CardAlt,
                BorderColor = Theme.BorderSoft,
                CornerRadius = 16,
                Padding = new Padding(14)
            };

            accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Theme.Accent
            };

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            titleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = "Tile"
            };

            valueLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.ValueFont,
                Margin = new Padding(0, 0, 0, 2),
                Text = "—"
            };

            detailLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0),
                Text = string.Empty
            };

            stack.Controls.Add(titleLabel, 0, 0);
            stack.Controls.Add(valueLabel, 0, 1);
            stack.Controls.Add(detailLabel, 0, 2);

            surface.Controls.Add(stack);
            surface.Controls.Add(accentBar);
            Controls.Add(surface);
        }

        public string TileTitle
        {
            get { return titleLabel.Text; }
            set { titleLabel.Text = value; }
        }

        public string ValueText
        {
            get { return valueLabel.Text; }
            set { valueLabel.Text = string.IsNullOrWhiteSpace(value) ? "—" : value; }
        }

        public string DetailText
        {
            get { return detailLabel.Text; }
            set { detailLabel.Text = value ?? string.Empty; }
        }

        public Color AccentColor
        {
            get { return accentBar.BackColor; }
            set { accentBar.BackColor = value; }
        }

        public Color ValueColor
        {
            get { return valueLabel.ForeColor; }
            set { valueLabel.ForeColor = value; }
        }
    }
}
