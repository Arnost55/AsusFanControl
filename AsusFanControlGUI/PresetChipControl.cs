using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class PresetChipControl : UserControl
    {
        private readonly CardPanel surface;
        private readonly Panel accentBar;
        private readonly Label nameLabel;
        private readonly Label speedLabel;
        private readonly Label tagLabel;
        private Preset preset;
        private bool isActive;

        public event EventHandler PresetActivated;

        public event EventHandler RenameRequested;

        public event EventHandler DeleteRequested;

        public PresetChipControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 0, 12, 12);
            MinimumSize = new Size(180, 72);
            Size = new Size(200, 80);

            surface = new CardPanel
            {
                Dock = DockStyle.Fill,
                SurfaceColor = Theme.CardAlt,
                BorderColor = Theme.BorderSoft,
                CornerRadius = 16,
                Padding = new Padding(14, 12, 14, 12)
            };

            accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Theme.Info
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

            nameLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.SectionFont,
                Margin = new Padding(0, 0, 0, 2),
                Text = "Preset"
            };

            speedLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = "0%"
            };

            tagLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0),
                Text = "Custom"
            };

            stack.Controls.Add(nameLabel, 0, 0);
            stack.Controls.Add(speedLabel, 0, 1);
            stack.Controls.Add(tagLabel, 0, 2);

            surface.Controls.Add(stack);
            surface.Controls.Add(accentBar);

            Controls.Add(surface);

            WireClick(surface);
            WireClick(nameLabel);
            WireClick(speedLabel);
            WireClick(tagLabel);
            WireClick(accentBar);
        }

        public Preset BoundPreset
        {
            get { return preset; }
        }

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;
                UpdateVisualState();
            }
        }

        public void BindPreset(Preset value)
        {
            preset = value;
            if (preset == null)
            {
                return;
            }

            nameLabel.Text = preset.Name;
            speedLabel.Text = preset.Speed + "%";
            tagLabel.Text = preset.IsBuiltIn ? "Built-in" : "Custom";
            RefreshContextMenu();
            UpdateVisualState();
        }

        private void RefreshContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Apply", null, delegate
            {
                OnPresetActivated();
            });

            if (preset != null && !preset.IsBuiltIn)
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Rename", null, delegate
                {
                    if (RenameRequested != null)
                    {
                        RenameRequested(this, EventArgs.Empty);
                    }
                });
                menu.Items.Add("Delete", null, delegate
                {
                    if (DeleteRequested != null)
                    {
                        DeleteRequested(this, EventArgs.Empty);
                    }
                });
            }

            ContextMenuStrip = menu;
            surface.ContextMenuStrip = menu;
            nameLabel.ContextMenuStrip = menu;
            speedLabel.ContextMenuStrip = menu;
            tagLabel.ContextMenuStrip = menu;
        }

        private void WireClick(Control control)
        {
            control.Click += delegate
            {
                OnPresetActivated();
            };
        }

        private void OnPresetActivated()
        {
            if (PresetActivated != null)
            {
                PresetActivated(this, EventArgs.Empty);
            }
        }

        private void UpdateVisualState()
        {
            if (preset == null)
            {
                return;
            }

            if (isActive)
            {
                surface.SurfaceColor = Theme.AccentDark;
                surface.BorderColor = Theme.Accent;
                accentBar.BackColor = Theme.Accent;
                nameLabel.ForeColor = Theme.Text;
                speedLabel.ForeColor = Theme.Text;
                tagLabel.ForeColor = Theme.Text;
            }
            else
            {
                surface.SurfaceColor = Theme.CardAlt;
                surface.BorderColor = Theme.BorderSoft;
                accentBar.BackColor = preset.IsBuiltIn ? Theme.Info : Theme.Success;
                nameLabel.ForeColor = Theme.Text;
                speedLabel.ForeColor = Theme.Muted;
                tagLabel.ForeColor = Theme.Muted;
            }
        }
    }
}
