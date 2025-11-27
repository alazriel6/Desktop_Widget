using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NAudio.Wave;
using System;
using System.Threading;
using System.Windows.Shapes;
using NAudio.Dsp;

namespace DesktopWidget.Widgets
{
    public partial class VisualizerWidget : UserControl
    {
        public Canvas VisualizerCanvasControl => VisualizerCanvas;
        public Border VisualizerBorder => VisualizerPanel;
    public RotateTransform VisualizerRotateTransform => VisualizerRotate;
    public SkewTransform VisualizerSkewTransform => VisualizerSkew;
        private WasapiLoopbackCapture? _capture;
        private float[] _lastSamples = Array.Empty<float>();
        private readonly object _audioLock = new object();
        private double[] _barValues = new double[256];
        private double[] _peakValues = new double[256];
        private double _peakDecay = 0.02;
        private Color _visualizerBaseColor = Colors.Aqua;

        // Events forwarded to MainWindow
        public event MouseButtonEventHandler? DragRequested;
        public event EventHandler? ResizeWidthRequested;
        public event EventHandler? ResizeHeightRequested;
        public event EventHandler? RotateRequested;
        public event EventHandler? SkewXRequested;
        public event EventHandler? SkewYRequested;
        public event EventHandler? SetZIndexRequested;
        public event EventHandler? OpenSettingsRequested;
        public event EventHandler? CloseRequested;

        public VisualizerWidget()
        {
            InitializeComponent();
            // keep XAML-defined transforms (VisualizerRotate / VisualizerSkew) as the RenderTransform
        }

        public void ApplySettings(WidgetSettings s)
        {
            if (s == null) return;

            if (s.VisualizerWidth > 0) this.Width = s.VisualizerWidth;
            if (s.VisualizerHeight > 0) this.Height = s.VisualizerHeight;

            InitializeTransforms(s.VisualizerRotate, s.VisualizerSkewX, s.VisualizerSkewY, (Color)ColorConverter.ConvertFromString(s.VisualizerColor ?? "Aqua"));
        }

        public void InitializeTransforms(double rotateAngle, double skewX, double skewY, Color baseColor)
        {
            if (VisualizerRotate != null) VisualizerRotate.Angle = rotateAngle;
            if (VisualizerSkew != null)
            {
                VisualizerSkew.AngleX = skewX;
                VisualizerSkew.AngleY = skewY;
            }
            _visualizerBaseColor = baseColor;
        }

        public void SetBaseColor(Color color)
        {
            _visualizerBaseColor = color;
            // redraw to reflect color change
            Dispatcher.Invoke(() => DrawVisualizer());
        }

        public void StartAudioCapture()
        {
            try
            {
                _capture = new WasapiLoopbackCapture();
                _capture.DataAvailable += (s, e) =>
                {
                    int sampleCount = e.BytesRecorded / 4;
                    var buffer = new float[sampleCount];
                    Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
                    lock (_audioLock) _lastSamples = buffer;
                    Dispatcher.Invoke(DrawVisualizer);
                };
                _capture.StartRecording();
            }
            catch
            {
                // ignore capture errors here; MainWindow may show details elsewhere
            }
        }

        public void StopAudioCapture()
        {
            _capture?.StopRecording();
            _capture?.Dispose();
            _capture = null;
        }

        public void DrawVisualizer()
        {
            var canvas = VisualizerCanvasControl;
            if (canvas == null) return;

            float[] samples;
            lock (_audioLock) samples = _lastSamples;

            canvas.Children.Clear();
            if (samples.Length == 0) return;

            double height = canvas.ActualHeight;
            double barWidth;
            int bars = 64;

            int fftSize = 8192;
            var fftBuffer = new Complex[fftSize];
            for (int i = 0; i < fftSize && i < samples.Length; i++)
            {
                float window = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (fftSize - 1))));
                fftBuffer[i].X = samples[i] * window;
                fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftSize, 2), fftBuffer);

            double[] spectrum = new double[fftSize / 2];
            for (int i = 0; i < spectrum.Length; i++)
                spectrum[i] = Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);

            barWidth = Math.Max(4, canvas.ActualWidth / bars);

            for (int i = 0; i < bars; i++)
            {
                double logStart = Math.Pow((double)i / bars, 2.0);
                double logEnd = Math.Pow((double)(i + 1) / bars, 2.0);

                int start = (int)(logStart * fftSize / 2);
                int end = (int)(logEnd * fftSize / 2);
                if (end <= start) end = start + 1;

                double sum = 0;
                for (int j = start; j < end; j++)
                    sum += spectrum[j];

                double magnitude = Math.Log10(spectrum[i] * 1000 + 1);
                double h = Math.Pow(magnitude * 180, 1.1);
                h = Math.Min(height, h);

                double normIndex = (i + 0.1) / bars;
                double weight = 0.1 + Math.Log10(1 + 50 * normIndex);
                h *= weight;

                if (_barValues == null || _barValues.Length != bars)
                    _barValues = new double[bars];

                h = _barValues[i] * 0.7 + h * 0.3;
                _barValues[i] = h;

                if (_peakValues == null || _peakValues.Length != bars)
                    _peakValues = new double[bars];

                if (h > _peakValues[i])
                    _peakValues[i] = h;
                else
                    _peakValues[i] = Math.Max(0, _peakValues[i] - _peakDecay * height);

                var rect = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 2),
                    Height = Math.Max(1, h),
                    Fill = new LinearGradientBrush(_visualizerBaseColor, Colors.White, 90),
                    RadiusX = 3,
                    RadiusY = 3,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, i * barWidth);
                Canvas.SetTop(rect, height - h);
                canvas.Children.Add(rect);

                var peakRect = new Rectangle
                {
                    Width = barWidth - 2,
                    Height = 2,
                    Fill = Brushes.White,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(peakRect, i * barWidth);
                Canvas.SetTop(peakRect, height - _peakValues[i] - 2);
                canvas.Children.Add(peakRect);
            }
        }

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

        private void ResizeVisualizer_SetWidth(object sender, RoutedEventArgs e) { ResizeWidthRequested?.Invoke(this, EventArgs.Empty); }
        private void ResizeVisualizer_SetHeight(object sender, RoutedEventArgs e) { ResizeHeightRequested?.Invoke(this, EventArgs.Empty); }
        private void RotateVisualizer_Set(object sender, RoutedEventArgs e) { RotateRequested?.Invoke(this, EventArgs.Empty); }
        private void SkewVisualizer_SetX(object sender, RoutedEventArgs e) { SkewXRequested?.Invoke(this, EventArgs.Empty); }
        private void SkewVisualizer_SetY(object sender, RoutedEventArgs e) { SkewYRequested?.Invoke(this, EventArgs.Empty); }
        private void SetVisualizerZIndex(object sender, RoutedEventArgs e) { SetZIndexRequested?.Invoke(this, EventArgs.Empty); }
    }
}
