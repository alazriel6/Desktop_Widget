using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using DesktopWidget; // for WidgetSettings

namespace DesktopWidget.Widgets
{
    public partial class WeatherWidget : UserControl
    {
        private DispatcherTimer? _weatherTimer;

        // === Compatibility properties used by MainWindow ===
// === Compatibility properties used by MainWindow ===
public TextBlock TempTextBlock => WeatherTemp;
public TextBlock LocationTextBlock => WeatherLocation;
public TextBlock SummaryTextBlock => WeatherSummary;
public TextBlock DetailsTextBlock => WeatherDetails;

// FIXED: avoid name conflict with XAML
public Image WeatherIconElement => WeatherIcon;

public Border WeatherBorder => WeatherPanel;
public RotateTransform WeatherRotateTransform => WeatherRotate;
public SkewTransform WeatherSkewTransform => WeatherSkew;


        public WeatherWidget()
        {
            InitializeComponent();
        }

        // Start timer (default 10 minutes)
        public void StartWeatherTimer()
        {
            _weatherTimer?.Stop();
            _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
            _weatherTimer.Tick += async (s, e) => await FetchWeather();
            _weatherTimer.Start();
            _ = FetchWeather(); // initial fetch
        }

        // Fetch Weather from MET Norway
        private async Task FetchWeather()
        {
            try
            {
                double lat = -6.2;   // Jakarta
                double lon = 106.8;

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DesktopWidget/1.0 (contact@example.com)");

                string url = $"https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={lat}&lon={lon}";
                string json = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("properties", out var props) ||
                    !props.TryGetProperty("timeseries", out var series) ||
                    series.ValueKind != JsonValueKind.Array ||
                    series.GetArrayLength() == 0)
                {
                    throw new Exception("No forecast data");
                }

                // find timeseries entry closest to now
                DateTime now = DateTime.UtcNow;
                JsonElement closest = series[0];
                double minDiff = double.MaxValue;

                foreach (var entry in series.EnumerateArray())
                {
                    if (!entry.TryGetProperty("time", out var timeEl)) continue;
                    var timeStr = timeEl.GetString();
                    if (!DateTime.TryParse(timeStr, out DateTime t)) continue;
                    double diff = Math.Abs((t - now).TotalSeconds);
                    if (diff < minDiff) { minDiff = diff; closest = entry; }
                }

                var instant = closest.GetProperty("data").GetProperty("instant").GetProperty("details");

                string temp = instant.TryGetProperty("air_temperature", out var tEl) ? tEl.GetRawText() : "?";
                string humidity = instant.TryGetProperty("relative_humidity", out var hEl) ? hEl.GetRawText() : "?";
                string wind = instant.TryGetProperty("wind_speed", out var wEl) ? wEl.GetRawText() : "?";

                string symbol = "clearsky_day";
                if (closest.GetProperty("data").TryGetProperty("next_1_hours", out var next1) &&
                    next1.TryGetProperty("summary", out var summary) &&
                    summary.TryGetProperty("symbol_code", out var sc))
                {
                    symbol = sc.GetString() ?? symbol;
                }

                bool isDay = IsLocalDay(lat, lon);

                string desc = GetDescription(symbol);
                string iconPath = GetIcon(symbol, isDay);   // patched here

                Dispatcher.Invoke(() =>
                {
                    UpdateWeatherUI(temp, humidity, wind, symbol, desc, iconPath);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    SummaryTextBlock.Text = "Weather API error";
                    DetailsTextBlock.Text = ex.Message;
                });
            }
        }

        // Update UI
        private void UpdateWeatherUI(string temp, string humidity, string wind, string symbol, string description, string iconPath)
        {
            if (double.TryParse(temp, out var td))
                temp = Math.Round(td).ToString();

            TempTextBlock.Text = $"{temp}°C";
            LocationTextBlock.Text = "Jakarta";
            SummaryTextBlock.Text = description;
            DetailsTextBlock.Text = $"Humidity: {humidity}% | Wind: {wind} m/s";

           try
{
    var uri = new Uri($"pack://application:,,,/{iconPath}", UriKind.Absolute);
    WeatherIconElement.Source = new BitmapImage(uri);
}
catch (Exception ex)
{
    DetailsTextBlock.Text = "Icon error: " + ex.Message;
}

        }

