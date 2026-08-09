using AsusFanControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    public partial class Form1 : Form
    {
        private const int SafeMinSpeed = 40;
        private const int SafeMaxSpeed = 99;
        private const int RefreshIntervalMs = 2000;
        private const string ReleasesUrl = "https://github.com/Karmel0x/AsusFanControl/releases";

        private readonly FanControlService fanControlService = new FanControlService();
        private readonly PresetStore presetStore = new PresetStore();
        private readonly Timer refreshTimer = new Timer();
        private readonly List<Preset> presets = new List<Preset>();
        private readonly List<PresetChipControl> presetChipControls = new List<PresetChipControl>();
        private readonly List<StatTileControl> fanTileControls = new List<StatTileControl>();

        private TableLayoutPanel rootLayout;
        private CardPanel headerCard;
        private CardPanel errorBanner;
        private CardPanel manualCard;
        private CardPanel presetCard;
        private CardPanel statsCard;
        private CardPanel settingsCard;
        private TableLayoutPanel bodyLayout;
        private TableLayoutPanel leftColumnLayout;
        private TableLayoutPanel rightColumnLayout;

        private Label headerTitleLabel;
        private Label headerSubtitleLabel;
        private Label serviceBadge;
        private Label controlBadge;
        private Label cpuBadge;
        private Label fan1Badge;
        private Label fan2Badge;
        private Label presetBadge;
        private Button refreshButton;
        private Button updatesButton;

        private Label bannerTitleLabel;
        private Label bannerDetailLabel;
        private Button bannerRetryButton;

        private Label manualStateBadge;
        private ToggleSwitchControl controlToggle;
        private FanCurvePreviewControl curvePreview;
        private Label requestedSpeedLabel;
        private Label presetMatchLabel;
        private TrackBar speedTrackBar;
        private Label clampRangeLabel;
        private Button applyButton;
        private Button releaseButton;
        private Label manualHintLabel;

        private Button savePresetButton;
        private Label builtInSectionLabel;
        private Label customSectionLabel;
        private Label presetHintLabel;
        private FlowLayoutPanel builtInPresetFlow;
        private FlowLayoutPanel customPresetFlow;
        private Label customEmptyLabel;

        private StatTileControl serviceTile;
        private StatTileControl activePresetTile;
        private StatTileControl cpuTile;
        private FlowLayoutPanel fanTileFlow;
        private Label fanSectionLabel;

        private SettingToggleControl turnOffOnExitToggle;
        private SettingToggleControl safeClampToggle;
        private SettingToggleControl minimizeToTrayToggle;
        private SettingToggleControl autoRefreshToggle;
        private Button checkUpdatesButton;
        private Label settingsHintLabel;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private bool isExiting;
        private bool isRefreshing;
        private bool suppressUiUpdates;
        private int lastCommittedSpeed;
        private string selectedPresetId = string.Empty;
        private FanStatusSnapshot lastSnapshot;

        public Form1()
        {
            Program.StartupTrace.Log("Form1 ctor start");
            InitializeComponent();
            DoubleBuffered = true;

            BuildDashboardUi();

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += RefreshTimer_Tick;

            Shown += Form1_Shown;
            FormClosing += Form1_FormClosing;
            FormClosed += Form1_FormClosed;
            Program.StartupTrace.Log("Form1 ctor end");
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var rect = ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                base.OnPaintBackground(e);
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            using (var background = new LinearGradientBrush(rect, Theme.Window, Theme.CardAlt, 88f))
            {
                e.Graphics.FillRectangle(background, rect);
            }

            DrawBackgroundGlow(e.Graphics, rect, new Rectangle(rect.Left - 160, rect.Top - 120, 520, 520), Color.FromArgb(60, Theme.Info));
            DrawBackgroundGlow(e.Graphics, rect, new Rectangle(rect.Right - 420, rect.Top + 40, 520, 520), Color.FromArgb(54, Theme.Accent));
            DrawBackgroundGlow(e.Graphics, rect, new Rectangle(rect.Right - 520, rect.Bottom - 320, 600, 600), Color.FromArgb(32, Theme.Success));
        }

        private void DrawBackgroundGlow(Graphics graphics, Rectangle bounds, Rectangle glowBounds, Color color)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(glowBounds);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = color;
                    brush.SurroundColors = new[] { Color.FromArgb(0, color) };
                    graphics.FillPath(brush, path);
                }
            }
        }

        private void BuildDashboardUi()
        {
            Program.StartupTrace.Log("BuildDashboardUi start");
            SuspendLayout();

            Controls.Clear();

            rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Window,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(18)
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            headerCard = BuildHeaderCard();
            errorBanner = BuildErrorBanner();
            bodyLayout = BuildBodyLayout();

            rootLayout.Controls.Add(headerCard, 0, 0);
            rootLayout.Controls.Add(errorBanner, 0, 1);
            rootLayout.Controls.Add(bodyLayout, 0, 2);

            Controls.Add(rootLayout);

            ResumeLayout(performLayout: true);
            Program.StartupTrace.Log("BuildDashboardUi end");
        }

        private CardPanel BuildHeaderCard()
        {
            headerCard = new CardPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                SurfaceColor = Theme.Card,
                BorderColor = Theme.Border,
                CornerRadius = 18,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            headerTitleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.TitleFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = "Asus Fan Control"
            };

            headerSubtitleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 0, 0, 10),
                Text = "Cinematic control center for staged curves, live telemetry, and safe tuning."
            };

            var badgeRow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };

            serviceBadge = CreateBadgeLabel("Service checking", Theme.CardAlt, Theme.Muted);
            controlBadge = CreateBadgeLabel("Control staged", Theme.CardAlt, Theme.Warning);
            cpuBadge = CreateBadgeLabel("CPU 65 C", Theme.CardAlt, Theme.Warning);
            fan1Badge = CreateBadgeLabel("Fan 1 -- RPM", Theme.CardAlt, Theme.Info);
            fan2Badge = CreateBadgeLabel("Fan 2 -- RPM", Theme.CardAlt, Theme.Success);
            presetBadge = CreateBadgeLabel("Preset Custom", Theme.AccentDark, Theme.Text);
            badgeRow.Controls.Add(serviceBadge);
            badgeRow.Controls.Add(controlBadge);
            badgeRow.Controls.Add(cpuBadge);
            badgeRow.Controls.Add(fan1Badge);
            badgeRow.Controls.Add(fan2Badge);
            badgeRow.Controls.Add(presetBadge);

            left.Controls.Add(headerTitleLabel, 0, 0);
            left.Controls.Add(headerSubtitleLabel, 0, 1);
            left.Controls.Add(badgeRow, 0, 2);

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            refreshButton = new Button
            {
                Text = "Refresh",
                MinimumSize = new Size(104, 0)
            };
            Theme.StyleButton(refreshButton, false);
            refreshButton.Click += RefreshButton_Click;

            updatesButton = new Button
            {
                Text = "Check updates",
                MinimumSize = new Size(120, 0)
            };
            Theme.StyleButton(updatesButton, false);
            updatesButton.Click += UpdatesButton_Click;

            actions.Controls.Add(updatesButton);
            actions.Controls.Add(refreshButton);

            layout.Controls.Add(left, 0, 0);
            layout.Controls.Add(actions, 1, 0);
            headerCard.Controls.Add(layout);

            return headerCard;
        }

        private CardPanel BuildErrorBanner()
        {
            errorBanner = new CardPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                SurfaceColor = Color.FromArgb(38, 20, 24),
                BorderColor = Theme.Danger,
                CornerRadius = 16,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 14),
                Visible = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var textStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            textStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            bannerTitleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.SectionFont,
                Margin = new Padding(0, 0, 0, 3),
                Text = "ASUS hardware read failed"
            };

            bannerDetailLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0),
                Text = "Click Retry after confirming that MyASUS and ASUS System Analysis are running."
            };

            textStack.Controls.Add(bannerTitleLabel, 0, 0);
            textStack.Controls.Add(bannerDetailLabel, 0, 1);

            bannerRetryButton = new Button
            {
                Text = "Retry",
                MinimumSize = new Size(96, 0)
            };
            Theme.StyleButton(bannerRetryButton, true);
            bannerRetryButton.Click += RefreshButton_Click;

            layout.Controls.Add(textStack, 0, 0);
            layout.Controls.Add(bannerRetryButton, 1, 0);
            errorBanner.Controls.Add(layout);

            return errorBanner;
        }

        private TableLayoutPanel BuildBodyLayout()
        {
            bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Window,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));

            leftColumnLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.Window,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(0)
            };
            leftColumnLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftColumnLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftColumnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            rightColumnLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.Window,
                Margin = new Padding(12, 0, 0, 0),
                Padding = new Padding(0)
            };
            rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            manualCard = BuildManualCard();
            presetCard = BuildPresetCard();
            statsCard = BuildStatsCard();
            settingsCard = BuildSettingsCard();

            leftColumnLayout.Controls.Add(manualCard, 0, 0);
            leftColumnLayout.Controls.Add(presetCard, 0, 1);

            rightColumnLayout.Controls.Add(statsCard, 0, 0);
            rightColumnLayout.Controls.Add(settingsCard, 0, 1);

            bodyLayout.Controls.Add(leftColumnLayout, 0, 0);
            bodyLayout.Controls.Add(rightColumnLayout, 1, 0);

            return bodyLayout;
        }

        private CardPanel BuildManualCard()
        {
            manualCard = new CardPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                SurfaceColor = Theme.Card,
                BorderColor = Theme.Border,
                CornerRadius = 18,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            manualStateBadge = CreateBadgeLabel("STAGED", Theme.Warning, Theme.Window);

            layout.Controls.Add(CreateSectionHeader("Manual fan curve", "Two live curves, a moving temperature cursor, and staged apply/discard control.", manualStateBadge), 0, 0);

            curvePreview = new FanCurvePreviewControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 14)
            };
            curvePreview.TargetSpeedChanged += CurvePreview_TargetSpeedChanged;
            layout.Controls.Add(curvePreview, 0, 1);

            var toggleRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            toggleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toggleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var toggleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 2, 0, 0),
                Text = "Enable manual control"
            };
            controlToggle = new ToggleSwitchControl
            {
                Checked = false,
                OnColor = Theme.Accent,
                OffColor = Theme.BorderSoft,
                Margin = new Padding(0, 0, 0, 0)
            };
            controlToggle.CheckedChanged += ControlToggle_CheckedChanged;
            toggleRow.Controls.Add(toggleLabel, 0, 0);
            toggleRow.Controls.Add(controlToggle, 1, 0);

            var speedRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var speedStack = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            speedStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            speedStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var targetLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 4),
                Text = "Target %"
            };

            var speedValueCard = new CardPanel
            {
                SurfaceColor = Theme.CardAlt,
                BorderColor = Theme.BorderSoft,
                CornerRadius = 12,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0)
            };

            requestedSpeedLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.DisplayFont,
                Margin = new Padding(0, 0, 0, 0),
                Text = "0%"
            };
            speedValueCard.Controls.Add(requestedSpeedLabel);

            presetMatchLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 2, 0, 0),
                Text = "Active preset: Custom",
                TextAlign = ContentAlignment.MiddleRight
            };

            speedStack.Controls.Add(targetLabel, 0, 0);
            speedStack.Controls.Add(speedValueCard, 0, 1);

            speedRow.Controls.Add(speedStack, 0, 0);
            speedRow.Controls.Add(presetMatchLabel, 1, 0);

            speedTrackBar = new TrackBar
            {
                Dock = DockStyle.Fill,
                Maximum = 100,
                Minimum = 0,
                TickFrequency = 10,
                TickStyle = TickStyle.None,
                LargeChange = 5,
                SmallChange = 1,
                Margin = new Padding(0, 10, 0, 6),
                BackColor = Theme.Card
            };
            speedTrackBar.ValueChanged += SpeedTrackBar_ValueChanged;
            speedTrackBar.MouseCaptureChanged += SpeedTrackBar_MouseCaptureChanged;
            speedTrackBar.KeyUp += SpeedTrackBar_KeyUp;

            clampRangeLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Warning,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 12),
                Text = "Safe range: 40-99%"
            };

            var buttonRow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            applyButton = new Button
            {
                Text = "Apply staged changes",
                MinimumSize = new Size(120, 0)
            };
            Theme.StyleButton(applyButton, true);
            applyButton.Click += ApplyButton_Click;

            releaseButton = new Button
            {
                Text = "Discard changes",
                MinimumSize = new Size(128, 0)
            };
            Theme.StyleButton(releaseButton, false);
            releaseButton.Click += DiscardButton_Click;

            buttonRow.Controls.Add(applyButton);
            buttonRow.Controls.Add(releaseButton);

            manualHintLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 10, 0, 0),
                Text = "Staged changes wait here until you apply them or enable live control."
            };

            layout.Controls.Add(toggleRow, 0, 2);
            layout.Controls.Add(speedRow, 0, 3);
            layout.Controls.Add(speedTrackBar, 0, 4);
            layout.Controls.Add(clampRangeLabel, 0, 5);
            layout.Controls.Add(buttonRow, 0, 6);
            layout.Controls.Add(manualHintLabel, 0, 7);

            manualCard.Controls.Add(layout);

            return manualCard;
        }

        private CardPanel BuildPresetCard()
        {
            presetCard = new CardPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                SurfaceColor = Theme.Card,
                BorderColor = Theme.Border,
                CornerRadius = 18,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            savePresetButton = new Button
            {
                Text = "Save current",
                MinimumSize = new Size(120, 0)
            };
            Theme.StyleButton(savePresetButton, true);
            savePresetButton.Click += SavePresetButton_Click;

            layout.Controls.Add(CreateSectionHeader("Performance presets", "Built-in modes and saved custom profiles live here.", savePresetButton), 0, 0);

            presetHintLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 12),
                Text = "Built-in profiles sit above custom profiles you save yourself."
            };
            layout.Controls.Add(presetHintLabel, 0, 1);

            builtInSectionLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 6),
                Text = "BUILT-IN"
            };
            layout.Controls.Add(builtInSectionLabel, 0, 2);

            builtInPresetFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(0)
            };
            layout.Controls.Add(builtInPresetFlow, 0, 3);

            customSectionLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 6),
                Text = "CUSTOM"
            };
            layout.Controls.Add(customSectionLabel, 0, 4);

            customPresetFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.Controls.Add(customPresetFlow, 0, 5);

            customEmptyLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.BodyFont,
                Margin = new Padding(0, 0, 0, 0),
                Text = "No custom profiles saved yet."
            };
            layout.Controls.Add(customEmptyLabel, 0, 6);

            presetCard.Controls.Add(layout);

            return presetCard;
        }

        private CardPanel BuildStatsCard()
        {
            statsCard = new CardPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                SurfaceColor = Theme.Card,
                BorderColor = Theme.Border,
                CornerRadius = 18,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(CreateSectionHeader("Hardware telemetry", "Live CPU temperature and fan RPM from ASUS hardware.", null), 0, 0);

            var staticTiles = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };

            serviceTile = CreateStatTile("Service", "Checking", "Waiting for hardware", Theme.Warning);
            activePresetTile = CreateStatTile("Active preset", "Custom", "Requested speed", Theme.Info);
            cpuTile = CreateStatTile("CPU temperature", "--", "No data yet", Theme.Warning);
            staticTiles.Controls.Add(serviceTile);
            staticTiles.Controls.Add(activePresetTile);
            staticTiles.Controls.Add(cpuTile);

            fanSectionLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 4, 0, 6),
                Text = "FANS"
            };

            fanTileFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            layout.Controls.Add(staticTiles, 0, 1);
            layout.Controls.Add(fanSectionLabel, 0, 2);
            layout.Controls.Add(fanTileFlow, 0, 3);
            layout.Controls.Add(new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Theme.BorderSoft, Margin = new Padding(0, 12, 0, 0) }, 0, 4);

            statsCard.Controls.Add(layout);

            return statsCard;
        }

        private CardPanel BuildSettingsCard()
        {
            settingsCard = new CardPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                SurfaceColor = Theme.Card,
                BorderColor = Theme.Border,
                CornerRadius = 18,
                Padding = new Padding(18),
                Margin = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(CreateSectionHeader("Application settings", "Safety, tray, and refresh behavior.", null), 0, 0);

            turnOffOnExitToggle = new SettingToggleControl
            {
                ToggleTitle = "Turn off control on exit",
                ToggleDescription = "Release manual control when the dashboard closes."
            };
            turnOffOnExitToggle.CheckedChanged += TurnOffOnExitToggle_CheckedChanged;

            safeClampToggle = new SettingToggleControl
            {
                ToggleTitle = "Limit to safe 40-99%",
                ToggleDescription = "Clamp manual speeds to the safe ASUS range."
            };
            safeClampToggle.CheckedChanged += SafeClampToggle_CheckedChanged;

            minimizeToTrayToggle = new SettingToggleControl
            {
                ToggleTitle = "Minimize to tray on close",
                ToggleDescription = "Hide the window instead of exiting when you close it."
            };
            minimizeToTrayToggle.CheckedChanged += MinimizeToTrayToggle_CheckedChanged;

            autoRefreshToggle = new SettingToggleControl
            {
                ToggleTitle = "Auto refresh stats",
                ToggleDescription = "Refresh RPM and temperature every 2 seconds."
            };
            autoRefreshToggle.CheckedChanged += AutoRefreshToggle_CheckedChanged;

            layout.Controls.Add(turnOffOnExitToggle, 0, 1);
            layout.Controls.Add(safeClampToggle, 0, 2);
            layout.Controls.Add(minimizeToTrayToggle, 0, 3);
            layout.Controls.Add(autoRefreshToggle, 0, 4);

            checkUpdatesButton = new Button
            {
                Text = "Check updates",
                MinimumSize = new Size(140, 0),
                Margin = new Padding(0, 8, 0, 0)
            };
            Theme.StyleButton(checkUpdatesButton, false);
            checkUpdatesButton.Click += UpdatesButton_Click;
            layout.Controls.Add(checkUpdatesButton, 0, 5);

            settingsHintLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 10, 0, 0),
                Text = "Settings are saved immediately. Tray mode keeps the control panel available in the background."
            };
            layout.Controls.Add(settingsHintLabel, 0, 6);

            settingsCard.Controls.Add(layout);

            return settingsCard;
        }

        private TableLayoutPanel CreateSectionHeader(string title, string subtitle, Control rightControl)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = rightControl == null ? 1 : 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (rightControl != null)
            {
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.TitleFont,
                Margin = new Padding(0, 0, 0, 2),
                Text = title
            };

            var subtitleLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 0, 0),
                Text = subtitle ?? string.Empty
            };

            panel.Controls.Add(titleLabel, 0, 0);
            panel.Controls.Add(subtitleLabel, 0, 1);

            if (rightControl != null)
            {
                rightControl.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                rightControl.Margin = new Padding(8, 0, 0, 0);
                panel.SetColumnSpan(titleLabel, 1);
                panel.SetColumnSpan(subtitleLabel, 1);
                panel.Controls.Add(rightControl, 1, 0);
                panel.SetRowSpan(rightControl, 2);
            }

            return panel;
        }

        private Label CreateBadgeLabel(string text, Color backColor, Color foreColor)
        {
            return new Label
            {
                AutoSize = true,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = Theme.SmallFont,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 5, 12, 5),
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private StatTileControl CreateStatTile(string title, string value, string detail, Color accentColor)
        {
            var tile = new StatTileControl
            {
                AccentColor = accentColor,
                TileTitle = title,
                ValueText = value,
                DetailText = detail
            };
            return tile;
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            Program.StartupTrace.Log("Form1 shown");
            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            Program.StartupTrace.Log("LoadDashboardAsync start");
            LoadPersistedState();
            RebuildPresetChips();
            RestoreWindowBounds();
            UpdateAllVisuals();

            await RefreshHardwareAsync();

            if (autoRefreshToggle.Checked)
            {
                refreshTimer.Start();
            }
            Program.StartupTrace.Log("LoadDashboardAsync end");
        }

        private void LoadPersistedState()
        {
            suppressUiUpdates = true;

            presets.Clear();
            presets.AddRange(presetStore.LoadPresets());

            selectedPresetId = Properties.Settings.Default.lastSelectedPresetId ?? string.Empty;

            var storedSpeed = Properties.Settings.Default.fanSpeed;
            var normalizedSpeed = NormalizeRequestedSpeed(storedSpeed);
            if (normalizedSpeed != storedSpeed)
            {
                Properties.Settings.Default.fanSpeed = normalizedSpeed;
                Properties.Settings.Default.Save();
            }

            controlToggle.Checked = false;
            safeClampToggle.Checked = Properties.Settings.Default.forbidUnsafeSettings;
            turnOffOnExitToggle.Checked = Properties.Settings.Default.turnOffControlOnExit;
            minimizeToTrayToggle.Checked = Properties.Settings.Default.minimizeToTrayOnClose;
            autoRefreshToggle.Checked = Properties.Settings.Default.autoRefreshStats;
            speedTrackBar.Value = normalizedSpeed;

            suppressUiUpdates = false;

            lastCommittedSpeed = normalizedSpeed;
            SyncSelectedPresetToCurrentSpeed(normalizedSpeed, true);
        }

        private void RestoreWindowBounds()
        {
            var bounds = Properties.Settings.Default.windowBounds;
            if (bounds.Width > 0 && bounds.Height > 0 && IsVisibleOnAnyScreen(bounds))
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
                WindowState = FormWindowState.Normal;
                return;
            }

            StartPosition = FormStartPosition.CenterScreen;
        }

        private bool IsVisibleOnAnyScreen(Rectangle bounds)
        {
            return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds));
        }

        private void UpdateAllVisuals()
        {
            UpdateManualUi();
            UpdatePresetUi();
            UpdateHeaderBadges();
            UpdateRangeLabel();
            UpdateCurvePreview();
            UpdateSettingsState();
        }

        private void UpdateManualUi()
        {
            if (controlToggle == null)
            {
                return;
            }

            var currentSpeed = speedTrackBar.Value;
            requestedSpeedLabel.Text = currentSpeed.ToString() + "%";
            presetMatchLabel.Text = "Active preset: " + ResolveActivePreset(currentSpeed).Name;

            UpdateManualStateBadge();
            UpdateControlBadge();
            UpdateCurvePreview();
        }

        private void UpdatePresetUi()
        {
            var currentSpeed = speedTrackBar.Value;
            var activePreset = ResolveActivePreset(currentSpeed);

            activePresetTile.TileTitle = "Active preset";
            activePresetTile.ValueText = activePreset.Name;
            activePresetTile.DetailText = activePreset.Speed + "% requested";
            activePresetTile.AccentColor = activePreset.Id == "custom"
                ? Theme.Accent
                : (activePreset.IsBuiltIn ? Theme.Info : Theme.Success);
            activePresetTile.ValueColor = Theme.Text;

            foreach (var chip in presetChipControls)
            {
                chip.IsActive = chip.BoundPreset != null && chip.BoundPreset.Id == activePreset.Id;
            }
        }

        private void UpdateHeaderBadges()
        {
            if (lastSnapshot == null)
            {
                SetBadge(serviceBadge, "Service checking", Theme.CardAlt, Theme.Muted);
                SetBadge(cpuBadge, "CPU --", Theme.CardAlt, Theme.Muted);
                SetBadge(fan1Badge, "Fan 1 -- RPM", Theme.CardAlt, Theme.Muted);
                SetBadge(fan2Badge, "Fan 2 -- RPM", Theme.CardAlt, Theme.Muted);
            }
            else
            {
                if (lastSnapshot.IsReady)
                {
                    SetBadge(serviceBadge, "Service ready", Theme.CardAlt, Theme.Success);
                }
                else
                {
                    SetBadge(serviceBadge, "Service issue", Theme.CardAlt, Theme.Danger);
                }

                if (lastSnapshot.CpuTemperature.HasValue)
                {
                    var cpuText = "CPU " + lastSnapshot.CpuTemperature.Value + " C";
                    SetBadge(cpuBadge, cpuText, Theme.CardAlt, Theme.Warning);
                }
                else
                {
                    SetBadge(cpuBadge, "CPU --", Theme.CardAlt, Theme.Muted);
                }

                if (lastSnapshot.FanSpeeds != null && lastSnapshot.FanSpeeds.Count > 0)
                {
                    var fan1Text = "Fan 1 " + lastSnapshot.FanSpeeds[0] + " RPM";
                    SetBadge(fan1Badge, fan1Text, Theme.CardAlt, Theme.Info);

                    if (lastSnapshot.FanSpeeds.Count > 1)
                    {
                        var fan2Text = "Fan 2 " + lastSnapshot.FanSpeeds[1] + " RPM";
                        SetBadge(fan2Badge, fan2Text, Theme.CardAlt, Theme.Success);
                    }
                    else
                    {
                        SetBadge(fan2Badge, "Fan 2 -- RPM", Theme.CardAlt, Theme.Muted);
                    }
                }
                else
                {
                    SetBadge(fan1Badge, "Fan 1 -- RPM", Theme.CardAlt, Theme.Muted);
                    SetBadge(fan2Badge, "Fan 2 -- RPM", Theme.CardAlt, Theme.Muted);
                }
            }

            UpdateControlBadge();

            var currentSpeed = speedTrackBar != null ? speedTrackBar.Value : 0;
            var activePreset = ResolveActivePreset(currentSpeed);
            var presetBackColor = activePreset != null && !activePreset.IsBuiltIn ? Theme.AccentDark : Theme.InfoDark;
            SetBadge(presetBadge, "Preset " + (activePreset != null ? activePreset.Name : "Custom"), presetBackColor, Theme.Text);
        }

        private void UpdateRangeLabel()
        {
            if (safeClampToggle.Checked)
            {
                clampRangeLabel.Text = "Safe range " + SafeMinSpeed + "-" + SafeMaxSpeed + "%";
                clampRangeLabel.ForeColor = Theme.Warning;
            }
            else
            {
                clampRangeLabel.Text = "Full range 0-100%";
                clampRangeLabel.ForeColor = Theme.Info;
            }
        }

        private void UpdateCurvePreview()
        {
            if (curvePreview == null)
            {
                return;
            }

            var currentSpeed = speedTrackBar != null ? speedTrackBar.Value : 0;
            var activePreset = ResolveActivePreset(currentSpeed);

            curvePreview.TargetSpeed = currentSpeed;
            curvePreview.ControlEnabled = controlToggle != null && controlToggle.Checked;
            curvePreview.SafeClampEnabled = safeClampToggle != null && safeClampToggle.Checked;
            curvePreview.SafeMinSpeed = SafeMinSpeed;
            curvePreview.SafeMaxSpeed = SafeMaxSpeed;
            curvePreview.ServiceReady = lastSnapshot == null || lastSnapshot.IsReady;
            curvePreview.CpuTemperature = lastSnapshot != null && lastSnapshot.CpuTemperature.HasValue ? lastSnapshot.CpuTemperature.Value : 65;
            curvePreview.ActivePresetName = activePreset != null ? activePreset.Name : "Custom";
            curvePreview.FanSpeeds = lastSnapshot != null && lastSnapshot.FanSpeeds != null
                ? lastSnapshot.FanSpeeds.ToArray()
                : null;
        }

        private void UpdateSettingsState()
        {
            // Keep the control badges in sync with the current toggles.
            UpdateControlBadge();
        }

        private void UpdateManualStateBadge()
        {
            if (controlToggle.Checked)
            {
                SetBadge(manualStateBadge, "LIVE", Theme.Success, Theme.Window);
                manualHintLabel.Text = "Control is live, so the current speed is written to the hardware.";
            }
            else
            {
                SetBadge(manualStateBadge, "STAGED", Theme.Warning, Theme.Window);
                manualHintLabel.Text = "Control is staged until you apply it or enable live control.";
            }
        }

        private void UpdateControlBadge()
        {
            if (controlToggle.Checked)
            {
                SetBadge(controlBadge, "Control live", Theme.CardAlt, Theme.Success);
                applyButton.Text = "Apply staged changes";
            }
            else
            {
                SetBadge(controlBadge, "Control staged", Theme.CardAlt, Theme.Warning);
                applyButton.Text = "Enable and apply";
            }

            releaseButton.Enabled = speedTrackBar != null && speedTrackBar.Value != lastCommittedSpeed;
        }

        private void SetBadge(Label badge, string text, Color backColor, Color foreColor)
        {
            badge.Text = text;
            badge.BackColor = backColor;
            badge.ForeColor = foreColor;
        }

        private int NormalizeRequestedSpeed(int speed)
        {
            if (safeClampToggle != null && safeClampToggle.Checked)
            {
                if (speed < SafeMinSpeed)
                {
                    speed = SafeMinSpeed;
                }
                else if (speed > SafeMaxSpeed)
                {
                    speed = SafeMaxSpeed;
                }
            }

            if (speed < 0)
            {
                speed = 0;
            }
            else if (speed > 100)
            {
                speed = 100;
            }

            return speed;
        }

        private int GetRequestedSpeed()
        {
            if (speedTrackBar == null)
            {
                return 0;
            }

            return speedTrackBar.Value;
        }

        private Preset ResolveActivePreset(int speed)
        {
            if (!string.IsNullOrWhiteSpace(selectedPresetId))
            {
                var selected = presets.FirstOrDefault(p => p.Id == selectedPresetId);
                if (selected != null && selected.Speed == speed)
                {
                    return selected;
                }
            }

            var preset = presets.FirstOrDefault(p => p.Speed == speed);
            if (preset != null)
            {
                return preset;
            }

            return new Preset
            {
                Id = "custom",
                Name = "Custom",
                Speed = speed,
                IsBuiltIn = true
            };
        }

        private void SyncSelectedPresetToCurrentSpeed(int speed, bool preserveExplicitSelection)
        {
            Preset resolved = null;

            if (preserveExplicitSelection && !string.IsNullOrWhiteSpace(selectedPresetId))
            {
                var explicitPreset = presets.FirstOrDefault(p => p.Id == selectedPresetId);
                if (explicitPreset != null && explicitPreset.Speed == speed)
                {
                    resolved = explicitPreset;
                }
            }

            if (resolved == null)
            {
                resolved = presets.FirstOrDefault(p => p.Speed == speed);
            }

            selectedPresetId = resolved == null ? string.Empty : resolved.Id;
            Properties.Settings.Default.lastSelectedPresetId = selectedPresetId;
            Properties.Settings.Default.Save();
        }

        private async Task RefreshHardwareAsync()
        {
            if (isRefreshing)
            {
                return;
            }

            isRefreshing = true;
            try
            {
                var snapshot = await fanControlService.ReadSnapshotAsync();
                lastSnapshot = snapshot;
                UpdateHardwareVisuals(snapshot);
                errorBanner.Visible = !snapshot.IsReady;
                if (snapshot.IsReady)
                {
                    bannerDetailLabel.Text = "Hardware reads are healthy.";
                }
                else
                {
                    bannerTitleLabel.Text = "ASUS hardware read failed";
                    bannerDetailLabel.Text = string.IsNullOrWhiteSpace(snapshot.ErrorMessage)
                        ? "Check that MyASUS and ASUS System Analysis are running, then retry."
                        : snapshot.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                lastSnapshot = new FanStatusSnapshot
                {
                    IsReady = false,
                    StatusText = "ASUS interface issue",
                    ErrorMessage = ex.Message,
                    FanSpeeds = new List<int>()
                };
                UpdateHardwareVisuals(lastSnapshot);
                errorBanner.Visible = true;
                bannerTitleLabel.Text = "ASUS hardware read failed";
                bannerDetailLabel.Text = ex.Message;
            }
            finally
            {
                UpdateHeaderBadges();
                isRefreshing = false;
            }
        }

        private void UpdateHardwareVisuals(FanStatusSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            serviceTile.ValueText = snapshot.IsReady ? "Ready" : "Issue";
            serviceTile.DetailText = snapshot.IsReady
                ? "ASUS System Analysis"
                : snapshot.ErrorMessage ?? "Check the ASUS service";
            serviceTile.AccentColor = snapshot.IsReady ? Theme.Success : Theme.Danger;
            serviceTile.ValueColor = snapshot.IsReady ? Theme.Text : Theme.Text;

            if (snapshot.CpuTemperature.HasValue)
            {
                cpuTile.ValueText = snapshot.CpuTemperature.Value + " C";
                cpuTile.DetailText = "CPU temperature";
                cpuTile.AccentColor = Theme.Warning;
            }
            else
            {
                cpuTile.ValueText = "--";
                cpuTile.DetailText = snapshot.IsReady ? "No temperature data" : "Temperature unavailable";
                cpuTile.AccentColor = Theme.Warning;
            }

            if (snapshot.FanSpeeds != null && snapshot.FanSpeeds.Count > 0)
            {
                EnsureFanTiles(snapshot.FanSpeeds.Count);
                for (var i = 0; i < fanTileControls.Count; i++)
                {
                    if (i < snapshot.FanSpeeds.Count)
                    {
                        fanTileControls[i].TileTitle = "Fan " + (i + 1);
                        fanTileControls[i].ValueText = snapshot.FanSpeeds[i] + " RPM";
                        fanTileControls[i].DetailText = "Live fan speed";
                        fanTileControls[i].AccentColor = Theme.Accent;
                        fanTileControls[i].ValueColor = Theme.Text;
                    }
                    else
                    {
                        fanTileControls[i].ValueText = "--";
                        fanTileControls[i].DetailText = "No data";
                    }
                }
            }
            else
            {
                EnsureFanTiles(fanTileControls.Count == 0 ? 1 : fanTileControls.Count);
                foreach (var tile in fanTileControls)
                {
                    tile.ValueText = "--";
                    tile.DetailText = snapshot.IsReady ? "No fan data" : "Fan data unavailable";
                    tile.AccentColor = Theme.Accent;
                    tile.ValueColor = Theme.Text;
                }
            }

            UpdatePresetUi();
            UpdateHeaderBadges();
            UpdateCurvePreview();
        }

        private void EnsureFanTiles(int count)
        {
            if (count < 1)
            {
                count = 1;
            }

            if (fanTileControls.Count == count)
            {
                return;
            }

            fanTileFlow.SuspendLayout();
            fanTileFlow.Controls.Clear();

            foreach (var tile in fanTileControls)
            {
                tile.Dispose();
            }
            fanTileControls.Clear();

            for (var i = 0; i < count; i++)
            {
                var tile = CreateStatTile("Fan " + (i + 1), "--", "Waiting for data", Theme.Accent);
                fanTileControls.Add(tile);
                fanTileFlow.Controls.Add(tile);
            }

            fanTileFlow.ResumeLayout();
        }

        private void RebuildPresetChips()
        {
            builtInPresetFlow.SuspendLayout();
            customPresetFlow.SuspendLayout();

            foreach (var chip in presetChipControls)
            {
                chip.PresetActivated -= PresetChipControl_PresetActivated;
                chip.RenameRequested -= PresetChipControl_RenameRequested;
                chip.DeleteRequested -= PresetChipControl_DeleteRequested;
                chip.Dispose();
            }
            presetChipControls.Clear();

            builtInPresetFlow.Controls.Clear();
            customPresetFlow.Controls.Clear();

            foreach (var preset in presets.Where(p => p.IsBuiltIn))
            {
                builtInPresetFlow.Controls.Add(CreatePresetChip(preset));
            }

            foreach (var preset in presets.Where(p => !p.IsBuiltIn))
            {
                customPresetFlow.Controls.Add(CreatePresetChip(preset));
            }

            customEmptyLabel.Visible = customPresetFlow.Controls.Count == 0;
            customSectionLabel.Visible = true;

            builtInPresetFlow.ResumeLayout();
            customPresetFlow.ResumeLayout();

            UpdatePresetUi();
        }

        private PresetChipControl CreatePresetChip(Preset preset)
        {
            var chip = new PresetChipControl();
            chip.BindPreset(preset);
            chip.PresetActivated += PresetChipControl_PresetActivated;
            chip.RenameRequested += PresetChipControl_RenameRequested;
            chip.DeleteRequested += PresetChipControl_DeleteRequested;
            presetChipControls.Add(chip);
            return chip;
        }

        private async void PresetChipControl_PresetActivated(object sender, EventArgs e)
        {
            var chip = sender as PresetChipControl;
            if (chip == null || chip.BoundPreset == null)
            {
                return;
            }

            await SetRequestedSpeedAsync(chip.BoundPreset.Speed, true, true);
        }

        private async void PresetChipControl_RenameRequested(object sender, EventArgs e)
        {
            var chip = sender as PresetChipControl;
            if (chip == null || chip.BoundPreset == null || chip.BoundPreset.IsBuiltIn)
            {
                return;
            }

            await RenamePresetAsync(chip.BoundPreset);
        }

        private async void PresetChipControl_DeleteRequested(object sender, EventArgs e)
        {
            var chip = sender as PresetChipControl;
            if (chip == null || chip.BoundPreset == null || chip.BoundPreset.IsBuiltIn)
            {
                return;
            }

            await DeletePresetAsync(chip.BoundPreset);
        }

        private async void ControlToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            UpdateManualUi();
            UpdateHeaderBadges();

            if (controlToggle.Checked)
            {
                await ApplyCurrentSpeedToHardwareAsync(GetRequestedSpeed(), true);
            }
            else
            {
                await ReleaseControlAsync();
            }
        }

        private void SpeedTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            UpdateManualUi();
            UpdatePresetUi();
            UpdateRangeLabel();
        }

        private void CurvePreview_TargetSpeedChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates || speedTrackBar == null || curvePreview == null)
            {
                return;
            }

            var targetSpeed = curvePreview.TargetSpeed;
            if (speedTrackBar.Value != targetSpeed)
            {
                speedTrackBar.Value = targetSpeed;
            }
        }

        private async void SpeedTrackBar_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            if (!speedTrackBar.Capture)
            {
                await CommitRequestedSpeedAsync(false, controlToggle.Checked);
            }
        }

        private async void SpeedTrackBar_KeyUp(object sender, KeyEventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown)
            {
                await CommitRequestedSpeedAsync(false, controlToggle.Checked);
            }
        }

        private async void ApplyButton_Click(object sender, EventArgs e)
        {
            if (controlToggle.Checked)
            {
                await ApplyCurrentSpeedToHardwareAsync(GetRequestedSpeed(), true);
                return;
            }

            controlToggle.Checked = true;
        }

        private async void DiscardButton_Click(object sender, EventArgs e)
        {
            if (speedTrackBar == null)
            {
                return;
            }

            var rollbackSpeed = NormalizeRequestedSpeed(lastCommittedSpeed);
            if (speedTrackBar.Value != rollbackSpeed)
            {
                suppressUiUpdates = true;
                speedTrackBar.Value = rollbackSpeed;
                suppressUiUpdates = false;
            }

            Properties.Settings.Default.fanSpeed = rollbackSpeed;
            Properties.Settings.Default.Save();

            UpdateAllVisuals();

            if (controlToggle.Checked)
            {
                await ApplyCurrentSpeedToHardwareAsync(rollbackSpeed, true);
            }
        }

        private async void SavePresetButton_Click(object sender, EventArgs e)
        {
            await SaveCurrentAsPresetAsync();
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            await RefreshHardwareAsync();
        }

        private void UpdatesButton_Click(object sender, EventArgs e)
        {
            OpenReleasesUrl();
        }

        private void TurnOffOnExitToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            Properties.Settings.Default.turnOffControlOnExit = turnOffOnExitToggle.Checked;
            Properties.Settings.Default.Save();
        }

        private void SafeClampToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            Properties.Settings.Default.forbidUnsafeSettings = safeClampToggle.Checked;
            Properties.Settings.Default.Save();

            var normalized = NormalizeRequestedSpeed(speedTrackBar.Value);
            if (normalized != speedTrackBar.Value)
            {
                suppressUiUpdates = true;
                speedTrackBar.Value = normalized;
                suppressUiUpdates = false;
            }

            UpdateRangeLabel();
            UpdateManualUi();
            UpdatePresetUi();

            if (controlToggle.Checked)
            {
                _ = ApplyCurrentSpeedToHardwareAsync(normalized, false);
            }
            else
            {
                _ = CommitRequestedSpeedAsync(false, false);
            }
        }

        private void MinimizeToTrayToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            Properties.Settings.Default.minimizeToTrayOnClose = minimizeToTrayToggle.Checked;
            Properties.Settings.Default.Save();
        }

        private void AutoRefreshToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressUiUpdates)
            {
                return;
            }

            Properties.Settings.Default.autoRefreshStats = autoRefreshToggle.Checked;
            Properties.Settings.Default.Save();

            if (autoRefreshToggle.Checked)
            {
                refreshTimer.Start();
                _ = RefreshHardwareAsync();
            }
            else
            {
                refreshTimer.Stop();
            }
        }

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (isRefreshing)
            {
                return;
            }

            await RefreshHardwareAsync();
        }

        private async Task CommitRequestedSpeedAsync(bool preserveExplicitSelection, bool applyToHardware)
        {
            var requestedSpeed = NormalizeRequestedSpeed(speedTrackBar.Value);
            if (requestedSpeed != speedTrackBar.Value)
            {
                suppressUiUpdates = true;
                speedTrackBar.Value = requestedSpeed;
                suppressUiUpdates = false;
            }

            Properties.Settings.Default.fanSpeed = requestedSpeed;
            Properties.Settings.Default.Save();

            SyncSelectedPresetToCurrentSpeed(requestedSpeed, preserveExplicitSelection);
            UpdateAllVisuals();

            if (applyToHardware && controlToggle.Checked)
            {
                await ApplyCurrentSpeedToHardwareAsync(requestedSpeed, true);
            }
        }

        private async Task SetRequestedSpeedAsync(int requestedSpeed, bool preserveExplicitSelection, bool applyToHardware)
        {
            requestedSpeed = NormalizeRequestedSpeed(requestedSpeed);

            suppressUiUpdates = true;
            speedTrackBar.Value = requestedSpeed;
            suppressUiUpdates = false;

            Properties.Settings.Default.fanSpeed = requestedSpeed;
            Properties.Settings.Default.Save();

            SyncSelectedPresetToCurrentSpeed(requestedSpeed, preserveExplicitSelection);
            UpdateAllVisuals();

            if (applyToHardware && controlToggle.Checked)
            {
                await ApplyCurrentSpeedToHardwareAsync(requestedSpeed, true);
            }
        }

        private async Task ApplyCurrentSpeedToHardwareAsync(int requestedSpeed, bool refreshAfterApply)
        {
            requestedSpeed = NormalizeRequestedSpeed(requestedSpeed);

            try
            {
                await fanControlService.ApplySpeedAsync(requestedSpeed);
            }
            catch (Exception ex)
            {
                ShowHardwareError("Unable to apply the selected speed.", ex.Message);
                return;
            }

            lastCommittedSpeed = requestedSpeed;

            if (refreshAfterApply)
            {
                await RefreshHardwareAsync();
            }
        }

        private async Task ReleaseControlAsync()
        {
            try
            {
                fanControlService.DisableControl();
            }
            catch (Exception ex)
            {
                ShowHardwareError("Unable to release ASUS control.", ex.Message);
                return;
            }

            await RefreshHardwareAsync();
        }

        private void ShowHardwareError(string title, string detail)
        {
            bannerTitleLabel.Text = title;
            bannerDetailLabel.Text = detail;
            errorBanner.Visible = true;

            serviceTile.ValueText = "Issue";
            serviceTile.DetailText = detail;
            serviceTile.AccentColor = Theme.Danger;

            UpdateHeaderBadges();
            UpdateCurvePreview();
        }

        private async Task SaveCurrentAsPresetAsync()
        {
            var requestedSpeed = NormalizeRequestedSpeed(speedTrackBar.Value);
            var defaultName = GetDefaultCustomPresetName();
            var helperText = "Capture the current speed as a reusable preset.";

            using (var editor = new PresetEditorForm("Save custom preset", defaultName, requestedSpeed, GetPresetEditorMinimumSpeed(), GetPresetEditorMaximumSpeed(), helperText))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var presetName = string.IsNullOrWhiteSpace(editor.PresetName) ? defaultName : editor.PresetName;
                var existingPreset = presets.FirstOrDefault(p => !p.IsBuiltIn && string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase));
                var uniqueName = MakeUniquePresetName(presetName, existingPreset);
                var speed = NormalizeRequestedSpeed(editor.PresetSpeed);

                if (existingPreset != null)
                {
                    existingPreset.Name = uniqueName;
                    existingPreset.Speed = speed;
                    selectedPresetId = existingPreset.Id;
                }
                else
                {
                    var newPreset = new Preset
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = uniqueName,
                        Speed = speed,
                        IsBuiltIn = false
                    };
                    presets.Add(newPreset);
                    selectedPresetId = newPreset.Id;
                }

                Properties.Settings.Default.lastSelectedPresetId = selectedPresetId;
                Properties.Settings.Default.fanSpeed = speed;
                Properties.Settings.Default.Save();

                RebuildPresetChips();
                UpdateAllVisuals();
                await SetRequestedSpeedAsync(speed, true, controlToggle.Checked);
            }
        }

        private async Task RenamePresetAsync(Preset preset)
        {
            if (preset == null || preset.IsBuiltIn)
            {
                return;
            }

            using (var editor = new PresetEditorForm("Rename preset", preset.Name, preset.Speed, GetPresetEditorMinimumSpeed(), GetPresetEditorMaximumSpeed(), "Update the preset name or adjust the saved speed."))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var requestedName = string.IsNullOrWhiteSpace(editor.PresetName) ? preset.Name : editor.PresetName;
                var uniqueName = MakeUniquePresetName(requestedName, preset);
                var speed = NormalizeRequestedSpeed(editor.PresetSpeed);

                preset.Name = uniqueName;
                preset.Speed = speed;
                selectedPresetId = preset.Id;

                Properties.Settings.Default.lastSelectedPresetId = selectedPresetId;
                Properties.Settings.Default.fanSpeed = speed;
                Properties.Settings.Default.Save();

                RebuildPresetChips();
                UpdateAllVisuals();
                await SetRequestedSpeedAsync(speed, true, controlToggle.Checked);
            }
        }

        private async Task DeletePresetAsync(Preset preset)
        {
            if (preset == null || preset.IsBuiltIn)
            {
                return;
            }

            var confirm = MessageBox.Show(this, "Delete preset \"" + preset.Name + "\"?", "Delete preset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            presets.Remove(preset);
            if (selectedPresetId == preset.Id)
            {
                selectedPresetId = string.Empty;
                Properties.Settings.Default.lastSelectedPresetId = string.Empty;
                Properties.Settings.Default.Save();
            }

            presetStore.SavePresets(presets);
            RebuildPresetChips();
            UpdateAllVisuals();
            await RefreshHardwareAsync();
        }

        private string GetDefaultCustomPresetName()
        {
            var index = 1;
            string candidate;
            do
            {
                candidate = "Custom preset " + index;
                index++;
            }
            while (presets.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)));

            return candidate;
        }

        private int GetPresetEditorMinimumSpeed()
        {
            return safeClampToggle.Checked ? SafeMinSpeed : 0;
        }

        private int GetPresetEditorMaximumSpeed()
        {
            return safeClampToggle.Checked ? SafeMaxSpeed : 100;
        }

        private string MakeUniquePresetName(string desiredName, Preset ignorePreset)
        {
            var baseName = string.IsNullOrWhiteSpace(desiredName) ? "Custom preset" : desiredName.Trim();
            var candidate = baseName;
            var suffix = 2;

            while (presets.Any(p => p != ignorePreset && string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = baseName + " (" + suffix + ")";
                suffix++;
            }

            return candidate;
        }

        private void OpenReleasesUrl()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ReleasesUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to open updates page", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureTrayIcon()
        {
            if (trayIcon != null)
            {
                return;
            }

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show dashboard", null, delegate
            {
                RestoreFromTray();
            });
            trayMenu.Items.Add("Refresh now", null, RefreshButton_Click);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, delegate
            {
                ExitApplication();
            });

            trayIcon = new NotifyIcon
            {
                Icon = Icon,
                Text = "Asus Fan Control",
                ContextMenuStrip = trayMenu,
                Visible = false
            };
            trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        }

        private void RestoreFromTray()
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }

            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        private void ExitApplication()
        {
            isExiting = true;
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isExiting && e.CloseReason == CloseReason.UserClosing && minimizeToTrayToggle != null && minimizeToTrayToggle.Checked)
            {
                EnsureTrayIcon();
                trayIcon.Visible = true;
                Hide();
                e.Cancel = true;
                return;
            }

            PersistWindowBounds();

            if (turnOffOnExitToggle != null && turnOffOnExitToggle.Checked)
            {
                fanControlService.DisableControl();
            }
        }

        private void PersistWindowBounds()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                return;
            }

            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            Properties.Settings.Default.windowBounds = bounds;
            Properties.Settings.Default.Save();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }

            fanControlService.Dispose();
        }
    }
}

