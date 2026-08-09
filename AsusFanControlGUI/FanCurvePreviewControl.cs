using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal sealed class FanCurvePreviewControl : UserControl
    {
        private const float ChartMin = 0f;
        private const float ChartMax = 100f;

        private int cpuTemperature = 65;
        private int targetSpeed = 81;
        private int safeMinSpeed = 40;
        private int safeMaxSpeed = 99;
        private bool controlEnabled;
        private bool serviceReady = true;
        private bool safeClampEnabled = true;
        private string activePresetName = "Custom";
        private int[] fanSpeeds = new int[0];
        private Rectangle chartRect = Rectangle.Empty;
        private bool isDraggingPoint;
        private bool draggingPrimaryPoint;
        private int draggingPointIndex = -1;
        private bool hoveredPrimaryPoint;
        private int hoveredPointIndex = -1;
        private bool hoveredSecondaryPoint;

        public event EventHandler TargetSpeedChanged;

        public FanCurvePreviewControl()
        {
            DoubleBuffered = true;
            BackColor = Theme.Card;
            Cursor = Cursors.Default;
            Margin = new Padding(0, 0, 0, 14);
            MinimumSize = new Size(540, 300);
            Size = new Size(720, 360);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        public int CpuTemperature
        {
            get { return cpuTemperature; }
            set
            {
                var clamped = Clamp(value, 0, 100);
                if (cpuTemperature == clamped)
                {
                    return;
                }

                cpuTemperature = clamped;
                Invalidate();
            }
        }

        public int TargetSpeed
        {
            get { return targetSpeed; }
            set
            {
                var clamped = Clamp(value, 0, 100);
                if (targetSpeed == clamped)
                {
                    return;
                }

                targetSpeed = clamped;
                if (TargetSpeedChanged != null)
                {
                    TargetSpeedChanged(this, EventArgs.Empty);
                }
                Invalidate();
            }
        }

        public int SafeMinSpeed
        {
            get { return safeMinSpeed; }
            set
            {
                var clamped = Clamp(value, 0, 100);
                if (safeMinSpeed == clamped)
                {
                    return;
                }

                safeMinSpeed = clamped;
                Invalidate();
            }
        }

        public int SafeMaxSpeed
        {
            get { return safeMaxSpeed; }
            set
            {
                var clamped = Clamp(value, 0, 100);
                if (safeMaxSpeed == clamped)
                {
                    return;
                }

                safeMaxSpeed = clamped;
                Invalidate();
            }
        }

        public bool ControlEnabled
        {
            get { return controlEnabled; }
            set
            {
                if (controlEnabled == value)
                {
                    return;
                }

                controlEnabled = value;
                Invalidate();
            }
        }

        public bool ServiceReady
        {
            get { return serviceReady; }
            set
            {
                if (serviceReady == value)
                {
                    return;
                }

                serviceReady = value;
                Invalidate();
            }
        }

        public bool SafeClampEnabled
        {
            get { return safeClampEnabled; }
            set
            {
                if (safeClampEnabled == value)
                {
                    return;
                }

                safeClampEnabled = value;
                Invalidate();
            }
        }

        public string ActivePresetName
        {
            get { return activePresetName; }
            set
            {
                activePresetName = string.IsNullOrWhiteSpace(value) ? "Custom" : value.Trim();
                Invalidate();
            }
        }

        public int[] FanSpeeds
        {
            get { return fanSpeeds == null ? new int[0] : fanSpeeds.ToArray(); }
            set
            {
                fanSpeeds = value == null ? new int[0] : value.ToArray();
                Invalidate();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (TryHitTestPoint(e.Location, out hoveredPrimaryPoint, out hoveredSecondaryPoint, out hoveredPointIndex))
            {
                isDraggingPoint = true;
                draggingPrimaryPoint = hoveredPrimaryPoint;
                draggingPointIndex = hoveredPointIndex;
                Capture = true;
                UpdateDragTarget(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (isDraggingPoint)
            {
                UpdateDragTarget(e.Location);
                return;
            }

            UpdateHoverState(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!isDraggingPoint)
            {
                return;
            }

            isDraggingPoint = false;
            draggingPrimaryPoint = false;
            draggingPointIndex = -1;
            Capture = false;
            UpdateHoverState(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (isDraggingPoint)
            {
                return;
            }

            hoveredPrimaryPoint = false;
            hoveredSecondaryPoint = false;
            hoveredPointIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var outer = new Rectangle(0, 0, Width - 1, Height - 1);
            if (outer.Width <= 0 || outer.Height <= 0)
            {
                return;
            }

            using (var background = new LinearGradientBrush(outer, Theme.Blend(Theme.Card, Theme.CardAlt, 0.2f), Theme.Blend(Theme.Window, Theme.CardAlt, 0.15f), 115f))
            using (var surfacePath = Theme.CreateRoundedPath(outer, 18))
            {
                e.Graphics.FillPath(background, surfacePath);
            }

            using (var border = new Pen(Theme.Border))
            using (var borderPath = Theme.CreateRoundedPath(outer, 18))
            {
                e.Graphics.DrawPath(border, borderPath);
            }

            DrawGlow(e.Graphics, outer);

            var headerRect = new Rectangle(18, 14, Math.Max(0, Width - 36), 28);
            DrawHeader(e.Graphics, headerRect);

            chartRect = new Rectangle(56, 58, Math.Max(0, Width - 92), Math.Max(0, Height - 128));
            if (chartRect.Width <= 0 || chartRect.Height <= 0)
            {
                return;
            }

            DrawChartFrame(e.Graphics, chartRect);
            DrawGrid(e.Graphics, chartRect);

            var primaryPoints = BuildCurvePoints(true, chartRect);
            var secondaryPoints = BuildCurvePoints(false, chartRect);

            DrawCurveFill(e.Graphics, chartRect, primaryPoints, Theme.Success, 68);
            DrawCurve(e.Graphics, primaryPoints, Theme.Success, 3.4f, 235);
            DrawCurve(e.Graphics, secondaryPoints, Theme.Info, 2.8f, 225);

            DrawPointMarkers(e.Graphics, primaryPoints, Theme.Success);
            DrawPointMarkers(e.Graphics, secondaryPoints, Theme.Info);

            DrawCursor(e.Graphics, chartRect);
            DrawAxes(e.Graphics, chartRect);
            DrawFooter(e.Graphics, chartRect);
        }

        private Rectangle GetChartRect()
        {
            return new Rectangle(56, 58, Math.Max(0, Width - 92), Math.Max(0, Height - 128));
        }

        private void UpdateHoverState(Point location)
        {
            int pointIndex;
            bool primary;
            bool secondary;
            if (TryHitTestPoint(location, out primary, out secondary, out pointIndex))
            {
                hoveredPrimaryPoint = primary;
                hoveredSecondaryPoint = secondary;
                hoveredPointIndex = pointIndex;
                Cursor = Cursors.Hand;
            }
            else
            {
                hoveredPrimaryPoint = false;
                hoveredSecondaryPoint = false;
                hoveredPointIndex = -1;
                Cursor = Cursors.Default;
            }

            Invalidate();
        }

        private void UpdateDragTarget(Point location)
        {
            var rect = chartRect.Width > 0 && chartRect.Height > 0 ? chartRect : GetChartRect();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var normalized = 1f - ((location.Y - rect.Top) / (float)rect.Height);
            var value = Clamp((int)Math.Round(normalized * 100f), 0, 100);
            TargetSpeed = value;
            Cursor = Cursors.SizeNS;
        }

        private bool TryHitTestPoint(Point location, out bool primary, out bool secondary, out int pointIndex)
        {
            primary = false;
            secondary = false;
            pointIndex = -1;

            var rect = chartRect.Width > 0 && chartRect.Height > 0 ? chartRect : GetChartRect();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return false;
            }

            var primaryPoints = BuildCurvePoints(true, rect);
            for (var i = 0; i < primaryPoints.Length; i++)
            {
                if (Distance(location, primaryPoints[i]) <= 11f)
                {
                    primary = true;
                    pointIndex = i;
                    return true;
                }
            }

            var secondaryPoints = BuildCurvePoints(false, rect);
            for (var i = 0; i < secondaryPoints.Length; i++)
            {
                if (Distance(location, secondaryPoints[i]) <= 11f)
                {
                    secondary = true;
                    pointIndex = i;
                    return true;
                }
            }

            return false;
        }

        private float Distance(Point location, PointF point)
        {
            var dx = location.X - point.X;
            var dy = location.Y - point.Y;
            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        }

        private void DrawGlow(Graphics graphics, Rectangle bounds)
        {
            var leftGlow = new Rectangle(bounds.Left - 160, bounds.Top - 110, 360, 360);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(leftGlow);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(42, Theme.Success);
                    brush.SurroundColors = new[] { Color.FromArgb(0, Theme.Success) };
                    graphics.FillPath(brush, path);
                }
            }

            var rightGlow = new Rectangle(bounds.Right - 260, bounds.Top - 20, 340, 340);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(rightGlow);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(36, Theme.Info);
                    brush.SurroundColors = new[] { Color.FromArgb(0, Theme.Info) };
                    graphics.FillPath(brush, path);
                }
            }

            var lowerGlow = new Rectangle(bounds.Right - 280, bounds.Bottom - 160, 280, 280);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(lowerGlow);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(20, Theme.Accent);
                    brush.SurroundColors = new[] { Color.FromArgb(0, Theme.Accent) };
                    graphics.FillPath(brush, path);
                }
            }
        }

        private void DrawHeader(Graphics graphics, Rectangle headerRect)
        {
            var titleFont = Theme.SectionFont;
            var captionFont = Theme.SmallFont;

            using (var titleBrush = new SolidBrush(Theme.Text))
            using (var captionBrush = new SolidBrush(Theme.Muted))
            {
                graphics.DrawString("Fan curve preview", titleFont, titleBrush, headerRect.Left, headerRect.Top - 1);

                var subtitle = controlEnabled ? "LIVE" : "STAGED";
                var subtitleSize = graphics.MeasureString(subtitle, captionFont);
                var pillWidth = (int)Math.Ceiling(subtitleSize.Width) + 18;
                var pillRect = new Rectangle(headerRect.Right - pillWidth, headerRect.Top + 1, pillWidth, 22);

                using (var pillPath = Theme.CreateRoundedPath(pillRect, 11))
                using (var fill = new SolidBrush(controlEnabled ? Theme.AccentDark : Theme.Warning))
                using (var border = new Pen(controlEnabled ? Theme.Accent : Theme.Warning))
                using (var textBrush = new SolidBrush(controlEnabled ? Theme.Text : Theme.Window))
                {
                    graphics.FillPath(fill, pillPath);
                    graphics.DrawPath(border, pillPath);
                    graphics.DrawString(subtitle, captionFont, textBrush, pillRect.Left + 9, pillRect.Top + 3);
                }

                var meta = string.Format("Preset {0}  |  CPU {1} C  |  Target {2}%", activePresetName, cpuTemperature, targetSpeed);
                graphics.DrawString(meta, captionFont, captionBrush, headerRect.Left, headerRect.Top + 20);

                var legendY = headerRect.Top + 20;
                var legendX = headerRect.Right - 156;
                DrawLegendChip(graphics, new Rectangle(legendX, legendY, 72, 16), Theme.Success, "Fan 1");
                DrawLegendChip(graphics, new Rectangle(legendX + 76, legendY, 72, 16), Theme.Info, "Fan 2");
            }
        }

        private void DrawChartFrame(Graphics graphics, Rectangle chartRect)
        {
            using (var path = Theme.CreateRoundedPath(chartRect, 16))
            using (var fill = new LinearGradientBrush(chartRect, Theme.Blend(Theme.CardAlt, Theme.Window, 0.16f), Theme.Blend(Theme.Window, Theme.CardAlt, 0.08f), 90f))
            using (var border = new Pen(Color.FromArgb(150, Theme.Border)))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(border, path);

                using (var topHighlight = new Pen(Theme.WithAlpha(Color.White, 24)))
                {
                    graphics.DrawLine(topHighlight, chartRect.Left + 12, chartRect.Top + 1, chartRect.Right - 12, chartRect.Top + 1);
                }
            }
        }

        private void DrawGrid(Graphics graphics, Rectangle chartRect)
        {
            using (var gridPen = new Pen(Color.FromArgb(52, Theme.BorderSoft)))
            using (var tickBrush = new SolidBrush(Theme.Muted))
            using (var smallFont = Theme.SmallFont)
            {
                for (var i = 0; i <= 5; i++)
                {
                    var x = chartRect.Left + (int)Math.Round(chartRect.Width * (i / 5f));
                    graphics.DrawLine(gridPen, x, chartRect.Top, x, chartRect.Bottom);
                }

                for (var i = 0; i <= 5; i++)
                {
                    var y = chartRect.Bottom - (int)Math.Round(chartRect.Height * (i / 5f));
                    graphics.DrawLine(gridPen, chartRect.Left, y, chartRect.Right, y);
                }

                for (var i = 0; i <= 5; i++)
                {
                    var label = (i * 20).ToString();
                    var x = chartRect.Left + (int)Math.Round(chartRect.Width * (i / 5f));
                    var size = graphics.MeasureString(label, smallFont);
                    graphics.DrawString(label, smallFont, tickBrush, x - size.Width / 2f, chartRect.Bottom + 6);
                }

                for (var i = 0; i <= 5; i++)
                {
                    var label = (i * 20).ToString() + "%";
                    var y = chartRect.Bottom - (int)Math.Round(chartRect.Height * (i / 5f));
                    var size = graphics.MeasureString(label, smallFont);
                    graphics.DrawString(label, smallFont, tickBrush, chartRect.Left - size.Width - 10, y - size.Height / 2f - 1);
                }
            }
        }

        private void DrawAxes(Graphics graphics, Rectangle chartRect)
        {
            using (var brush = new SolidBrush(Theme.Muted))
            using (var font = Theme.SmallFont)
            {
                var leftLabel = safeClampEnabled ? string.Format("Safe range {0}-{1}%", safeMinSpeed, safeMaxSpeed) : "Full range 0-100%";
                graphics.DrawString(leftLabel, font, brush, chartRect.Left, chartRect.Bottom + 26);
                graphics.DrawString("Temperature C", font, brush, chartRect.Right - 96, chartRect.Bottom + 26);
            }
        }

        private void DrawFooter(Graphics graphics, Rectangle chartRect)
        {
            var fanSummary = BuildFanSummary();
            if (string.IsNullOrWhiteSpace(fanSummary))
            {
                return;
            }

            using (var brush = new SolidBrush(Theme.Muted))
            using (var font = Theme.SmallFont)
            {
                var size = graphics.MeasureString(fanSummary, font);
                graphics.DrawString(fanSummary, font, brush, chartRect.Right - size.Width, chartRect.Bottom + 26);
            }
        }

        private string BuildFanSummary()
        {
            if (fanSpeeds == null || fanSpeeds.Length == 0)
            {
                return serviceReady ? "No RPM data yet" : "Hardware unavailable";
            }

            var summary = string.Join("  |  ", fanSpeeds.Select((speed, index) => "Fan " + (index + 1) + " " + speed + " RPM"));
            return summary;
        }

        private void DrawCurveFill(Graphics graphics, Rectangle chartRect, PointF[] points, Color color, int alpha)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            var fillPath = new GraphicsPath();
            try
            {
                fillPath.AddCurve(points, 0.35f);
                fillPath.AddLine(points[points.Length - 1], new PointF(points[points.Length - 1].X, chartRect.Bottom));
                fillPath.AddLine(new PointF(points[points.Length - 1].X, chartRect.Bottom), new PointF(points[0].X, chartRect.Bottom));
                fillPath.CloseFigure();

                using (var fill = new SolidBrush(Color.FromArgb(alpha, color)))
                {
                    graphics.FillPath(fill, fillPath);
                }

                using (var edge = new Pen(Color.FromArgb(Math.Min(255, alpha + 28), color), 1.5f))
                {
                    edge.LineJoin = LineJoin.Round;
                    graphics.DrawPath(edge, fillPath);
                }
            }
            finally
            {
                fillPath.Dispose();
            }
        }

        private void DrawCurve(Graphics graphics, PointF[] points, Color color, float width, int alpha)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            using (var glow = new Pen(Color.FromArgb(Math.Min(255, alpha / 2), color), width + 2.2f))
            using (var pen = new Pen(Color.FromArgb(alpha, color), width))
            {
                glow.LineJoin = LineJoin.Round;
                glow.StartCap = LineCap.Round;
                glow.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawCurve(glow, points, 0.35f);
                graphics.DrawCurve(pen, points, 0.35f);
            }
        }

        private void DrawPointMarkers(Graphics graphics, PointF[] points, Color color)
        {
            if (points == null || points.Length == 0)
            {
                return;
            }

            using (var outer = new SolidBrush(Color.FromArgb(220, Theme.Text)))
            using (var inner = new SolidBrush(color))
            using (var border = new Pen(Color.FromArgb(180, Theme.Window), 2f))
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var marker = new RectangleF(point.X - 5.5f, point.Y - 5.5f, 11f, 11f);
                    var isHot = (color == Theme.Success && hoveredPrimaryPoint && hoveredPointIndex == i)
                        || (color == Theme.Info && hoveredSecondaryPoint && hoveredPointIndex == i)
                        || (isDraggingPoint && draggingPointIndex == i && ((draggingPrimaryPoint && color == Theme.Success) || (!draggingPrimaryPoint && color == Theme.Info)));
                    if (isHot)
                    {
                        var glowRect = marker;
                        glowRect.Inflate(6f, 6f);
                        using (var glowBrush = new SolidBrush(Color.FromArgb(42, color)))
                        {
                            graphics.FillEllipse(glowBrush, glowRect);
                        }
                    }

                    graphics.FillEllipse(outer, marker);
                    var innerMarker = marker;
                    innerMarker.Inflate(-1f, -1f);
                    graphics.FillEllipse(inner, innerMarker);
                    graphics.DrawEllipse(border, marker);

                    if (isHot)
                    {
                        using (var accentBorder = new Pen(Color.FromArgb(210, color), 1.5f))
                        {
                            var accentRect = marker;
                            accentRect.Inflate(2f, 2f);
                            graphics.DrawEllipse(accentBorder, accentRect);
                        }
                    }
                }
            }
        }

        private void DrawCursor(Graphics graphics, Rectangle chartRect)
        {
            var x = chartRect.Left + (chartRect.Width * cpuTemperature / 100f);
            using (var linePen = new Pen(Color.FromArgb(controlEnabled ? 220 : 170, Theme.Danger), 2f))
            {
                linePen.DashStyle = DashStyle.Solid;
                graphics.DrawLine(linePen, x, chartRect.Top, x, chartRect.Bottom);

                var label = cpuTemperature.ToString() + " C";
                var size = graphics.MeasureString(label, Theme.SectionFont);
                var bubbleWidth = (int)Math.Ceiling(size.Width) + 18;
                var bubbleHeight = 24;
                var bubbleX = Clamp((int)Math.Round(x - bubbleWidth / 2f), chartRect.Left + 2, chartRect.Right - bubbleWidth - 2);
                var bubbleY = chartRect.Bottom + 2;
                var bubbleRect = new Rectangle(bubbleX, bubbleY, bubbleWidth, bubbleHeight);

                using (var path = Theme.CreateRoundedPath(bubbleRect, 10))
                using (var fill = new SolidBrush(controlEnabled ? Theme.Danger : Theme.AccentDark))
                using (var border = new Pen(controlEnabled ? Theme.Danger : Theme.Accent))
                using (var textBrush = new SolidBrush(Theme.Text))
                {
                    graphics.FillPath(fill, path);
                    graphics.DrawPath(border, path);
                    graphics.DrawString(label, Theme.SectionFont, textBrush, bubbleRect.Left + 8, bubbleRect.Top + 3);
                }
            }
        }

        private void DrawLegendChip(Graphics graphics, Rectangle rect, Color color, string text)
        {
            using (var path = Theme.CreateRoundedPath(rect, 8))
            using (var fill = new SolidBrush(Color.FromArgb(18, color)))
            using (var border = new Pen(Color.FromArgb(110, color)))
            using (var dot = new SolidBrush(color))
            using (var textBrush = new SolidBrush(Theme.Text))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(border, path);
                graphics.FillEllipse(dot, rect.Left + 6, rect.Top + 4, 8, 8);
                graphics.DrawString(text, Theme.SmallFont, textBrush, rect.Left + 18, rect.Top + 2);
            }
        }

        private PointF[] BuildCurvePoints(bool primary, Rectangle chartRect)
        {
            var left = chartRect.Left;
            var top = chartRect.Top;
            var width = chartRect.Width;
            var height = chartRect.Height;

            var start = 20f;
            var firstRise = primary ? 24f : 22f;
            var secondRise = primary ? 36f : 30f;
            var cruise = Clamp(targetSpeed - (primary ? 8 : 14), 20, 100);
            var currentLift = Clamp(targetSpeed - (primary ? 2 : 8), 20, 100);
            var peak = Clamp(targetSpeed + (primary ? 12 : 3), 20, 100);
            var end = primary ? 98f : 86f;

            if (!serviceReady)
            {
                peak = 58;
                end = primary ? 62f : 55f;
            }

            var values = new[]
            {
                new PointF(0f, start),
                new PointF(18f, start),
                new PointF(36f, firstRise),
                new PointF(58f, secondRise),
                new PointF(78f, cruise < secondRise ? secondRise + 4f : cruise),
                new PointF(100f, end)
            };

            if (primary)
            {
                values[3] = new PointF(58f, currentLift);
                values[4] = new PointF(78f, peak);
            }
            else
            {
                values[3] = new PointF(58f, Clamp(targetSpeed - 12, 20, 100));
                values[4] = new PointF(78f, Clamp(targetSpeed + 1, 20, 100));
            }

            var projected = new PointF[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                projected[i] = Project(values[i].X, values[i].Y, left, top, width, height);
            }

            return projected;
        }

        private PointF Project(float xValue, float yValue, int left, int top, int width, int height)
        {
            var x = left + (xValue / ChartMax) * width;
            var y = top + height - ((yValue - ChartMin) / (ChartMax - ChartMin)) * height;
            return new PointF(x, y);
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
