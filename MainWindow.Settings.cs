using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms; // for ColorDialog
using Microsoft.VisualBasic; // untuk InputBox

namespace DesktopWidget
{
    public partial class MainWindow
    {
        // === SETTINGS PANEL ===
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        // === CLOCK ===

        private void ResizeClock_SetWidth(object sender, RoutedEventArgs e)
        {
            var defaultWidth = ClockWidgetControl?.Width ?? _settings.ClockWidth;
            var input = Interaction.InputBox("Masukkan lebar (px):", "Set Lebar Clock", defaultWidth.ToString());
            if (double.TryParse(input, out double val))
            {
                if (ClockWidgetControl != null)
                {
                    // push undo (old size)
                    PushWidgetUndo("Clock", WidgetActionType.Resize, ClockWidgetControl.Width, ClockWidgetControl.Height);
                    ClockWidgetControl.Width = val;
                }
                _settings.ClockWidth = val;
                _settings.Save();
            }
        }

        private void ResizeClock_SetHeight(object sender, RoutedEventArgs e)
        {
            var defaultHeight = ClockWidgetControl?.Height ?? _settings.ClockHeight;
            var input = Interaction.InputBox("Masukkan tinggi (px):", "Set Tinggi Clock", defaultHeight.ToString());
            if (double.TryParse(input, out double val))
            {
                if (ClockWidgetControl != null)
                {
                    PushWidgetUndo("Clock", WidgetActionType.Resize, ClockWidgetControl.Width, ClockWidgetControl.Height);
                    ClockWidgetControl.Height = val;
                }
                _settings.ClockHeight = val;
                _settings.Save();
            }
        }

        private void RotateClock_Set(object sender, RoutedEventArgs e)
        {
            var defaultAngle = ClockWidgetControl?.ClockRotateTransform?.Angle ?? _settings.ClockRotate;
            var input = Interaction.InputBox("Masukkan sudut rotasi (°):", "Set Rotasi Clock", defaultAngle.ToString());
            if (double.TryParse(input, out double val))
            {
                var old = ClockWidgetControl?.ClockRotateTransform?.Angle ?? _settings.ClockRotate;
                if (ClockWidgetControl?.ClockRotateTransform != null) ClockWidgetControl.ClockRotateTransform.Angle = val;
                // push undo for rotate
                PushWidgetUndo("Clock", WidgetActionType.Rotate, old);
                _settings.ClockRotate = val;
                _settings.Save();
            }
        }

        private void SkewClock_SetX(object sender, RoutedEventArgs e)
        {
            var defaultVal = ClockWidgetControl?.ClockSkewTransform?.AngleX ?? _settings.ClockSkewX;
            var input = Interaction.InputBox("Masukkan Skew X:", "Set Skew X Clock", defaultVal.ToString());
            if (double.TryParse(input, out double val))
            {
                var old = ClockWidgetControl?.ClockSkewTransform?.AngleX ?? _settings.ClockSkewX;
                if (ClockWidgetControl?.ClockSkewTransform != null) ClockWidgetControl.ClockSkewTransform.AngleX = val;
                PushWidgetUndo("Clock", WidgetActionType.SkewX, old);
                _settings.ClockSkewX = val;
                _settings.Save();
            }
        }

        private void SkewClock_SetY(object sender, RoutedEventArgs e)
        {
            var defaultVal = ClockWidgetControl?.ClockSkewTransform?.AngleY ?? _settings.ClockSkewY;
            var input = Interaction.InputBox("Masukkan Skew Y:", "Set Skew Y Clock", defaultVal.ToString());
            if (double.TryParse(input, out double val))
            {
                var old = ClockWidgetControl?.ClockSkewTransform?.AngleY ?? _settings.ClockSkewY;
                if (ClockWidgetControl?.ClockSkewTransform != null) ClockWidgetControl.ClockSkewTransform.AngleY = val;
                PushWidgetUndo("Clock", WidgetActionType.SkewY, old);
                _settings.ClockSkewY = val;
                _settings.Save();
            }
        }

        // === WEATHER ===
        // Weather color pickers now use Pick buttons and swatches

