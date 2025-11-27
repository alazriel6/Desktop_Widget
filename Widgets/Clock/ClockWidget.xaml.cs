using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // <- wajib untuk RotateTransform / SkewTransform
using System.Windows.Threading;

namespace DesktopWidget.Widgets
{
    public partial class ClockWidget : UserControl
    {
        public TextBlock DayTextBlock => DayText;
        public TextBlock DateTextBlock => DateText;
        public TextBlock TimeTextBlock => TimeText;
        public Border ClockBorder => ClockPanel;
        public RotateTransform ClockRotateTransform => ClockRotate;
    public SkewTransform ClockSkewTransform => ClockSkew;

        // Events raised to parent window so MainWindow can handle editing/saving
        public event MouseButtonEventHandler? DragRequested;
        public event EventHandler? ResizeWidthRequested;
        public event EventHandler? ResizeHeightRequested;
        public event EventHandler? RotateRequested;
        public event EventHandler? SkewXRequested;
        public event EventHandler? SkewYRequested;
        public event EventHandler? SetZIndexRequested;
        public event EventHandler? OpenSettingsRequested;
        public event EventHandler? CloseRequested;

    // _rotate/_skew fields removed because transforms are accessed via the named elements in XAML
        private DispatcherTimer _clockTimer;

        public ClockWidget()
        {
            InitializeComponent();

            // widget owns its own clock timer now
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            var culture = new System.Globalization.CultureInfo("en-US");

            DayTextBlock.Text = now.ToString("dddd", culture).ToUpper();
            DateTextBlock.Text = now.ToString("dd MMM yyyy", culture).ToUpper();
            TimeTextBlock.Text = now.ToString("HH:mm", culture);
        }

        // Apply settings from central WidgetSettings
        public void ApplySettings(WidgetSettings s)
        {
            if (s == null) return;

            if (!double.IsNaN(s.ClockWidth)) this.Width = s.ClockWidth; else this.Width = Double.NaN;
            if (!double.IsNaN(s.ClockHeight)) this.Height = s.ClockHeight; else this.Height = Double.NaN;

            if (ClockRotateTransform != null) ClockRotateTransform.Angle = s.ClockRotate;
            if (ClockSkewTransform != null)
            {
                ClockSkewTransform.AngleX = s.ClockSkewX;
                ClockSkewTransform.AngleY = s.ClockSkewY;
            }

            try
            {
                var c = (Color)ColorConverter.ConvertFromString(s.ClockColor ?? "White");
                var brush = new SolidColorBrush(c);
                DayTextBlock.Foreground = brush;
                DateTextBlock.Foreground = brush;
                TimeTextBlock.Foreground = brush;
            }
            catch { }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Raise drag request to MainWindow which will perform per-widget dragging
            DragRequested?.Invoke(this, e);
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        private void ResizeClock_SetWidth(object sender, RoutedEventArgs e) { ResizeWidthRequested?.Invoke(this, EventArgs.Empty); }
        private void ResizeClock_SetHeight(object sender, RoutedEventArgs e) { ResizeHeightRequested?.Invoke(this, EventArgs.Empty); }
        private void RotateClock_Set(object sender, RoutedEventArgs e) { RotateRequested?.Invoke(this, EventArgs.Empty); }
        private void SkewClock_SetX(object sender, RoutedEventArgs e) { SkewXRequested?.Invoke(this, EventArgs.Empty); }
        private void SkewClock_SetY(object sender, RoutedEventArgs e) { SkewYRequested?.Invoke(this, EventArgs.Empty); }
        private void SetClockZIndex(object sender, RoutedEventArgs e) { SetZIndexRequested?.Invoke(this, EventArgs.Empty); }
    }
}
