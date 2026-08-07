using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class PresetEditorForm : Form
    {
        private readonly TextBox nameTextBox;
        private readonly NumericUpDown speedNumeric;
        private readonly Label helperLabel;

        public PresetEditorForm(string title, string presetName, int presetSpeed, int minSpeed, int maxSpeed, string helperText)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = Theme.BodyFont;
            ClientSize = new Size(420, 240);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9,
                BackColor = Theme.Window,
                Margin = new Padding(0),
                Padding = new Padding(18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.TitleFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = title
            };

            var subtitleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 14),
                Text = helperText
            };

            var nameLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = "Preset name"
            };

            nameTextBox = new TextBox
            {
                Margin = new Padding(0, 0, 0, 14),
                Text = presetName ?? string.Empty,
                Width = 360
            };
            Theme.StyleTextBox(nameTextBox);

            var speedLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = "Fan speed"
            };

            speedNumeric = new NumericUpDown
            {
                Margin = new Padding(0, 0, 0, 8),
                Minimum = minSpeed,
                Maximum = maxSpeed,
                Value = presetSpeed < minSpeed ? minSpeed : presetSpeed > maxSpeed ? maxSpeed : presetSpeed,
                Width = 120
            };
            Theme.StyleNumeric(speedNumeric);

            var rangeLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 16),
                Text = "The speed you save here will be clamped by the current safety setting."
            };

            helperLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 12),
                Text = "Custom presets are stored locally and can be renamed or deleted later."
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 96
            };
            Theme.StyleButton(cancelButton, false);

            var saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Width = 96
            };
            Theme.StyleButton(saveButton, true);

            buttons.Controls.Add(saveButton);
            buttons.Controls.Add(cancelButton);

            root.Controls.Add(titleLabel, 0, 0);
            root.Controls.Add(subtitleLabel, 0, 1);
            root.Controls.Add(nameLabel, 0, 2);
            root.Controls.Add(nameTextBox, 0, 3);
            root.Controls.Add(speedLabel, 0, 4);
            root.Controls.Add(speedNumeric, 0, 5);
            root.Controls.Add(rangeLabel, 0, 6);
            root.Controls.Add(helperLabel, 0, 7);
            root.Controls.Add(buttons, 0, 8);

            Controls.Add(root);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += delegate
            {
                nameTextBox.Focus();
                nameTextBox.SelectAll();
            };
        }

        public string PresetName
        {
            get { return nameTextBox.Text.Trim(); }
        }

        public int PresetSpeed
        {
            get { return (int)speedNumeric.Value; }
        }

        public string ValidationMessage
        {
            get { return helperLabel.Text; }
            set { helperLabel.Text = value ?? string.Empty; }
        }
    }
}