        // Simple day/night detection
        private bool IsLocalDay(double lat, double lon)
        {
            try
            {
                int offset = (int)Math.Round(lon / 15.0);
                string tz = offset >= 0 ? $"Etc/GMT-{offset}" : $"Etc/GMT+{Math.Abs(offset)}";
                var now = DateTime.UtcNow;
                var local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, tz);
                return local.Hour >= 6 && local.Hour < 18;
            }
            catch
            {
                int hour = DateTime.UtcNow.Hour;
                return hour >= 6 && hour < 18;
            }
        }

        // =========================
        // Description Mapping
        // =========================
        private string GetDescription(string code)
        {
            code = (code ?? "").ToLowerInvariant();
            if (code.Contains("clearsky")) return "Cerah";
            if (code.Contains("partlycloudy")) return "Cerah Berawan";
            if (code.Contains("cloudy")) return "Berawan";
            if (code.Contains("lightrain") || code.Contains("rainshowers")) return "Hujan Ringan";
            if (code.Contains("rain")) return "Hujan";
            if (code.Contains("heavyrain")) return "Hujan Lebat";
            if (code.Contains("thunder") || code.Contains("ts")) return "Badai Petir";
            if (code.Contains("fog")) return "Kabut";
            return "Cuaca";
        }

        // =========================
        // Icon Mapping (PATCHED)
        // =========================
        private string GetIcon(string code, bool isDay)
        {
            code = (code ?? "").ToLowerInvariant();

            if (code.Contains("clearsky")) return isDay ? "Assets/Icons/sun.png" : "Assets/Icons/moon.png";
            if (code.Contains("partlycloudy")) return isDay ? "Assets/Icons/partlycloudy_day.png" : "Assets/Icons/partlycloudy_night.png";
            if (code.Contains("cloudy")) return "Assets/Icons/cloud.png";
            if (code.Contains("lightrain") || code.Contains("rainshowers")) return "Assets/Icons/rain_light.png";
            if (code.Contains("heavyrain")) return "Assets/Icons/rain_heavy.png";
            if (code.Contains("rain")) return "Assets/Icons/rain.png";
            if (code.Contains("thunder") || code.Contains("ts")) return "Assets/Icons/storm.png";
            if (code.Contains("fog")) return "Assets/Icons/fog.png";

            return isDay ? "Assets/Icons/sun.png" : "Assets/Icons/moon.png";
        }

        // Apply Settings
        public void ApplySettings(WidgetSettings s)
        {
            if (s == null) return;

            if (!double.IsNaN(s.WeatherWidth)) this.Width = s.WeatherWidth; else this.Width = Double.NaN;
            if (!double.IsNaN(s.WeatherHeight)) this.Height = s.WeatherHeight; else this.Height = Double.NaN;

            if (WeatherRotateTransform != null) WeatherRotateTransform.Angle = s.WeatherRotate;

            if (WeatherSkewTransform != null)
            {
                WeatherSkewTransform.AngleX = s.WeatherSkewX;
                WeatherSkewTransform.AngleY = s.WeatherSkewY;
            }

            try
            {
                var c = (Color)ColorConverter.ConvertFromString(s.WeatherTempColor ?? "White");
                TempTextBlock.Foreground = new SolidColorBrush(c);
            }
            catch { }
        }

        // Events for MainWindow
        public event MouseButtonEventHandler? DragRequested;
        public event EventHandler? ResizeWidthRequested;
        public event EventHandler? ResizeHeightRequested;
        public event EventHandler? RotateRequested;
        public event EventHandler? SkewXRequested;
        public event EventHandler? SkewYRequested;
        public event EventHandler? SetZIndexRequested;
        public event EventHandler? OpenSettingsRequested;
        public event EventHandler? CloseRequested;

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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

        private void ResizeWeather_SetWidth(object sender, RoutedEventArgs e) => ResizeWidthRequested?.Invoke(this, EventArgs.Empty);
        private void ResizeWeather_SetHeight(object sender, RoutedEventArgs e) => ResizeHeightRequested?.Invoke(this, EventArgs.Empty);
        private void RotateWeather_Set(object sender, RoutedEventArgs e) => RotateRequested?.Invoke(this, EventArgs.Empty);
        private void SkewWeather_SetX(object sender, RoutedEventArgs e) => SkewXRequested?.Invoke(this, EventArgs.Empty);
        private void SkewWeather_SetY(object sender, RoutedEventArgs e) => SkewYRequested?.Invoke(this, EventArgs.Empty);
        private void SetWeatherZIndex(object sender, RoutedEventArgs e) => SetZIndexRequested?.Invoke(this, EventArgs.Empty);
    }
}
