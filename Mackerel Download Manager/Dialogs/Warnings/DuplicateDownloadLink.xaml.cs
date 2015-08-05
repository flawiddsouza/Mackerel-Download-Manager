using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Linq;

namespace Mackerel_Download_Manager.Dialogs.Warnings
{
	/// <summary>
	/// Interaction logic for DuplicateDownloadLink.xaml
	/// </summary>
	public partial class DuplicateDownloadLink : Window
	{
		public DuplicateDownloadLink()
		{
			InitializeComponent();
		}

		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			Uri Link = new Uri(DownloadLink.Text);
			if (Option1.IsChecked.Value == true)
			{
				int count = Downloads.DownloadEntries.Count(download => download.DownloadLink == Link);
				MessageBox.Show(count.ToString());
			}			
			else if (Option2.IsChecked.Value == true)
			{
				// just the reset download state of the existing file to redownload
			}
			Owner.Focus(); // for some reason MainWindow goes into the background if this isn't there
			this.Close();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			Owner.Focus();
			this.Close();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			HideWindowIcon.RemoveIcon(this);
		}
	}

	public static class HideWindowIcon
	{
		[DllImport("user32.dll")]
		static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll")]
		static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

		[DllImport("user32.dll")]
		static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
				   int x, int y, int width, int height, uint flags);

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hwnd, uint msg,
				   IntPtr wParam, IntPtr lParam);

		const int GWL_EXSTYLE = -20;
		const int WS_EX_DLGMODALFRAME = 0x0001;
		const int SWP_NOSIZE = 0x0001;
		const int SWP_NOMOVE = 0x0002;
		const int SWP_NOZORDER = 0x0004;
		const int SWP_FRAMECHANGED = 0x0020;
		const uint WM_SETICON = 0x0080;

		public static void RemoveIcon(Window window)
		{
			// Get this window's handle
			IntPtr hwnd = new WindowInteropHelper(window).Handle;

			// Change the extended window style to not show a window icon
			int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);

			// Update the window's non-client area to reflect the changes
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE |
				  SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
		}

	}
}

