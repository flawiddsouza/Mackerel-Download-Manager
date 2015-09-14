using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Linq;
using System.ComponentModel;

namespace Mackerel_Download_Manager.Dialogs
{
	/// <summary>
	/// Interaction logic for DownloadProgress.xaml
	/// </summary>
	public partial class DownloadProgress : Window
	{
        Download currentDownload;

        DownloadEntry downloadData;

        private bool paused;

        public DownloadProgress(string currentDownloadID)
		{
			InitializeComponent();
			SourceInitialized += OnSourceInitialized;

            downloadData = Downloads.DownloadEntries.Where(download => download.DownloadID == currentDownloadID).FirstOrDefault();

            currentDownload = Mackerel.currentDownloads[downloadData.DownloadID];

            currentDownload.ResumablityChanged += CurrentDownload_ResumablityChanged;
            currentDownload.ProgressChanged += CurrentDownload_ProgressChanged;
            currentDownload.Completed += CurrentDownload_Completed;
            currentDownload.Error += CurrentDownload_Error;

            //set temporary values before download starts
            this.Title = downloadData.FileName;
            FileSize.Text = Helper.SizeSuffix(downloadData.Size, 3);
            Downloaded.Text = "Checking...";
            TransferRate.Text = "(Unknown)";
            TimeLeft.Text = "(Unknown)";


            DownloadLink.Text = downloadData.DownloadLink.ToString();
			Status.Text = "Connecting...";

			FileSize.Text = Helper.SizeSuffix(downloadData.Size, 3) != null ? Helper.SizeSuffix(downloadData.Size, 3) : "Unknown";

            // enables Mackerel class to stop downloads just by setting downloadData.Running value to false
            Downloads.DownloadEntries.ListChanged += delegate
            {
                if (!downloadData.Running && !currentDownload.Stop)
                {
                    currentDownload.StopDownload();
                    downloadData.TransferRate = null;
                    Close();
                }
            };
        }

        private void CurrentDownload_Error(object sender, DownloadStatusChangedEventArgs e)
        {
            MessageBox.Show(e.ErrorMessage + "\nPlease check your internet connection!", downloadData.FileName);
            Dispatcher.Invoke(() =>
            {
                Close(); // HACK this only works once, the second time the messagebox is displayed it fails to close the window
            });
        }

        private void CurrentDownload_ResumablityChanged(object sender, DownloadStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
			Status.Text = "Receiving data...";

                if (e.ResumeSupported)
                {
                    ResumeCapability.Text = "Yes";
                }
                else
                {
                    ResumeCapability.Foreground = System.Windows.Media.Brushes.Red;
                    ResumeCapability.Text = "No";
                }
            });
        }

        private void CurrentDownload_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (e.ProgressPercentage >= 1)
                    {
                        this.Title = string.Format("{0:0}% ", e.ProgressPercentage) + downloadData.FileName;
                    }

                    FileSize.Text = Helper.SizeSuffix(e.TotalBytesToReceive, 3) != null && Helper.SizeSuffix(e.TotalBytesToReceive, 3) != "-1.00 bytes" ? Helper.SizeSuffix(e.TotalBytesToReceive, 3) : "Unknown";
                    downloadData.Size = e.TotalBytesToReceive;

                    Downloaded.Text = Helper.SizeSuffix(e.BytesReceived, 3) + string.Format(" ( {0:0.00} % )", e.ProgressPercentage);

                    TransferRate.Text = Helper.SizeSuffix((long)e.CurrentSpeed, 3) + "/sec";
                    downloadData.TransferRate = Helper.SizeSuffix((long)e.CurrentSpeed, 3) + "/sec";

                    TimeLeft.Text = e.TimeLeft.ToString(@"dd\.hh\:mm\:ss");
                    downloadData.TimeLeft = e.TimeLeft.ToString(@"dd\.hh\:mm\:ss");

                    ProgressBar.Value = e.ProgressPercentage;
                    downloadData.Status = string.Format("{0:0.00}% ", e.ProgressPercentage);
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal; // you need to set this, otherwise progressvalue does nothing
                    TaskbarItemInfo.ProgressValue = e.ProgressPercentage / 100f;
                });
            }
            catch (System.Threading.Tasks.TaskCanceledException) {}
        }

        private void CurrentDownload_Completed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Status.Text = "Completed";

                downloadData.Status = "Complete";
                downloadData.TimeLeft = null;
                downloadData.TransferRate = null;
                downloadData.Running = false;
                Downloads.Serialize();

                this.Close();
            });
		}

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if(!paused)
            {
                currentDownload.StopDownload();
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
                Status.Text = "Paused";
                Pause.Content = "Resume";
                paused = true;
            }
            else
            {
                Mackerel.StartDownloadResumeStyle(downloadData.DownloadID);
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                Status.Text = "Receiving data...";
                Pause.Content = "Pause";
                paused = false;
            }
        }

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
            currentDownload.StopDownload();
            downloadData.TransferRate = null; // not working
            downloadData.Running = false;
			this.Close();
		}

        protected override void OnClosing(CancelEventArgs e)
        {
            currentDownload.StopDownload();
            downloadData.TransferRate = null; // not working
            downloadData.Running = false;
            base.OnClosing(e);
        }

		// disable maximize button but don't disable the resize window option
		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private const int GWL_STYLE = -16;
		private const int WS_MAXIMIZEBOX = 0x10000;

		private void OnSourceInitialized(object sender, EventArgs e)
		{
			var hwnd = new WindowInteropHelper((Window)sender).Handle;
			var value = GetWindowLong(hwnd, GWL_STYLE);
			SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));
		}
	}
}