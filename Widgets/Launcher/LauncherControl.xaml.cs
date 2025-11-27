using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace DesktopWidget.Widgets
{
    public partial class LauncherControl : UserControl
    {
        public event EventHandler<bool>? ClockToggled;
        public event EventHandler<bool>? WeatherToggled;
        public event EventHandler<bool>? VisualizerToggled;
        public event EventHandler? ClockResizeRequested;
        public event EventHandler? WeatherResizeRequested;
        public event EventHandler? VisualizerResizeRequested;
        public event EventHandler? UndoRequested;

        public LauncherControl()
        {
            InitializeComponent();
        }

        private void Checkbox_Changed(object sender, RoutedEventArgs e)
        {
            ClockToggled?.Invoke(this, ClockCheckbox.IsChecked == true);
            WeatherToggled?.Invoke(this, WeatherCheckbox.IsChecked == true);
            VisualizerToggled?.Invoke(this, VisualizerCheckbox.IsChecked == true);
        }

        private void ClockResize_Click(object sender, RoutedEventArgs e)
        {
            ClockResizeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void WeatherResize_Click(object sender, RoutedEventArgs e)
        {
            WeatherResizeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void VisualizerResize_Click(object sender, RoutedEventArgs e)
        {
            VisualizerResizeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            UndoRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ShowSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            LauncherSettingsPanel.Visibility = ShowSettingsBtn.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
{
    // Tutup semua widget & app
    Application.Current.Shutdown();
}

private void HideLauncherBtn_Click(object sender, RoutedEventArgs e)
{
    ((App)Application.Current).HideLauncher();
}


    }
}