        // ----- Color pick dialog handlers (custom color selection) -----
        private System.Windows.Media.Color FromWinFormsColor(System.Drawing.Color c)
        {
            return System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        private void ApplyClockColor(Color color)
        {
            if (ClockWidgetControl?.DayTextBlock != null)
                ClockWidgetControl.DayTextBlock.Foreground = new SolidColorBrush(color);
            if (ClockWidgetControl?.DateTextBlock != null)
                ClockWidgetControl.DateTextBlock.Foreground = new SolidColorBrush(color);
            if (ClockWidgetControl?.TimeTextBlock != null)
                ClockWidgetControl.TimeTextBlock.Foreground = new SolidColorBrush(color);
        }

        private void ClockColorPick_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (ShowDialogResultIsOK(dlg))
                {
                    var wpf = FromWinFormsColor(dlg.Color);
                    ApplyClockColor(wpf);
                    var hex = $"#{wpf.R:X2}{wpf.G:X2}{wpf.B:X2}";
                    _settings.ClockColor = hex;
                    // update UI swatch/text
                    if (ClockColorSwatch != null) ClockColorSwatch.Background = new SolidColorBrush(wpf);
                    if (ClockColorText != null) { ClockColorText.Text = hex; ClockColorText.Foreground = new SolidColorBrush(ComputeReadableTextColor(wpf)); }
                    _settings.Save();
                }
            }
        }

