using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;
using NAudio.Wave;
using System.Windows.Controls;
using System.Text.Json;
using Microsoft.VisualBasic; // perlu add reference Microsoft.VisualBasic
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace DesktopWidget
{
    public partial class MainWindow : Window
    {
        private WidgetSettings _settings;
        private Color _visualizerBaseColor = Colors.Aqua;
        private UIElement? _dragTarget;
        private Point _dragOffset;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;
        const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        public MainWindow()
        {
            InitializeComponent();

            _settings = WidgetSettings.Load();

            // Set posisi awal dari file settings (guarded to avoid null dereference)
            if (ClockWidgetControl != null)
            {
                Canvas.SetLeft(ClockWidgetControl, _settings.ClockX);
                Canvas.SetTop(ClockWidgetControl, _settings.ClockY);
                // Apply per-widget settings using widget-level API
                ClockWidgetControl.ApplySettings(_settings);
                // apply saved z-index
                Canvas.SetZIndex(ClockWidgetControl, _settings.ClockZ);
            }

            if (WeatherWidget != null)
            {
                Canvas.SetLeft(WeatherWidget, _settings.WeatherX);
                Canvas.SetTop(WeatherWidget, _settings.WeatherY);
                WeatherWidget.ApplySettings(_settings);
                // apply saved z-index
                Canvas.SetZIndex(WeatherWidget, _settings.WeatherZ);
            }

            if (VisualizerWidget != null)
            {
                Canvas.SetLeft(VisualizerWidget, _settings.VisualizerX);
                Canvas.SetTop(VisualizerWidget, _settings.VisualizerY);
                VisualizerWidget.ApplySettings(_settings);
                // apply saved z-index
                Canvas.SetZIndex(VisualizerWidget, _settings.VisualizerZ);
            }

            // apply colors if stored (swatches/pickers used now)
            ApplySavedColorSelection(null, _settings.ClockColor);
            ApplySavedColorSelection(null, _settings.WeatherTempColor);

            // wire launcher
            HookLauncherEvents();

            // Listen for keys (Ctrl+Z) for undo
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Wire widget events so MainWindow handles editing/saving per-widget (guarded)
            if (ClockWidgetControl != null)
            {
                ClockWidgetControl.DragRequested += Widget_DragRequested;
                ClockWidgetControl.ResizeWidthRequested += (s, e) => ResizeClock_SetWidth(this, new RoutedEventArgs());
                ClockWidgetControl.ResizeHeightRequested += (s, e) => ResizeClock_SetHeight(this, new RoutedEventArgs());
                ClockWidgetControl.RotateRequested += (s, e) => RotateClock_Set(this, new RoutedEventArgs());
                ClockWidgetControl.SkewXRequested += (s, e) => SkewClock_SetX(this, new RoutedEventArgs());
                ClockWidgetControl.SkewYRequested += (s, e) => SkewClock_SetY(this, new RoutedEventArgs());
                ClockWidgetControl.SetZIndexRequested += (s, e) => SetClockZIndex(this, new RoutedEventArgs());
                ClockWidgetControl.OpenSettingsRequested += (s, e) => OpenSettings_Click(this, new RoutedEventArgs());
                ClockWidgetControl.CloseRequested += (s, e) => CloseBtn_Click(this, new RoutedEventArgs());
            }

            if (WeatherWidget != null)
            {
                WeatherWidget.DragRequested += Widget_DragRequested;
                WeatherWidget.ResizeWidthRequested += (s, e) => ResizeWeather_SetWidth(this, new RoutedEventArgs());
                WeatherWidget.ResizeHeightRequested += (s, e) => ResizeWeather_SetHeight(this, new RoutedEventArgs());
                WeatherWidget.RotateRequested += (s, e) => RotateWeather_Set(this, new RoutedEventArgs());
                WeatherWidget.SkewXRequested += (s, e) => SkewWeather_SetX(this, new RoutedEventArgs());
                WeatherWidget.SkewYRequested += (s, e) => SkewWeather_SetY(this, new RoutedEventArgs());
                WeatherWidget.SetZIndexRequested += (s, e) => SetWeatherZIndex(this, new RoutedEventArgs());
                WeatherWidget.OpenSettingsRequested += (s, e) => OpenSettings_Click(this, new RoutedEventArgs());
                WeatherWidget.CloseRequested += (s, e) => CloseBtn_Click(this, new RoutedEventArgs());
            }

            if (VisualizerWidget != null)
            {
                VisualizerWidget.DragRequested += Widget_DragRequested;
                VisualizerWidget.ResizeWidthRequested += (s, e) => ResizeVisualizer_SetWidth(this, new RoutedEventArgs());
                VisualizerWidget.ResizeHeightRequested += (s, e) => ResizeVisualizer_SetHeight(this, new RoutedEventArgs());
                VisualizerWidget.RotateRequested += (s, e) => RotateVisualizer_Set(this, new RoutedEventArgs());
                VisualizerWidget.SkewXRequested += (s, e) => SkewVisualizer_SetX(this, new RoutedEventArgs());
                VisualizerWidget.SkewYRequested += (s, e) => SkewVisualizer_SetY(this, new RoutedEventArgs());
                VisualizerWidget.SetZIndexRequested += (s, e) => SetVisualizerZIndex(this, new RoutedEventArgs());
                VisualizerWidget.OpenSettingsRequested += (s, e) => OpenSettings_Click(this, new RoutedEventArgs());
                VisualizerWidget.CloseRequested += (s, e) => CloseBtn_Click(this, new RoutedEventArgs());
            }

            // subscribe launcher settings events
            if (LauncherControl != null)
            {
                LauncherControl.ClockResizeRequested += (s, e) => ResizeClock_SetWidth(this, new RoutedEventArgs());
                LauncherControl.WeatherResizeRequested += (s, e) => ResizeWeather_SetWidth(this, new RoutedEventArgs());
                LauncherControl.VisualizerResizeRequested += (s, e) => ResizeVisualizer_SetWidth(this, new RoutedEventArgs());
                LauncherControl.UndoRequested += (s, e) => UndoLastAction();
            }

            // Set flag window Win32 transparan and ensure canvas fills screen
            Loaded += (s, e) =>
             {
                 var hwnd = new WindowInteropHelper(this).Handle;
                 int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                 // Don't set WS_EX_TRANSPARENT here — that makes the whole window click-through
                 // and prevents interacting with widgets. Keep layered + toolwindow + noactivate.
                 exStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                 SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

                 // Cari Progman (desktop utama)
                 IntPtr progman = FindWindow("Progman", null);
                 IntPtr desktopHandle = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                 IntPtr workerw = FindWindowEx(IntPtr.Zero, desktopHandle, "WorkerW", null);

                 // Set parent ke WorkerW agar menempel ke desktop
                 SetParent(hwnd, workerw != IntPtr.Zero ? workerw : progman);

                 // Pastikan ukuran tetap fullscreen
                 this.Left = 0;
                 this.Top = 0;
                 this.Width = SystemParameters.PrimaryScreenWidth;
                 this.Height = SystemParameters.PrimaryScreenHeight;
                 RootCanvas.Width = this.Width;
                 RootCanvas.Height = this.Height;
             };

            Loaded += (s, e) =>
            {
                // Start weather updates owned by WeatherWidget
                WeatherWidget?.StartWeatherTimer();
                // Start audio capture in the visualizer widget (guarded)
                if (VisualizerWidget != null)
                {
                    VisualizerWidget.InitializeTransforms(_settings.VisualizerRotate, _settings.VisualizerSkewX, _settings.VisualizerSkewY, (Color)ColorConverter.ConvertFromString(_settings.VisualizerColor ?? "Aqua"));
                    VisualizerWidget.StartAudioCapture();
                }
            };

            Closing += (s, e) => { if (VisualizerWidget != null) VisualizerWidget.StopAudioCapture(); };
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                _dragTarget = element;
                _dragOffset = e.GetPosition(element);
                MouseMove += OnMouseMove;
                MouseLeftButtonUp += OnMouseUp;
            }
        }

        private void Widget_DragRequested(object? sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                _dragTarget = element;
                _dragOffset = e.GetPosition(element);
                MouseMove += OnMouseMove;
                MouseLeftButtonUp += OnMouseUp;
            }
        }

        // Hook LauncherControl events (toggle visibility)
        private void HookLauncherEvents()
        {
            if (LauncherControl == null) return;

            LauncherControl.ClockToggled += (s, enabled) =>
            {
                ClockWidgetControl.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            };
            LauncherControl.WeatherToggled += (s, enabled) =>
            {
                if (WeatherWidget != null) WeatherWidget.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            };
            LauncherControl.VisualizerToggled += (s, enabled) =>
            {
                if (VisualizerWidget != null) VisualizerWidget.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_dragTarget != null)
            {
                var pos = e.GetPosition(this);
                Canvas.SetLeft(_dragTarget, pos.X - _dragOffset.X);
                Canvas.SetTop(_dragTarget, pos.Y - _dragOffset.Y);
            }
        }

        private void OnMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (_dragTarget != null)
            {
                // Simpan posisi tergantung elemen mana yang digeser
                if (_dragTarget == ClockWidgetControl)
                {
                    _settings.ClockX = Canvas.GetLeft(ClockWidgetControl);
                    _settings.ClockY = Canvas.GetTop(ClockWidgetControl);
                }
                else if (_dragTarget == WeatherWidget)
                {
                    _settings.WeatherX = Canvas.GetLeft(WeatherWidget);
                    _settings.WeatherY = Canvas.GetTop(WeatherWidget);
                }
                else if (_dragTarget == VisualizerWidget)
                {
                    _settings.VisualizerX = Canvas.GetLeft(VisualizerWidget);
                    _settings.VisualizerY = Canvas.GetTop(VisualizerWidget);
                }

                _settings.Save();  // Simpan ke file

                _dragTarget = null;
                MouseMove -= OnMouseMove;
                MouseLeftButtonUp -= OnMouseUp;
            }
        }

        // --- Undo stack for widget actions (resize, rotate, skew) ---
        private enum WidgetActionType { Resize, Rotate, SkewX, SkewY }
        private record WidgetAction(string Widget, WidgetActionType ActionType, double OldValue1, double OldValue2);
        private readonly System.Collections.Generic.Stack<WidgetAction> _undoStack = new();

        private void PushWidgetUndo(string widgetName, WidgetActionType actionType, double old1, double old2 = Double.NaN)
        {
            _undoStack.Push(new WidgetAction(widgetName, actionType, old1, old2));
        }

        private void UndoLastAction()
        {
            if (_undoStack.Count == 0) return;
            var a = _undoStack.Pop();
            switch (a.Widget)
            {
                case "Clock":
                    if (ClockWidgetControl != null)
                    {
                        if (a.ActionType == WidgetActionType.Resize)
                        {
                            ClockWidgetControl.Width = a.OldValue1;
                            ClockWidgetControl.Height = a.OldValue2;
                            _settings.ClockWidth = a.OldValue1;
                            _settings.ClockHeight = a.OldValue2;
                        }
                        else if (a.ActionType == WidgetActionType.Rotate)
                        {
                            if (ClockWidgetControl.ClockRotateTransform != null) ClockWidgetControl.ClockRotateTransform.Angle = a.OldValue1;
                            _settings.ClockRotate = a.OldValue1;
                        }
                        else if (a.ActionType == WidgetActionType.SkewX)
                        {
                            if (ClockWidgetControl.ClockSkewTransform != null) ClockWidgetControl.ClockSkewTransform.AngleX = a.OldValue1;
                            _settings.ClockSkewX = a.OldValue1;
                        }
                        else if (a.ActionType == WidgetActionType.SkewY)
                        {
                            if (ClockWidgetControl.ClockSkewTransform != null) ClockWidgetControl.ClockSkewTransform.AngleY = a.OldValue1;
                            _settings.ClockSkewY = a.OldValue1;
                        }
                        _settings.Save();
                    }
                    break;
                case "Weather":
                    if (WeatherWidget != null)
                    {
                        if (a.ActionType == WidgetActionType.Resize)
                        {
                            WeatherWidget.Width = a.OldValue1;
                            WeatherWidget.Height = a.OldValue2;
                            _settings.WeatherWidth = a.OldValue1;
                            _settings.WeatherHeight = a.OldValue2;
                        }
                        else if (a.ActionType == WidgetActionType.Rotate)
                        {
                            if (WeatherWidget.WeatherRotateTransform != null) WeatherWidget.WeatherRotateTransform.Angle = a.OldValue1;
                            _settings.WeatherRotate = a.OldValue1;
                        }
                        else if (a.ActionType == WidgetActionType.SkewX)
                        {
                            if (WeatherWidget.WeatherSkewTransform != null) WeatherWidget.WeatherSkewTransform.AngleX = a.OldValue1;
                            _settings.WeatherSkewX = a.OldValue1;
                        }
                        else if (a.ActionType == WidgetActionType.SkewY)
                        {
                            if (WeatherWidget.WeatherSkewTransform != null) WeatherWidget.WeatherSkewTransform.AngleY = a.OldValue1;
                            _settings.WeatherSkewY = a.OldValue1;
                        }
                        _settings.Save();
                    }
                    break;
                case "Visualizer":
                    if (VisualizerWidget != null)
                    {
                        if (a.ActionType == WidgetActionType.Resize)
                        {
                            VisualizerWidget.Width = a.OldValue1;
                            VisualizerWidget.Height = a.OldValue2;
                            _settings.VisualizerWidth = a.OldValue1;
                            _settings.VisualizerHeight = a.OldValue2;
                        }
                        else if (a.ActionType == WidgetActionType.Rotate)
                        {
                            if (VisualizerWidget.VisualizerRotateTransform != null) VisualizerWidget.VisualizerRotateTransform.Angle = a.OldValue1;
                            _settings.VisualizerRotate = a.OldValue1;
                        }
                        else if (a.ActionType == WidgetActionType.SkewX)
                        {
                            if (VisualizerWidget.VisualizerSkewTransform != null) VisualizerWidget.VisualizerSkewTransform.AngleX = a.OldValue1;
                            _settings.VisualizerSkewX = a.OldValue1;
                        }
                        else if (a.ActionType == WidgetActionType.SkewY)
                        {
                            if (VisualizerWidget.VisualizerSkewTransform != null) VisualizerWidget.VisualizerSkewTransform.AngleY = a.OldValue1;
                            _settings.VisualizerSkewY = a.OldValue1;
                        }
                        _settings.Save();
                    }
                    break;
            }
        }

        private void MainWindow_PreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.Z)
            {
                UndoLastAction();
                e.Handled = true;
            }
        }
        private void ApplySavedColorSelection(ComboBox? combo, string colorName)
        {
            // Backwards-compatible: try to set ComboBox selection if a combo was passed
            if (combo != null && !string.IsNullOrEmpty(colorName))
            {
                foreach (var item in combo.Items)
                {
                    if (item is ComboBoxItem cbi && (cbi.Content?.ToString() ?? string.Empty).Equals(colorName, StringComparison.OrdinalIgnoreCase))
                    {
                        combo.SelectedItem = cbi;
                        break;
                    }
                }
            }

            // Apply saved colors to swatches/texts and widgets
            ApplySavedColorToSwatch(ClockColorSwatch, ClockColorText, _settings.ClockColor, defaultHex: "#FFFFFF");
            ApplySavedColorToSwatch(WeatherTempColorSwatch, WeatherTempColorText, _settings.WeatherTempColor, defaultHex: "#FFFFFF");
            ApplySavedColorToSwatch(WeatherLocationColorSwatch, WeatherLocationColorText, _settings.WeatherLocationColor, defaultHex: "#FFFFFF");
            ApplySavedColorToSwatch(WeatherSummaryColorSwatch, WeatherSummaryColorText, _settings.WeatherSummaryColor, defaultHex: "#FFFFFF");
            ApplySavedColorToSwatch(WeatherDetailsColorSwatch, WeatherDetailsColorText, _settings.WeatherDetailsColor, defaultHex: "#FFFFFF");
            ApplySavedColorToSwatch(VisualizerColorSwatch, VisualizerColorText, _settings.VisualizerColor, defaultHex: "#00FFFF");
        }

        private void ApplySavedColorToSwatch(Border swatch, TextBlock label, string colorHex, string defaultHex = "#FFFFFF")
        {
            if (swatch == null || label == null) return;
            var hex = string.IsNullOrEmpty(colorHex) ? defaultHex : colorHex;
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                swatch.Background = new SolidColorBrush(c);
                label.Text = hex;
                label.Foreground = new SolidColorBrush(ComputeReadableTextColor(c));

                // also apply to widgets immediately
                var brush = new SolidColorBrush(c);
                if (label == ClockColorText)
                {
                    if (ClockWidgetControl?.DayTextBlock != null)
                    {
                        ClockWidgetControl.DayTextBlock.Foreground = brush;
                        ClockWidgetControl.DateTextBlock.Foreground = brush;
                        ClockWidgetControl.TimeTextBlock.Foreground = brush;
                    }
                }
                if (label == WeatherTempColorText && WeatherWidget?.TempTextBlock != null) WeatherWidget.TempTextBlock.Foreground = brush;
                if (label == WeatherLocationColorText && WeatherWidget?.LocationTextBlock != null) WeatherWidget.LocationTextBlock.Foreground = brush;
                if (label == WeatherSummaryColorText && WeatherWidget?.SummaryTextBlock != null) WeatherWidget.SummaryTextBlock.Foreground = brush;
                if (label == WeatherDetailsColorText && WeatherWidget?.DetailsTextBlock != null) WeatherWidget.DetailsTextBlock.Foreground = brush;
                if (label == VisualizerColorText)
                {
                    _visualizerBaseColor = c;
                    VisualizerWidget?.SetBaseColor(c);
                }
            }
            catch { }
        }

        private Color ComputeReadableTextColor(Color back)
        {
            // choose black/white depending on luminance
            double l = (0.299 * back.R + 0.587 * back.G + 0.114 * back.B) / 255.0;
            return l > 0.6 ? Colors.Black : Colors.White;
        }

        private void RestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;

            _settings.RestoreDefaults();
            _settings.Save();

            // Terapkan posisi default ke UI
            Canvas.SetLeft(ClockWidgetControl, _settings.ClockX);
            Canvas.SetTop(ClockWidgetControl, _settings.ClockY);

            Canvas.SetLeft(WeatherWidget, _settings.WeatherX);
            Canvas.SetTop(WeatherWidget, _settings.WeatherY);

            Canvas.SetLeft(VisualizerWidget, _settings.VisualizerX);
            Canvas.SetTop(VisualizerWidget, _settings.VisualizerY);
        }
        // helper
        private string GetStringSafe(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? "",
                JsonValueKind.Number => el.GetRawText(), // kalau angka biar jadi string
                _ => ""
            };
        }
    }
}
