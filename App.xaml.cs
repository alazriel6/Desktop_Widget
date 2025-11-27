using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace DesktopWidget
{
    public partial class App : Application
    {
private TaskbarIcon trayIcon = null!;
private MainWindow mainWindow = null!;




        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Buat window utama (launcher)
            mainWindow = new MainWindow();
            mainWindow.Show();

var iconUri = new Uri("pack://application:,,,/Assets/Icons/app.ico", UriKind.Absolute);
using var iconStream = Application.GetResourceStream(iconUri).Stream;

trayIcon = new TaskbarIcon
{
    Icon = new System.Drawing.Icon(iconStream),
    ToolTipText = "Desktop Widget"
};


            // Klik kiri tray icon = tampilkan launcher
            trayIcon.TrayLeftMouseUp += (s, ev) => ShowLauncher();

            // Context menu
            var menu = new System.Windows.Controls.ContextMenu();

            var showItem = new System.Windows.Controls.MenuItem { Header = "Show" };
            showItem.Click += (s, ev) => ShowLauncher();

            var hideItem = new System.Windows.Controls.MenuItem { Header = "Hide" };
            hideItem.Click += (s, ev) => HideLauncher();

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, ev) =>
            {
                mainWindow.Close();
                Shutdown();
            };

            menu.Items.Add(showItem);
            menu.Items.Add(hideItem);
            menu.Items.Add(exitItem);

            trayIcon.ContextMenu = menu;
        }

        // ==== FUNGSI PUBLIK ====
public void HideLauncher()
{
    mainWindow.LauncherControl.Visibility = Visibility.Collapsed;
}

public void ShowLauncher()
{
    mainWindow.LauncherControl.Visibility = Visibility.Visible;
    mainWindow.Activate();
}


        protected override void OnExit(ExitEventArgs e)
        {
            trayIcon.Dispose();
            base.OnExit(e);
        }
    }
}
