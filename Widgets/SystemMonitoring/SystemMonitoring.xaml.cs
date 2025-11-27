using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopWidget.Widgets
{
	public partial class SystemMonitorWidget : UserControl
	{
		// Event to notify parent window that this control requests a drag
		public event MouseButtonEventHandler? DragRequested;

		public SystemMonitorWidget()
		{
			InitializeComponent();
		}

		private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DragRequested?.Invoke(this, e);
		}
	}
}
