using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Windows.Threading;
using DesktopWidget;

namespace DesktopWidget.Widgets
{
    public partial class WeatherWidget : UserControl
    {
        public TextBlock TempTextBlock => WeatherTemp;
        public TextBlock LocationTextBlock => WeatherLocation;
        public TextBlock SummaryTextBlock => WeatherSummary;
        public TextBlock DetailsTextBlock => WeatherDetails;
        public Image IconImage => WeatherIcon;
        public Border WeatherBorder => WeatherPanel;
        public RotateTransform WeatherRotateTransform => WeatherRotate;
        public SkewTransform WeatherSkewTransform => WeatherSkew;

        public WeatherWidget()
        {
            InitializeComponent();
        }

        private DispatcherTimer? _weatherTimer;

        // start internal weather fetch timer
        public void StartWeatherTimer()
        {
            _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _weatherTimer.Tick += async (s, e) => await FetchWeather();
            _weatherTimer.Start();
            // initial fetch
            _ = FetchWeather();
        }

        private async Task FetchWeather()
        {
            try
            {
                using var client = new HttpClient();
                string url = "https://api.bmkg.go.id/publik/prakiraan-cuaca?adm4=32.01.33.2004"; // ganti sesuai kode wilayah
                var json = await client.GetStringAsync(url);

                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var lokasi = root.GetProperty("lokasi");
                string desa = lokasi.TryGetProperty("desa", out var desaEl) ? GetStringSafe(desaEl) : "";
                string kecamatan = lokasi.TryGetProperty("kecamatan", out var kecEl) ? GetStringSafe(kecEl) : "";

                if (root.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array && dataArr.GetArrayLength() > 0)
                {
                    var first = dataArr[0];
                    if (first.TryGetProperty("cuaca", out var cuacaArr) && cuacaArr.ValueKind == JsonValueKind.Array && cuacaArr.GetArrayLength() > 0)
                    {
                        var innerArr = cuacaArr[0];
                        if (innerArr.ValueKind == JsonValueKind.Array && innerArr.GetArrayLength() > 0)
                        {
                            var cuaca0 = innerArr[0];

                            string sky = cuaca0.TryGetProperty("weather_desc", out var desc) ? GetStringSafe(desc) : "?";
                            string temp = cuaca0.TryGetProperty("t", out var t) ? GetStringSafe(t) : "?";
                            string hum = cuaca0.TryGetProperty("hu", out var hu) ? GetStringSafe(hu) : "?";
                            string ws = cuaca0.TryGetProperty("ws", out var wsEl) ? GetStringSafe(wsEl) : "?";

                            // update UI on dispatcher
                            Dispatcher.Invoke(() => UpdateWeather(sky, temp, desa, kecamatan, hum, ws));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (SummaryTextBlock != null) SummaryTextBlock.Text = "BMKG error";
                if (DetailsTextBlock != null) DetailsTextBlock.Text = ex.Message;
            }
        }

        // helper moved here for parsing
        private string GetStringSafe(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? "",
                JsonValueKind.Number => el.GetRawText(),
                _ => ""
            };
        }

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
                var t = (Color)ColorConverter.ConvertFromString(s.WeatherTempColor ?? "White");
                TempTextBlock.Foreground = new SolidColorBrush(t);
            }
            catch { }
        }

        // Allow MainWindow to supply parsed weather values and let the widget update its own UI
        public void UpdateWeather(string sky, string temp, string desa, string kecamatan, string humidity, string wind)
        {
            TempTextBlock.Text = $"{temp}°C";
            LocationTextBlock.Text = $"{desa}, {kecamatan}";
            SummaryTextBlock.Text = sky;
            DetailsTextBlock.Text = $"Humidity: {humidity}% | Wind: {wind} m/s";
            SetValue(System.Windows.Controls.ToolTipService.ToolTipProperty, null);
            // choose icon based on sky
            var icon = "Assets/Icons/cloud.png";
            var s = sky?.ToLower() ?? string.Empty;
            if (s.Contains("hujan") && s.Contains("petir")) icon = "Assets/Icons/storm.png";
            else if (s.Contains("hujan") && s.Contains("lebat")) icon = "Assets/Icons/heavy_rain.png";
            else if (s.Contains("hujan")) icon = "Assets/Icons/rain.png";
            else if (s.Contains("berawan")) icon = "Assets/Icons/cloud.png";
            else if (s.Contains("cerah")) icon = "Assets/Icons/sun.png";
            else if (s.Contains("badai")) icon = "Assets/Icons/storm.png";
            else if (s.Contains("salju")) icon = "Assets/Icons/snow.png";

            try
            {
                IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(icon, UriKind.Relative));
            }
            catch
            {
                // ignore icon load errors
            }
        }
        
        // Events for parent window
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

        private void ResizeWeather_SetWidth(object sender, RoutedEventArgs e) { ResizeWidthRequested?.Invoke(this, EventArgs.Empty); }
        private void ResizeWeather_SetHeight(object sender, RoutedEventArgs e) { ResizeHeightRequested?.Invoke(this, EventArgs.Empty); }
        private void RotateWeather_Set(object sender, RoutedEventArgs e) { RotateRequested?.Invoke(this, EventArgs.Empty); }
        private void SkewWeather_SetX(object sender, RoutedEventArgs e) { SkewXRequested?.Invoke(this, EventArgs.Empty); }
        private void SkewWeather_SetY(object sender, RoutedEventArgs e) { SkewYRequested?.Invoke(this, EventArgs.Empty); }
        private void SetWeatherZIndex(object sender, RoutedEventArgs e) { SetZIndexRequested?.Invoke(this, EventArgs.Empty); }
    }
}
