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
        private readonly CheckBox toggleCheckBox;
        private bool suppressCheckedChanged;

        public event EventHandler CheckedChanged;

        public SettingToggleControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 0, 10);
            Size = new Size(320, 84);
            MinimumSize = new Size(280, 84);

            surface = new CardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceColor = Theme.CardAlt,
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
                MaximumSize = new Size(220, 0),
                Margin = new Padding(0),
                Text = string.Empty
            };

            toggleCheckBox = new CheckBox
            {
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Checked = false,
                ForeColor = Theme.Text,
                Font = Theme.BodyFont,
                Margin = new Padding(0)
            };
            toggleCheckBox.CheckedChanged += ToggleCheckBox_CheckedChanged;

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
            layout.Controls.Add(toggleCheckBox, 1, 0);

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
            get { return toggleCheckBox.Checked; }
            set
            {
                suppressCheckedChanged = true;
                toggleCheckBox.Checked = value;
                suppressCheckedChanged = false;
                UpdateVisualState();
            }
        }

        private void ToggleCheckBox_CheckedChanged(object sender, EventArgs e)
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
                toggleCheckBox.Checked = !toggleCheckBox.Checked;
            };
        }

        private void UpdateVisualState()
        {
            if (toggleCheckBox.Checked)
            {
                indicatorBar.BackColor = Theme.Accent;
                surface.BorderColor = Theme.Accent;
            }
            else
            {
                indicatorBar.BackColor = Theme.BorderSoft;
                surface.BorderColor = Theme.BorderSoft;
            }
        }
    }
}