        private void WeatherTempColorPick_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (ShowDialogResultIsOK(dlg))
                {
                    var wpf = FromWinFormsColor(dlg.Color);
                    if (WeatherWidget?.TempTextBlock != null) WeatherWidget.TempTextBlock.Foreground = new SolidColorBrush(wpf);
                    var hex2 = $"#{wpf.R:X2}{wpf.G:X2}{wpf.B:X2}";
                    _settings.WeatherTempColor = hex2;
                    if (WeatherTempColorSwatch != null) WeatherTempColorSwatch.Background = new SolidColorBrush(wpf);
                    if (WeatherTempColorText != null) { WeatherTempColorText.Text = hex2; WeatherTempColorText.Foreground = new SolidColorBrush(ComputeReadableTextColor(wpf)); }
                    _settings.Save();
                    WeatherWidget?.ApplySettings(_settings);
                }
            }
        }

        private void WeatherLocationColorPick_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (ShowDialogResultIsOK(dlg))
                {
                    var wpf = FromWinFormsColor(dlg.Color);
                    if (WeatherWidget?.LocationTextBlock != null) WeatherWidget.LocationTextBlock.Foreground = new SolidColorBrush(wpf);
                    var hex3 = $"#{wpf.R:X2}{wpf.G:X2}{wpf.B:X2}";
                    _settings.WeatherLocationColor = hex3;
                    if (WeatherLocationColorSwatch != null) WeatherLocationColorSwatch.Background = new SolidColorBrush(wpf);
                    if (WeatherLocationColorText != null) { WeatherLocationColorText.Text = hex3; WeatherLocationColorText.Foreground = new SolidColorBrush(ComputeReadableTextColor(wpf)); }
                    _settings.Save();
                    WeatherWidget?.ApplySettings(_settings);
                }
            }
        }

        private void WeatherSummaryColorPick_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (ShowDialogResultIsOK(dlg))
                {
                    var wpf = FromWinFormsColor(dlg.Color);
                    if (WeatherWidget?.SummaryTextBlock != null) WeatherWidget.SummaryTextBlock.Foreground = new SolidColorBrush(wpf);
                    var hex4 = $"#{wpf.R:X2}{wpf.G:X2}{wpf.B:X2}";
                    _settings.WeatherSummaryColor = hex4;
                    if (WeatherSummaryColorSwatch != null) WeatherSummaryColorSwatch.Background = new SolidColorBrush(wpf);
                    if (WeatherSummaryColorText != null) { WeatherSummaryColorText.Text = hex4; WeatherSummaryColorText.Foreground = new SolidColorBrush(ComputeReadableTextColor(wpf)); }
                    _settings.Save();
                    WeatherWidget?.ApplySettings(_settings);
                }
            }
        }

        private void WeatherDetailsColorPick_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (ShowDialogResultIsOK(dlg))
                {
                    var wpf = FromWinFormsColor(dlg.Color);
                    if (WeatherWidget?.DetailsTextBlock != null) WeatherWidget.DetailsTextBlock.Foreground = new SolidColorBrush(wpf);
                    var hex5 = $"#{wpf.R:X2}{wpf.G:X2}{wpf.B:X2}";
                    _settings.WeatherDetailsColor = hex5;
                    if (WeatherDetailsColorSwatch != null) WeatherDetailsColorSwatch.Background = new SolidColorBrush(wpf);
                    if (WeatherDetailsColorText != null) { WeatherDetailsColorText.Text = hex5; WeatherDetailsColorText.Foreground = new SolidColorBrush(ComputeReadableTextColor(wpf)); }
                    _settings.Save();
                    WeatherWidget?.ApplySettings(_settings);
                }
            }
        }

        private void VisualizerColorPick_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (ShowDialogResultIsOK(dlg))
                {
                    var wpf = FromWinFormsColor(dlg.Color);
                    _visualizerBaseColor = wpf;
                    VisualizerWidget?.SetBaseColor(wpf);
                    var hex6 = $"#{wpf.R:X2}{wpf.G:X2}{wpf.B:X2}";
                    _settings.VisualizerColor = hex6;
                    if (VisualizerColorSwatch != null) VisualizerColorSwatch.Background = new SolidColorBrush(wpf);
                    if (VisualizerColorText != null) { VisualizerColorText.Text = hex6; VisualizerColorText.Foreground = new SolidColorBrush(ComputeReadableTextColor(wpf)); }
                    _settings.Save();
                }
            }
        }

        private bool ShowDialogResultIsOK(object dlg)
        {
            try
            {
                var mi = dlg.GetType().GetMethod("ShowDialog", Type.EmptyTypes);
                if (mi == null) return false;
                var res = mi.Invoke(dlg, null);
                if (res == null) return false;
                var s = res.ToString();
                return string.Equals(s, "OK", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void ResizeWeather_SetWidth(object sender, RoutedEventArgs e)
        {
            var defaultWidth = WeatherWidget?.Width ?? _settings.WeatherWidth;
            var input = Interaction.InputBox("Masukkan lebar (px):", "Set Lebar Weather", defaultWidth.ToString());
            if (double.TryParse(input, out double val))
            {
                if (WeatherWidget != null)
                {
                    PushWidgetUndo("Weather", WidgetActionType.Resize, WeatherWidget.Width, WeatherWidget.Height);
                    WeatherWidget.Width = val;
                }
                _settings.WeatherWidth = val;
                _settings.Save();
            }
        }

        private void ResizeWeather_SetHeight(object sender, RoutedEventArgs e)
        {
            var defaultHeight = WeatherWidget?.Height ?? _settings.WeatherHeight;
            var input = Interaction.InputBox("Masukkan tinggi (px):", "Set Tinggi Weather", defaultHeight.ToString());
            if (double.TryParse(input, out double val))
            {
                if (WeatherWidget != null)
                {
                    PushWidgetUndo("Weather", WidgetActionType.Resize, WeatherWidget.Width, WeatherWidget.Height);
                    WeatherWidget.Height = val;
                }
                _settings.WeatherHeight = val;
                _settings.Save();
            }
        }

        private void RotateWeather_Set(object sender, RoutedEventArgs e)
        {
            var defaultAngle = WeatherWidget?.WeatherRotateTransform?.Angle ?? _settings.WeatherRotate;
            var input = Interaction.InputBox("Masukkan sudut rotasi (°):", "Set Rotasi Weather", defaultAngle.ToString());
            if (double.TryParse(input, out double val))
            {
                var old = WeatherWidget?.WeatherRotateTransform?.Angle ?? _settings.WeatherRotate;
                if (WeatherWidget?.WeatherRotateTransform != null) WeatherWidget.WeatherRotateTransform.Angle = val;
                PushWidgetUndo("Weather", WidgetActionType.Rotate, old);
                _settings.WeatherRotate = val;
                _settings.Save();
            }
        }

        private void SkewWeather_SetX(object sender, RoutedEventArgs e)
        {
            var defaultVal = WeatherWidget?.WeatherSkewTransform?.AngleX ?? _settings.WeatherSkewX;
            var input = Interaction.InputBox("Masukkan Skew X:", "Set Skew X Weather", defaultVal.ToString());
            if (double.TryParse(input, out double val))
            {
                var old = WeatherWidget?.WeatherSkewTransform?.AngleX ?? _settings.WeatherSkewX;
                if (WeatherWidget?.WeatherSkewTransform != null) WeatherWidget.WeatherSkewTransform.AngleX = val;
                PushWidgetUndo("Weather", WidgetActionType.SkewX, old);
                _settings.WeatherSkewX = val;
                _settings.Save();
            }
        }

        private void SkewWeather_SetY(object sender, RoutedEventArgs e)
        {
            var defaultVal = WeatherWidget?.WeatherSkewTransform?.AngleY ?? _settings.WeatherSkewY;
            var input = Interaction.InputBox("Masukkan Skew Y:", "Set Skew Y Weather", defaultVal.ToString());
            if (double.TryParse(input, out double val))
            {
                var old = WeatherWidget?.WeatherSkewTransform?.AngleY ?? _settings.WeatherSkewY;
                if (WeatherWidget?.WeatherSkewTransform != null) WeatherWidget.WeatherSkewTransform.AngleY = val;
                PushWidgetUndo("Weather", WidgetActionType.SkewY, old);
                _settings.WeatherSkewY = val;
                _settings.Save();
            }
        }

        // === VISUALIZER ===
        // Visualizer color now uses Pick button and swatch

        private void RotateVisualizer_Set(object sender, RoutedEventArgs e)
        {
            var defaultAngle = VisualizerWidget?.VisualizerRotateTransform?.Angle ?? _settings.VisualizerRotate;
            var input = Interaction.InputBox("Masukkan sudut rotasi (°):", "Rotate Visualizer", defaultAngle.ToString());
            if (double.TryParse(input, out double angle) && VisualizerWidget?.VisualizerRotateTransform is RotateTransform rt)
            {
                rt.Angle = angle;
                _settings.VisualizerRotate = angle;
                _settings.Save();
            }
        }

        private void SkewVisualizer_SetX(object sender, RoutedEventArgs e)
        {
            var defaultAngle = VisualizerWidget?.VisualizerSkewTransform?.AngleX ?? _settings.VisualizerSkewX;
            var input = Interaction.InputBox("Masukkan Skew X (°):", "Skew X", defaultAngle.ToString());
            if (double.TryParse(input, out double angle) && VisualizerWidget?.VisualizerSkewTransform is SkewTransform sx)
            {
                sx.AngleX = angle;
                _settings.VisualizerSkewX = angle;
                _settings.Save();
            }
        }

        private void SkewVisualizer_SetY(object sender, RoutedEventArgs e)
        {
            var defaultAngle = VisualizerWidget?.VisualizerSkewTransform?.AngleY ?? _settings.VisualizerSkewY;
            var input = Interaction.InputBox("Masukkan Skew Y (°):", "Skew Y", defaultAngle.ToString());
            if (double.TryParse(input, out double angle) && VisualizerWidget?.VisualizerSkewTransform is SkewTransform sy)
            {
                sy.AngleY = angle;
                _settings.VisualizerSkewY = angle;
                _settings.Save();
            }
        }


        private void ResizeVisualizer_SetWidth(object sender, RoutedEventArgs e)
        {
            var defaultWidth = VisualizerWidget?.Width ?? _settings.VisualizerWidth;
            var input = Interaction.InputBox("Masukkan lebar baru (px):", "Set Lebar Visualizer", defaultWidth.ToString());
            if (double.TryParse(input, out double newWidth) && newWidth > 0)
            {
                if (VisualizerWidget != null)
                {
                    PushWidgetUndo("Visualizer", WidgetActionType.Resize, VisualizerWidget.Width, VisualizerWidget.Height);
                    VisualizerWidget.Width = newWidth;
                }
                _settings.VisualizerWidth = newWidth;
                _settings.Save();
            }
        }

        private void ResizeVisualizer_SetHeight(object sender, RoutedEventArgs e)
        {
            var defaultHeight = VisualizerWidget?.Height ?? _settings.VisualizerHeight;
            var input = Interaction.InputBox("Masukkan tinggi baru (px):", "Set Tinggi Visualizer", defaultHeight.ToString());
            if (double.TryParse(input, out double newHeight) && newHeight > 0)
            {
                if (VisualizerWidget != null)
                {
                    PushWidgetUndo("Visualizer", WidgetActionType.Resize, VisualizerWidget.Width, VisualizerWidget.Height);
                    VisualizerWidget.Height = newHeight;
                }
                _settings.VisualizerHeight = newHeight;
                _settings.Save();
            }
        }

        // === RESET HELPERS ===
        private void ResetClock_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            // restore only clock-related transforms and size
            _settings.ClockWidth = def.ClockWidth;
            _settings.ClockHeight = def.ClockHeight;
            _settings.ClockRotate = def.ClockRotate;
            _settings.ClockSkewX = def.ClockSkewX;
            _settings.ClockSkewY = def.ClockSkewY;
            _settings.Save();

            // apply to UI
            if (ClockWidgetControl != null) ClockWidgetControl.ApplySettings(_settings);
        }

        private void ResetWeather_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            _settings.WeatherWidth = def.WeatherWidth;
            _settings.WeatherHeight = def.WeatherHeight;
            _settings.WeatherRotate = def.WeatherRotate;
            _settings.WeatherSkewX = def.WeatherSkewX;
            _settings.WeatherSkewY = def.WeatherSkewY;
            _settings.Save();

            if (WeatherWidget != null) WeatherWidget.ApplySettings(_settings);
        }

        private void ResetVisualizer_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            _settings.VisualizerWidth = def.VisualizerWidth;
            _settings.VisualizerHeight = def.VisualizerHeight;
            _settings.VisualizerRotate = def.VisualizerRotate;
            _settings.VisualizerSkewX = def.VisualizerSkewX;
            _settings.VisualizerSkewY = def.VisualizerSkewY;
            _settings.Save();

            if (VisualizerWidget != null) VisualizerWidget.ApplySettings(_settings);
        }

        // --- Per-widget more granular resets (size / rotate / skew) ---
        private void ResetClockSize_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            if (ClockWidgetControl != null) PushWidgetUndo("Clock", WidgetActionType.Resize, ClockWidgetControl.Width, ClockWidgetControl.Height);
            _settings.ClockWidth = def.ClockWidth;
            _settings.ClockHeight = def.ClockHeight;
            _settings.Save();
            if (ClockWidgetControl != null) ClockWidgetControl.ApplySettings(_settings);
        }

        private void ResetClockRotate_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            var old = ClockWidgetControl?.ClockRotateTransform?.Angle ?? _settings.ClockRotate;
            PushWidgetUndo("Clock", WidgetActionType.Rotate, old);
            _settings.ClockRotate = def.ClockRotate;
            _settings.Save();
            if (ClockWidgetControl != null) ClockWidgetControl.ApplySettings(_settings);
        }

        private void ResetClockSkew_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            var oldX = ClockWidgetControl?.ClockSkewTransform?.AngleX ?? _settings.ClockSkewX;
            var oldY = ClockWidgetControl?.ClockSkewTransform?.AngleY ?? _settings.ClockSkewY;
            PushWidgetUndo("Clock", WidgetActionType.SkewX, oldX);
            PushWidgetUndo("Clock", WidgetActionType.SkewY, oldY);
            _settings.ClockSkewX = def.ClockSkewX;
            _settings.ClockSkewY = def.ClockSkewY;
            _settings.Save();
            if (ClockWidgetControl != null) ClockWidgetControl.ApplySettings(_settings);
        }

        // Perspective/scale features removed — keep clock simple with rotate/skew only.

        private void ResetWeatherSize_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            if (WeatherWidget != null) PushWidgetUndo("Weather", WidgetActionType.Resize, WeatherWidget.Width, WeatherWidget.Height);
            _settings.WeatherWidth = def.WeatherWidth;
            _settings.WeatherHeight = def.WeatherHeight;
            _settings.Save();
            if (WeatherWidget != null) WeatherWidget.ApplySettings(_settings);
        }

        private void ResetWeatherRotate_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            var old = WeatherWidget?.WeatherRotateTransform?.Angle ?? _settings.WeatherRotate;
            PushWidgetUndo("Weather", WidgetActionType.Rotate, old);
            _settings.WeatherRotate = def.WeatherRotate;
            _settings.Save();
            if (WeatherWidget != null) WeatherWidget.ApplySettings(_settings);
        }

        private void ResetWeatherSkew_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            var oldX = WeatherWidget?.WeatherSkewTransform?.AngleX ?? _settings.WeatherSkewX;
            var oldY = WeatherWidget?.WeatherSkewTransform?.AngleY ?? _settings.WeatherSkewY;
            PushWidgetUndo("Weather", WidgetActionType.SkewX, oldX);
            PushWidgetUndo("Weather", WidgetActionType.SkewY, oldY);
            _settings.WeatherSkewX = def.WeatherSkewX;
            _settings.WeatherSkewY = def.WeatherSkewY;
            _settings.Save();
            if (WeatherWidget != null) WeatherWidget.ApplySettings(_settings);
        }

        private void ResetVisualizerSize_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            if (VisualizerWidget != null) PushWidgetUndo("Visualizer", WidgetActionType.Resize, VisualizerWidget.Width, VisualizerWidget.Height);
            _settings.VisualizerWidth = def.VisualizerWidth;
            _settings.VisualizerHeight = def.VisualizerHeight;
            _settings.Save();
            if (VisualizerWidget != null) VisualizerWidget.ApplySettings(_settings);
        }

        private void ResetVisualizerRotate_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            var old = VisualizerWidget?.VisualizerRotateTransform?.Angle ?? _settings.VisualizerRotate;
            PushWidgetUndo("Visualizer", WidgetActionType.Rotate, old);
            _settings.VisualizerRotate = def.VisualizerRotate;
            _settings.Save();
            if (VisualizerWidget != null) VisualizerWidget.ApplySettings(_settings);
        }

        private void ResetVisualizerSkew_Click(object sender, RoutedEventArgs e)
        {
            var def = new WidgetSettings();
            var oldX = VisualizerWidget?.VisualizerSkewTransform?.AngleX ?? _settings.VisualizerSkewX;
            var oldY = VisualizerWidget?.VisualizerSkewTransform?.AngleY ?? _settings.VisualizerSkewY;
            PushWidgetUndo("Visualizer", WidgetActionType.SkewX, oldX);
            PushWidgetUndo("Visualizer", WidgetActionType.SkewY, oldY);
            _settings.VisualizerSkewX = def.VisualizerSkewX;
            _settings.VisualizerSkewY = def.VisualizerSkewY;
            _settings.Save();
            if (VisualizerWidget != null) VisualizerWidget.ApplySettings(_settings);
        }

        // === Z-INDEX ===
        private void SetClockZIndex(object sender, RoutedEventArgs e)
        {
            var current = ClockWidgetControl != null ? Canvas.GetZIndex(ClockWidgetControl) : _settings.ClockZ;
            var input = Interaction.InputBox("Masukkan Z-Index (integer):", "Set Z-Index Clock", current.ToString());
            if (int.TryParse(input, out int z))
            {
                if (ClockWidgetControl != null) Canvas.SetZIndex(ClockWidgetControl, z);
                _settings.ClockZ = z;
                _settings.Save();
            }
        }

        private void SetWeatherZIndex(object sender, RoutedEventArgs e)
        {
            var current = WeatherWidget != null ? Canvas.GetZIndex(WeatherWidget) : _settings.WeatherZ;
            var input = Interaction.InputBox("Masukkan Z-Index (integer):", "Set Z-Index Weather", current.ToString());
            if (int.TryParse(input, out int z))
            {
                if (WeatherWidget != null) Canvas.SetZIndex(WeatherWidget, z);
                _settings.WeatherZ = z;
                _settings.Save();
            }
        }

        private void SetVisualizerZIndex(object sender, RoutedEventArgs e)
        {
            var current = VisualizerWidget != null ? Canvas.GetZIndex(VisualizerWidget) : _settings.VisualizerZ;
            var input = Interaction.InputBox("Masukkan Z-Index (integer):", "Set Z-Index Visualizer", current.ToString());
            if (int.TryParse(input, out int z))
            {
                if (VisualizerWidget != null) Canvas.SetZIndex(VisualizerWidget, z);
                _settings.VisualizerZ = z;
                _settings.Save();
            }
        }
    }
}
