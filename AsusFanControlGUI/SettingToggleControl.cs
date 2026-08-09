using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class SettingToggleControl : UserControl
    {
        private readonly CardPanel surface;
        private readonly Panel indicatorBar;
        private readonly Label titleLabel;
        private readonly Label descriptionLabel;
        private readonly ToggleSwitchControl toggleSwitch;
        private bool suppressCheckedChanged;

        public event EventHandler CheckedChanged;

        public SettingToggleControl()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 0, 10);
            Size = new Size(320, 88);
            MinimumSize = new Size(280, 88);

            surface = new CardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceColor = Theme.Card,
                BorderColor = Theme.BorderSoft,
                CornerRadius = 16,
                Padding = new Padding(14, 12, 14, 12)
            };

            indicatorBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Theme.BorderSoft
            };

            titleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.SectionFont,
                Margin = new Padding(0, 0, 0, 3),
                Text = "Setting"
            };

            descriptionLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                MaximumSize = new Size(230, 0),
                Margin = new Padding(0),
                Text = string.Empty
            };

            toggleSwitch = new ToggleSwitchControl
            {
                Anchor = AnchorStyles.Right,
                Checked = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            toggleSwitch.CheckedChanged += ToggleSwitch_CheckedChanged;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var textStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            textStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textStack.Controls.Add(titleLabel, 0, 0);
            textStack.Controls.Add(descriptionLabel, 0, 1);

            layout.Controls.Add(textStack, 0, 0);
            layout.Controls.Add(toggleSwitch, 1, 0);

            surface.Controls.Add(layout);
            surface.Controls.Add(indicatorBar);
            Controls.Add(surface);

            WireClick(surface);
            WireClick(textStack);
            WireClick(titleLabel);
            WireClick(descriptionLabel);
        }

        public string ToggleTitle
        {
            get { return titleLabel.Text; }
            set { titleLabel.Text = value; }
        }

        public string ToggleDescription
        {
            get { return descriptionLabel.Text; }
            set { descriptionLabel.Text = value ?? string.Empty; }
        }

        public bool Checked
        {
            get { return toggleSwitch.Checked; }
            set
            {
                suppressCheckedChanged = true;
                toggleSwitch.Checked = value;
                suppressCheckedChanged = false;
                UpdateVisualState();
            }
        }

        private void ToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            if (!suppressCheckedChanged)
            {
                UpdateVisualState();

                if (CheckedChanged != null)
                {
                    CheckedChanged(this, EventArgs.Empty);
                }
            }
        }

        private void WireClick(Control control)
        {
            control.Click += delegate
            {
                toggleSwitch.Checked = !toggleSwitch.Checked;
            };
        }

        private void UpdateVisualState()
        {
            if (toggleSwitch.Checked)
            {
                indicatorBar.BackColor = Theme.Info;
                surface.BorderColor = Theme.Info;
            }
            else
            {
                indicatorBar.BackColor = Theme.BorderSoft;
                surface.BorderColor = Theme.BorderSoft;
            }
        }
    }
}
